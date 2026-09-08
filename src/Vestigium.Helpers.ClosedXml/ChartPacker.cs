using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// ClosedXML 0.105 cannot create charts. After it writes the workbook we splice
/// native Excel chart/drawing parts into the OOXML package.
/// </summary>
internal static class ChartPacker
{
    private static readonly XNamespace Ss = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Ct = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Embed(byte[] xlsx, IReadOnlyList<SheetChart> charts)
    {
        if (charts.Count == 0)
            return xlsx;

        using var ms = new MemoryStream();
        ms.Write(xlsx, 0, xlsx.Length);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            EmbedCore(zip, charts);
        return ms.ToArray();
    }

    private static void EmbedCore(ZipArchive zip, IReadOnlyList<SheetChart> charts)
    {
        var sheetParts = MapSheets(zip);
        var types = XDocument.Parse(Read(zip, "[Content_Types].xml"));
        var drawingMax = MaxPart(zip, "xl/drawings/drawing", ".xml");
        var chartMax = MaxPart(zip, "xl/charts/chart", ".xml");

        foreach (var group in charts.GroupBy(c => c.Sheet, StringComparer.OrdinalIgnoreCase))
        {
            if (!sheetParts.TryGetValue(group.Key, out var sheetPart))
                continue;

            var sheetXml = XDocument.Parse(Read(zip, sheetPart));
            var relsPath = RelsPath(sheetPart);
            var rels = Exists(zip, relsPath)
                ? XDocument.Parse(Read(zip, relsPath))
                : new XDocument(new XElement(Pkg + "Relationships"));
            if (rels.Root!.Attribute("xmlns") is null && rels.Root.Name.Namespace == XNamespace.None)
                rels.Root.SetAttributeValue("xmlns", Pkg.NamespaceName);

            drawingMax++;
            var drawingPath = $"xl/drawings/drawing{drawingMax}.xml";
            var drawingRelPath = $"xl/drawings/_rels/drawing{drawingMax}.xml.rels";
            var drawingRid = NextRid(rels);
            AddRel(rels, drawingRid, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing", RelTarget(sheetPart, drawingPath));

            var drawingRels = new XDocument(new XElement(Pkg + "Relationships"));
            var anchors = new StringBuilder();
            var chartIndex = 0;
            foreach (var chart in group)
            {
                chartMax++;
                chartIndex++;
                var chartPath = $"xl/charts/chart{chartMax}.xml";
                var chartRid = "rId" + chartIndex;
                AddRel(drawingRels, chartRid, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart", RelTarget(drawingPath, chartPath));
                Write(zip, chartPath, ChartXml(chart));
                AddOverride(types, "/" + chartPath, "application/vnd.openxmlformats-officedocument.drawingml.chart+xml");
                anchors.Append(AnchorXml(chart, chartRid, chartIndex + 1));
            }

            Write(zip, drawingPath, DrawingXml(anchors.ToString()));
            Write(zip, drawingRelPath, XmlOf(drawingRels));
            AddOverride(types, "/" + drawingPath, "application/vnd.openxmlformats-officedocument.drawing+xml");
            EnsureDrawing(sheetXml, drawingRid);
            Write(zip, sheetPart, XmlOf(sheetXml));
            Write(zip, relsPath, XmlOf(rels));
        }

        Write(zip, "[Content_Types].xml", XmlOf(types));
    }

    private static Dictionary<string, string> MapSheets(ZipArchive zip)
    {
        var wb = XDocument.Parse(Read(zip, "xl/workbook.xml"));
        var rels = XDocument.Parse(Read(zip, "xl/_rels/workbook.xml.rels"));
        var ridToTarget = rels.Root!.Elements().ToDictionary(
            e => (string?)e.Attribute("Id") ?? "",
            e => (string?)e.Attribute("Target") ?? "",
            StringComparer.Ordinal);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sheets = wb.Root?.Element(Ss + "sheets") ?? wb.Root?.Element("sheets");
        if (sheets is null)
            return map;
        foreach (var sh in sheets.Elements())
        {
            var name = (string?)sh.Attribute("name");
            var rid = (string?)sh.Attribute(R + "id") ?? (string?)sh.Attribute("r:id");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(rid))
                continue;
            if (!ridToTarget.TryGetValue(rid, out var target) || string.IsNullOrWhiteSpace(target))
                continue;
            var part = target.Replace('\\', '/');
            if (part.StartsWith("/xl/", StringComparison.Ordinal))
                part = part.TrimStart('/');
            else if (part.StartsWith("xl/", StringComparison.Ordinal))
            { }
            else
                part = "xl/" + part.TrimStart('/');
            map[name] = part;
        }
        return map;
    }

    private static void EnsureDrawing(XDocument sheet, string rid)
    {
        var root = sheet.Root!;
        var ns = root.Name.Namespace;
        if (root.Attribute(XNamespace.Xmlns + "r") is null)
            root.SetAttributeValue(XNamespace.Xmlns + "r", R.NamespaceName);
        if (root.Element(ns + "drawing") is not null)
            return;
        var drawing = new XElement(ns + "drawing", new XAttribute(R + "id", rid));
        var tableParts = root.Element(ns + "tableParts");
        if (tableParts is not null)
            tableParts.AddBeforeSelf(drawing);
        else
            root.Add(drawing);
    }

    private static void AddOverride(XDocument types, string partName, string contentType)
    {
        var root = types.Root!;
        var ns = root.Name.Namespace == XNamespace.None ? Ct : root.Name.Namespace;
        foreach (var ov in root.Elements())
        {
            if (string.Equals((string?)ov.Attribute("PartName"), partName, StringComparison.OrdinalIgnoreCase))
                return;
        }
        root.Add(new XElement(ns + "Override",
            new XAttribute("PartName", partName),
            new XAttribute("ContentType", contentType)));
    }

    private static void AddRel(XDocument rels, string id, string type, string target)
    {
        var root = rels.Root!;
        var ns = root.Name.Namespace == XNamespace.None ? Pkg : root.Name.Namespace;
        root.Add(new XElement(ns + "Relationship",
            new XAttribute("Id", id),
            new XAttribute("Type", type),
            new XAttribute("Target", target)));
    }

    private static string NextRid(XDocument rels)
    {
        var max = 0;
        foreach (var e in rels.Root!.Elements())
        {
            var id = (string?)e.Attribute("Id") ?? "";
            if (id.StartsWith("rId", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), out var n) && n > max)
                max = n;
        }
        return "rId" + (max + 1);
    }

