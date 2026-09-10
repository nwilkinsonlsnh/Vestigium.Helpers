using ClosedXML.Excel;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using CxChartKind = Vestigium.Helpers.ClosedXml.ChartKind;
using CxChartSeries = Vestigium.Helpers.ClosedXml.ChartSeries;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ClosedXmlBranchTests
{
    [Fact]
    public void ExcelNames_sanitize_empty_illegal_long_and_digit_table()
    {
        Assert.Equal("Sheet1", ExcelNames.Sanitize(null));
        Assert.Equal("Sheet1", ExcelNames.Sanitize("   "));
        Assert.Equal("Sheet1", ExcelNames.Sanitize("'''"));
        Assert.Equal("a_b_c", ExcelNames.Sanitize("a/b*c"));
        var longName = new string('N', 40);
        Assert.Equal(31, ExcelNames.Sanitize(longName).Length);

        Assert.Equal("T_123abc", ExcelNames.SanitizeTable("123abc", "Data"));
        Assert.Equal("T_", ExcelNames.SanitizeTable("***", "Data")[..2]);
        Assert.Equal("Data", ExcelNames.SanitizeTable("   ", "Data"));
        var huge = ExcelNames.SanitizeTable(new string('A', 250), "Data");
        Assert.True(huge.Length <= 200);

        Assert.Equal("Data", ExcelNames.SanitizeDefinedName("123bad"));
        Assert.Equal("A_B.C", ExcelNames.SanitizeDefinedName("A B.C"));
        Assert.Equal(255, ExcelNames.SanitizeDefinedName(new string('Z', 300)).Length);
        Assert.Equal("___", ExcelNames.Sanitize("???"));
        Assert.Equal("'O''Brien'", ExcelNames.QuoteSheet("O'Brien"));
    }

    [Fact]
    public void ExcelNames_tab_color_column_letter_and_quote_sheet()
    {
        Assert.Null(ExcelNames.ParseTabColor(null));
        Assert.Null(ExcelNames.ParseTabColor(" "));
        Assert.Null(ExcelNames.ParseTabColor("zz00zz"));
        Assert.Null(ExcelNames.ParseTabColor("12345"));
        Assert.NotNull(ExcelNames.ParseTabColor("#3EC6FF"));
        Assert.NotNull(ExcelNames.ParseTabColor("FF3EC6FF"));
        Assert.NotNull(ExcelNames.ParseTabColor("3ec6ff"));

        Assert.Equal("A", ExcelNames.ColumnLetter(1));
        Assert.Equal("Z", ExcelNames.ColumnLetter(26));
        Assert.Equal("AA", ExcelNames.ColumnLetter(27));
        Assert.Equal("AB", ExcelNames.ColumnLetter(28));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelNames.ColumnLetter(0));

        Assert.Equal("Sheet1", ExcelNames.QuoteSheet("Sheet1"));
        Assert.Equal("'1Sheet'", ExcelNames.QuoteSheet("1Sheet"));
        Assert.Contains("'", ExcelNames.QuoteSheet("Sheet 1"));
        var a1 = ExcelNames.A1Range("Data", 1, 1, 2, 9);
        Assert.Contains("$A$1:$B$9", a1);
        Assert.Equal("AAA", ExcelNames.ColumnLetter(703));
    }

    [Fact]
    public void Table_styles_normalize_none_excel_name_and_unknown()
    {
        Assert.Equal(ExcelTableStyles.DefaultId, ExcelTableStyles.Normalize(null));
        Assert.Equal(ExcelTableStyles.DefaultId, ExcelTableStyles.Normalize("  "));
        Assert.Equal("None", ExcelTableStyles.Normalize("None"));
        Assert.Equal("None", ExcelTableStyles.Normalize("none"));
        Assert.Equal("Medium2", ExcelTableStyles.Normalize("TableStyleMedium2"));
        Assert.Equal("Medium2", ExcelTableStyles.Normalize(" TableStyleMedium2 "));
        Assert.Equal("Light1", ExcelTableStyles.Normalize("LIGHT1"));
        Assert.Equal("Dark11", ExcelTableStyles.Normalize("dark11"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("Medium99"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("123"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("Medium"));

        Assert.Equal("None", ExcelTableStyles.ToExcelName("None"));
        Assert.Equal("TableStyleMedium2", ExcelTableStyles.ToExcelName("Medium2"));
        Assert.False(ExcelTableStyles.IsKnown(null));
        Assert.False(ExcelTableStyles.IsKnown(" "));
        Assert.True(ExcelTableStyles.IsKnown("None"));
        Assert.True(ExcelTableStyles.IsKnown("Medium2"));
        Assert.NotNull(ExcelTableStyles.Resolve("Medium2"));
        Assert.NotEmpty(ExcelTableStyles.Ids);
        Assert.Contains("Light21", ExcelTableStyles.Ids);
        Assert.Equal(ExcelTableStyles.Ids, WorkbookHelper.TableStyles);
    }

    [Fact]
    public void Cell_reader_kinds_and_mixed_round_trip()
    {
        Assert.Equal("blank", SheetTable.CellKind(null));
        Assert.Equal("bool", SheetTable.CellKind(true));
        Assert.Equal("datetime", SheetTable.CellKind(DateTime.UtcNow));
        Assert.Equal("text", SheetTable.CellKind("hi"));
        Assert.Equal("number", SheetTable.CellKind(3.14));

        using var book = WorkbookHelper.Create("Mix", "ClosedXml");
        var when = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Unspecified);
        book.Sheet("Mix").WriteTable(SheetTable.Create(
            ["Blank", "Flag", "When", "Text", "Num"],
            [[null, true, when, "hello", 42], [false, false, when.AddDays(1), "bye", 0]]));
        var table = book.Sheet("Mix").ReadUsedRange();
        Assert.Equal(2, table.Rows.Count);
        Assert.Contains(table.Rows[0], v => v is bool);
        Assert.Contains(table.Rows[0], v => v is string);
        Assert.Contains(table.Rows[0], v => v is double or int or long or decimal);
    }

    [Fact]
    public void OpenOrCreate_mints_then_reopens_without_mixing_saveto()
    {
        var path = TempXlsx();
        using (var created = WorkbookHelper.OpenOrCreate(path, "First", "ClosedXml"))
        {
            created.Sheet("First").WriteTable(SheetTable.Create(["N"], [[1]]));
            created.AddChart(MakeChart("First", "N", CxChartKind.Column));
            created.SaveAs(path);
        }

        using (var reopened = WorkbookHelper.OpenOrCreate(path, "First", "ClosedXml"))
        {
            Assert.Contains("First", reopened.SheetNames);
            using var noCharts = new MemoryStream();
            reopened.IncludeCharts = false;
            reopened.SaveTo(noCharts);
            Assert.True(noCharts.Length > 0);
        }

        var minted = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlTests", Guid.NewGuid().ToString("N"), "new.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(minted)!);
        using var createdMissing = WorkbookHelper.OpenOrCreate(minted, "Sheet1", "ClosedXml");
        createdMissing.Dispose();
        Assert.True(File.Exists(minted));

        using var sameName = WorkbookHelper.Create("Sheet1", "ClosedXml");
        Assert.Contains("Sheet1", sameName.SheetNames);
        using var blank = WorkbookHelper.Create(null, "ClosedXml");
        Assert.NotEmpty(blank.SheetNames);
    }

    [Fact]
    public void SaveTo_embeds_every_chart_kind_and_two_series()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(SheetTable.Create(["A", "B"], [["x", 1], ["y", 2]]));
        foreach (var kind in new[] { CxChartKind.Column, CxChartKind.Bar, CxChartKind.Line, CxChartKind.Pie, CxChartKind.Scatter })
            book.AddChart(MakeChart("Data", kind.ToString(), kind, twoSeries: true));

        using var buffer = new MemoryStream();
        book.SaveTo(buffer);
        Assert.True(buffer.Length > 1000);
        Assert.Equal(5, book.Charts.Count);
    }

    [Fact]
    public void AddChart_rejects_blank_sheet_and_empty_series()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        Assert.Throws<ArgumentException>(() => book.AddChart(new SheetChart
        {
            Sheet = "  ",
            Title = "x",
            CategoriesFormula = "A1",
            Series =
            [
                new CxChartSeries { Name = "s", ValuesFormula = "B1", Values = [1] }
            ]
        }));
        Assert.Throws<ArgumentException>(() => book.AddChart(new SheetChart
        {
            Sheet = "Data",
            Title = "x",
            CategoriesFormula = "A1",
            Series = []
        }));
    }

    [Fact]
    public void WriteSeries_prefix_and_long_sheet_name()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 5), "seq");
        using (var book = WorkbookHelper.Create(new string('S', 40), "ClosedXml"))
        {
            book.TableStyle = "Light9";
            WorkbookHelper.WriteSeries(book, series, prefix: "Run", populationSize: 5, tableStyle: "Light1");
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        Assert.Contains(wb.Worksheets, w => w.Name.Contains("Run", StringComparison.Ordinal) || w.Name.Contains("Summary", StringComparison.Ordinal));
    }

    [Fact]
    public void WorkbookHelper_export_paths_open_template_and_remove_sheet()
    {
        Assert.Contains(Path.Combine("Vestigium", "Exports", "ClosedXml"), WorkbookHelper.DefaultExportDirectory("ClosedXml"));
        Assert.Throws<ArgumentException>(() => WorkbookHelper.DefaultExportDirectory(" "));
        var named = WorkbookHelper.NewExportPath("ClosedXml", "nathan");
        Assert.Contains("nathan-", Path.GetFileName(named));
        Assert.EndsWith(".xlsx", named);
        var stamped = WorkbookHelper.NewExportPath("ClosedXml");
        Assert.Contains("vestigium-ClosedXml-", Path.GetFileName(stamped));

        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Letter", "ClosedXml"))
        {
            book.Sheet("Letter").WriteTable(SheetTable.Create(["N"], [[1]]));
            book.AddSheet("Keep");
            Assert.True(book.RemoveSheet("Keep"));
            Assert.False(book.RemoveSheet("Missing"));
            Assert.Throws<InvalidOperationException>(() => book.RemoveSheet("Letter"));
            book.SaveAs(path);
        }

        using var opened = WorkbookHelper.OpenTemplate(path, "ClosedXml");
        Assert.Contains("Letter", opened.SheetNames);
        var saved = opened.Save();
        Assert.Equal(path, saved);
    }

    [Fact]
    public void Chart_packer_skips_unknown_sheet_and_empty_list()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(SheetTable.Create(["A", "B"], [["x", 1], ["y", 2]]));
        book.AddChart(MakeChart("NoSuchSheet", "ghost", CxChartKind.Pie));
        book.AddChart(MakeChart("Data", "pie", CxChartKind.Pie));
        book.AddChart(MakeChart("Data", "bar", CxChartKind.Bar));
        book.AddChart(MakeChart("Data", "line", CxChartKind.Line));
        using var buffer = new MemoryStream();
        book.SaveTo(buffer);
        var packed = ChartPacker.Embed(buffer.ToArray(), []);
        Assert.Equal(buffer.Length, packed.Length);

        using var noCharts = WorkbookHelper.Create("Data", "ClosedXml");
        noCharts.IncludeCharts = false;
        WorkbookHelper.WriteSeries(noCharts, NumericSeries.From(Enumerable.Range(1, 8), "seq"), prefix: "Run", populationSize: 8);
        noCharts.DefineName("Letter", "Data", 1, 1, 2, 2);
        Assert.Contains(noCharts.NamedRanges, n => n.Contains("Letter", StringComparison.OrdinalIgnoreCase));
        noCharts.WriteNamedRange("Letter", SheetTable.Create(["N"], [[9]]));
        noCharts.ReorderSheets(" ", "Missing", "Run Summary");
        noCharts.MoveSheet("Run Summary", 1);
        Assert.Throws<KeyNotFoundException>(() => noCharts.MoveSheet("Nope", 1));
        using var saved = new MemoryStream();
        noCharts.SaveTo(saved);
        Assert.True(saved.Length > 0);

        var timed = NumericSeries.FromObservations(
        [
            new Observation(1m, DateTimeOffset.UtcNow),
            new Observation(2m, DateTimeOffset.UtcNow.AddSeconds(1)),
            new Observation(3m, DateTimeOffset.UtcNow.AddSeconds(2)),
            new Observation(4m, DateTimeOffset.UtcNow.AddSeconds(3)),
            new Observation(5m, DateTimeOffset.UtcNow.AddSeconds(4))
        ], "timed");
        using var dated = WorkbookHelper.Create("Timed", "ClosedXml");
        dated.IncludeCharts = true;
        WorkbookHelper.WriteSeries(dated, timed, prefix: "T", populationSize: 5);
        using var datedBuf = new MemoryStream();
        dated.SaveTo(datedBuf);
        Assert.True(datedBuf.Length > 0);
    }

    [Fact]
    public void Session_merge_unique_name_timespan_and_chart_repack()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(SheetTable.Create(
            ["When", "Span", "Num"],
            [[new DateTime(2026, 9, 10, 8, 0, 0), TimeSpan.FromHours(1.5), 3.5]]));
        book.AddSheet("Data");
        Assert.Contains(book.SheetNames, n => n.StartsWith("Data", StringComparison.Ordinal));
        book.AddSheet("Extra");
        using var other = WorkbookHelper.Create("Other", "ClosedXml");
        other.Sheet("Other").WriteTable(SheetTable.Create(["N"], [[1]]));
        other.Sheet("Data").WriteTable(SheetTable.Create(["N"], [[9]]));
        book.Merge(other);
        Assert.Contains("Other", book.SheetNames);
        Assert.Throws<ArgumentException>(() => book.Merge(book));
        book.MoveSheet("Extra", 99);
        Assert.Throws<KeyNotFoundException>(() => book.WriteNamedRange("MissingName", SheetTable.Create(["N"], [[1]])));
        book.Dispose();
        book.Dispose();

        using var charts = WorkbookHelper.Create("Data", "ClosedXml");
        charts.Sheet("Data").WriteTable(SheetTable.Create(["A", "B"], [["x", 1], ["y", 2]]));
        charts.Sheet("Two").WriteTable(SheetTable.Create(["A", "B"], [["x", 3], ["y", 4]]));
        charts.AddChart(MakeChart("Data", "one", CxChartKind.Column));
        charts.AddChart(MakeChart("Two", "two", CxChartKind.Pie, twoSeries: false));
        charts.AddChart(new SheetChart
        {
            Sheet = "Data",
            Title = "plain",
            Kind = CxChartKind.Scatter,
            Color = null,
            CategoriesFormula = "Data!$A$2:$A$3",
            Categories = ["1", "2"],
            NumericCategories = true,
            Series =
            [
                new CxChartSeries { Name = "S", ValuesFormula = "Data!$B$2:$B$3", Values = [1, 2], Color = null }
            ]
        });
        using var first = new MemoryStream();
        charts.SaveTo(first);
        var packed = ChartPacker.Embed(first.ToArray(),
        [
            MakeChart("Data", "again", CxChartKind.Line),
            MakeChart("Two", "pie2", CxChartKind.Pie)
        ]);
        Assert.True(packed.Length > first.Length);

        using var dateFmt = WorkbookHelper.Create("Dates", "ClosedXml");
        dateFmt.Sheet("Dates").WriteTable(SheetTable.Create(
            ["Day"],
            [[new DateTime(2026, 1, 2)]],
            "Dates"),
            new SheetWriteOptions { DateFormat = "yyyy-mm-dd", CreateExcelTable = true });
        var table = dateFmt.Sheet("Dates").ReadUsedRange();
        Assert.NotEmpty(table.Rows);
        var path = TempXlsx();
        dateFmt.SaveAs(path);
        using var reopened = WorkbookHelper.Open(path, "ClosedXml");
        Assert.Contains("Dates", reopened.SheetNames);
    }

    [Fact]
    public void Chart_packer_rewrites_sheet_targets_and_no_table()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(
            SheetTable.Create(["A", "B"], [["x", 1], ["y", 2]]),
            new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
        book.AddChart(MakeChart("Data", "col", CxChartKind.Column));
        using var buffer = new MemoryStream();
        book.SaveTo(buffer);
        var original = buffer.ToArray();

        byte[] Rewrite(string target)
        {
            using var input = new MemoryStream(original);
            using var output = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(input, System.IO.Compression.ZipArchiveMode.Read))
            using (var dest = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in zip.Entries)
                {
                    var copy = dest.CreateEntry(entry.FullName);
                    using var src = entry.Open();
                    using var dst = copy.Open();
                    if (entry.FullName.Replace('\\', '/').EndsWith("workbook.xml.rels", StringComparison.OrdinalIgnoreCase))
                    {
                        var xml = System.Xml.Linq.XDocument.Load(src);
                        foreach (var e in xml.Root!.Elements())
                        {
                            var t = (string?)e.Attribute("Target");
                            if (t is not null && t.Contains("sheet", StringComparison.OrdinalIgnoreCase))
                                e.SetAttributeValue("Target", target);
                        }
                        xml.Save(dst);
                    }
                    else
                        src.CopyTo(dst);
                }
            }
            return ChartPacker.Embed(output.ToArray(), [MakeChart("Data", "repack", CxChartKind.Bar)]);
        }

        Assert.True(Rewrite("/xl/worksheets/sheet1.xml").Length > 0);
        Assert.True(Rewrite("xl/worksheets/sheet1.xml").Length > 0);
        Assert.True(Rewrite("worksheets/sheet1.xml").Length > 0);
    }

    static SheetChart MakeChart(string sheet, string title, CxChartKind kind, bool twoSeries = false)
    {
        var series = new List<CxChartSeries>
        {
            new() { Name = "S1", ValuesFormula = $"{sheet}!$B$2:$B$3", Values = [1, 2], Color = "1F4E79" }
        };
        if (twoSeries)
            series.Add(new CxChartSeries { Name = "S2", ValuesFormula = $"{sheet}!$B$2:$B$3", Values = [2, 1] });
        return new SheetChart
        {
            Sheet = sheet,
            Title = title,
            Kind = kind,
            Color = "#3EC6FF",
            CategoriesFormula = $"{sheet}!$A$2:$A$3",
            Categories = ["x", "y"],
            Series = series,
            NumericCategories = kind == CxChartKind.Scatter
        };
    }

    static string TempXlsx()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "book.xlsx");
    }
}