    private static string RelTarget(string fromPart, string toPart)
    {
        var file = Path.GetFileName(toPart.Replace('\\', '/'));
        if (fromPart.Contains("/worksheets/", StringComparison.OrdinalIgnoreCase))
            return "../drawings/" + file;
        if (fromPart.Contains("/drawings/", StringComparison.OrdinalIgnoreCase))
            return "../charts/" + file;
        var fromDir = Path.GetDirectoryName(fromPart.Replace('/', Path.DirectorySeparatorChar)) ?? "xl";
        var relative = Path.GetRelativePath(fromDir, toPart.Replace('/', Path.DirectorySeparatorChar));
        return relative.Replace('\\', '/');
    }

    private static string RelsPath(string part)
    {
        var dir = Path.GetDirectoryName(part.Replace('/', Path.DirectorySeparatorChar))!.Replace('\\', '/');
        var file = Path.GetFileName(part);
        return $"{dir}/_rels/{file}.rels";
    }

    private static int MaxPart(ZipArchive zip, string prefix, string suffix)
    {
        var max = 0;
        foreach (var e in zip.Entries)
        {
            var n = e.FullName.Replace('\\', '/');
            if (!n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;
            var mid = n[prefix.Length..^suffix.Length];
            if (int.TryParse(mid, out var v) && v > max)
                max = v;
        }
        return max;
    }

    private static string ChartXml(SheetChart chart)
    {
        var color = (chart.Color ?? "1F4E79").TrimStart('#');
        var plot = chart.Kind switch
        {
            ChartKind.Line => LinePlot(chart),
            ChartKind.Bar => BarPlot(chart, "bar"),
            _ => BarPlot(chart, "col")
        };
        var catPos = chart.Kind == ChartKind.Bar ? "l" : "b";
        var valPos = chart.Kind == ChartKind.Bar ? "b" : "l";
        var legend = chart.Series.Count > 1
            ? """<c:legend><c:legendPos val="b"/><c:overlay val="0"/></c:legend>"""
            : """<c:legend><c:legendPos val="b"/><c:overlay val="1"/><c:delete val="1"/></c:legend>""";
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <c:chartSpace xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <c:date1904 val="0"/>
              <c:roundedCorners val="0"/>
              <c:chart>
                <c:title>
                  <c:tx>
                    <c:rich>
                      <a:bodyPr/>
                      <a:lstStyle/>
                      <a:p>
                        <a:pPr><a:defRPr sz="1200" b="1"/></a:pPr>
                        <a:r>
                          <a:rPr lang="en-US" sz="1200" b="1">
                            <a:solidFill><a:srgbClr val="{color}"/></a:solidFill>
                          </a:rPr>
                          <a:t>{Esc(chart.Title)}</a:t>
                        </a:r>
                      </a:p>
                    </c:rich>
                  </c:tx>
                  <c:overlay val="0"/>
                </c:title>
                <c:autoTitleDeleted val="0"/>
                <c:plotArea>
                  <c:layout/>
                  {plot}
                  <c:catAx>
                    <c:axId val="1"/>
                    <c:scaling><c:orientation val="minMax"/></c:scaling>
                    <c:delete val="0"/>
                    <c:axPos val="{catPos}"/>
                    <c:crossAx val="2"/>
                    <c:crosses val="autoZero"/>
                    <c:auto val="1"/>
                    <c:lblAlgn val="ctr"/>
                    <c:lblOffset val="100"/>
                    <c:tickLblPos val="nextTo"/>
                  </c:catAx>
                  <c:valAx>
                    <c:axId val="2"/>
                    <c:scaling><c:orientation val="minMax"/></c:scaling>
                    <c:delete val="0"/>
                    <c:axPos val="{valPos}"/>
                    <c:majorGridlines/>
                    <c:crossAx val="1"/>
                    <c:crosses val="autoZero"/>
                  </c:valAx>
                </c:plotArea>
                {legend}
                <c:plotVisOnly val="1"/>
              </c:chart>
            </c:chartSpace>
            """;
    }

    private static string BarPlot(SheetChart chart, string dir)
    {
        var series = new StringBuilder();
        for (var i = 0; i < chart.Series.Count; i++)
            series.Append(SeriesXml(chart, i, markers: false, smooth: false));
        return $"""
            <c:barChart>
              <c:barDir val="{dir}"/>
              <c:grouping val="clustered"/>
              <c:varyColors val="0"/>
              {series}
              <c:gapWidth val="80"/>
              <c:overlap val="0"/>
              <c:axId val="1"/>
              <c:axId val="2"/>
            </c:barChart>
            """;
    }

    private static string LinePlot(SheetChart chart)
    {
        var series = new StringBuilder();
        for (var i = 0; i < chart.Series.Count; i++)
            series.Append(SeriesXml(chart, i, markers: false, smooth: false));
        return $"""
            <c:lineChart>
              <c:grouping val="standard"/>
              <c:varyColors val="0"/>
              {series}
              <c:marker val="0"/>
              <c:smooth val="0"/>
              <c:axId val="1"/>
              <c:axId val="2"/>
            </c:lineChart>
            """;
    }

    private static string SeriesXml(SheetChart chart, int index, bool markers, bool smooth)
    {
        var s = chart.Series[index];
        var color = (s.Color ?? chart.Color ?? "1F4E79").TrimStart('#');
        var cat = chart.NumericCategories
            ? NumRef("cat", chart.CategoriesFormula, ParseNumbers(chart.Categories))
            : StrRef(chart.CategoriesFormula, chart.Categories);
        var extra = chart.Kind == ChartKind.Line
            ? """<c:marker><c:symbol val="none"/></c:marker><c:smooth val="0"/>"""
            : "";
        _ = markers;
        _ = smooth;
        return $"""
            <c:ser>
              <c:idx val="{index}"/>
              <c:order val="{index}"/>
              <c:tx><c:v>{Esc(s.Name)}</c:v></c:tx>
              <c:spPr>
                <a:solidFill><a:srgbClr val="{color}"/></a:solidFill>
                <a:ln w="25000"><a:solidFill><a:srgbClr val="{color}"/></a:solidFill></a:ln>
              </c:spPr>
              {cat}
              {NumRef("val", s.ValuesFormula, s.Values)}
              {extra}
            </c:ser>
            """;
    }

    private static string StrRef(string formula, IReadOnlyList<string> values)
    {
        var pts = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
            pts.Append($"<c:pt idx=\"{i}\"><c:v>{Esc(values[i])}</c:v></c:pt>");
        return $"""
            <c:cat>
              <c:strRef>
                <c:f>{Esc(formula)}</c:f>
                <c:strCache>
                  <c:ptCount val="{values.Count}"/>
                  {pts}
                </c:strCache>
              </c:strRef>
            </c:cat>
            """;
    }

    private static string NumRef(string tag, string formula, IReadOnlyList<double> values)
    {
        var pts = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
            pts.Append($"<c:pt idx=\"{i}\"><c:v>{Num(values[i])}</c:v></c:pt>");
        return $"""
            <c:{tag}>
              <c:numRef>
                <c:f>{Esc(formula)}</c:f>
                <c:numCache>
                  <c:formatCode>General</c:formatCode>
                  <c:ptCount val="{values.Count}"/>
                  {pts}
                </c:numCache>
              </c:numRef>
            </c:{tag}>
            """;
    }

    private static string DrawingXml(string anchors) => $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <xdr:wsDr xmlns:xdr="http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
          {anchors}
        </xdr:wsDr>
        """;

    private static string AnchorXml(SheetChart chart, string rid, int id)
    {
        var fromCol = Math.Max(0, chart.FromColumn);
        var fromRow = Math.Max(0, chart.FromRow);
        var toCol = Math.Max(fromCol + 1, chart.ToColumn);
        var toRow = Math.Max(fromRow + 1, chart.ToRow);
        return $"""
            <xdr:twoCellAnchor>
              <xdr:from><xdr:col>{fromCol}</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>{fromRow}</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>
              <xdr:to><xdr:col>{toCol}</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>{toRow}</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>
              <xdr:graphicFrame macro="">
                <xdr:nvGraphicFramePr>
                  <xdr:cNvPr id="{id}" name="{Esc(chart.Title)}"/>
                  <xdr:cNvGraphicFramePr><a:graphicFrameLocks noGrp="1"/></xdr:cNvGraphicFramePr>
                </xdr:nvGraphicFramePr>
                <xdr:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/></xdr:xfrm>
                <a:graphic>
                  <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/chart">
                    <c:chart xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" r:id="{rid}"/>
                  </a:graphicData>
                </a:graphic>
              </xdr:graphicFrame>
              <xdr:clientData/>
            </xdr:twoCellAnchor>
            """;
    }

    private static IReadOnlyList<double> ParseNumbers(IReadOnlyList<string> values)
    {
        var list = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (!double.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                n = i;
            list[i] = n;
        }
        return list;
    }

    private static string Num(double v) => v.ToString("G15", CultureInfo.InvariantCulture);

    private static string Esc(string s)
    {
        var amp = "\u0026";
        return s.Replace("&", amp + "amp;", StringComparison.Ordinal)
            .Replace("<", amp + "lt;", StringComparison.Ordinal)
            .Replace(">", amp + "gt;", StringComparison.Ordinal)
            .Replace("\u0022", amp + "quot;", StringComparison.Ordinal);
    }

    private static string XmlOf(XDocument doc)
    {
        var body = doc.ToString(SaveOptions.DisableFormatting);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + body;
    }

    private static string Read(ZipArchive zip, string path)
    {
        var entry = Find(zip, path) ?? throw new InvalidOperationException("Package is missing " + path);
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void Write(ZipArchive zip, string path, string text)
    {
        Find(zip, path)?.Delete();
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(text);
    }

    private static bool Exists(ZipArchive zip, string path) => Find(zip, path) is not null;

    private static ZipArchiveEntry? Find(ZipArchive zip, string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        foreach (var e in zip.Entries)
        {
            if (e.FullName.Replace('\\', '/').TrimStart('/').Equals(path, StringComparison.OrdinalIgnoreCase))
                return e;
        }
        return null;
    }
}
