using ClosedXML.Excel;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Owns one <see cref="XLWorkbook"/>. ClosedXML is not thread-safe — one session, one owner.
/// </summary>
public sealed class WorkbookSession : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly string _appId;
    private string? _path;
    private bool _disposed;
    private string _tableStyle = ExcelTableStyles.DefaultId;
    private readonly List<SheetChart> _charts = [];

    internal WorkbookSession(XLWorkbook workbook, string? path, string? appId, string? sessionId = null)
    {
        _workbook = workbook;
        _path = path;
        _appId = string.IsNullOrWhiteSpace(appId) ? ClosedXmlCatalog.AppId : appId.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? ClosedXmlLog.NewId() : sessionId.Trim();
        if (_workbook.Worksheets.Count == 0)
            _workbook.AddWorksheet("Sheet1");
        ClosedXmlLog.Information(
            ClosedXmlEvents.SessionCreated,
            ClosedXmlCatalog.Subcategories.Session,
            "session created",
            SessionId,
            ClosedXmlLog.Props(("sheets", _workbook.Worksheets.Count.ToString()), ("path", _path)),
            _appId);
    }

    public string AppId => _appId;
    public string SessionId { get; }
    public string? Path => _path;

    public string TableStyle
    {
        get => _tableStyle;
        set => _tableStyle = ExcelTableStyles.Normalize(value);
    }

    public bool IncludeCharts { get; set; } = true;
    public IReadOnlyList<SheetChart> Charts => _charts;

    public void AddChart(SheetChart chart)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(chart);
        Enter(ClosedXmlCatalog.Subcategories.Chart, "AddChart", chart.Sheet);
        if (string.IsNullOrWhiteSpace(chart.Sheet))
        {
            ClosedXmlLog.Error(ClosedXmlEvents.ChartRejected, ClosedXmlCatalog.Subcategories.Chart, "rejected chart",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("reason", "blank-sheet")), appId: _appId);
            throw new ArgumentException("A chart needs a sheet name.", nameof(chart));
        }
        if (chart.Series.Count == 0)
        {
            ClosedXmlLog.Error(ClosedXmlEvents.ChartRejected, ClosedXmlCatalog.Subcategories.Chart, "rejected chart",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("reason", "empty-series")), appId: _appId);
            throw new ArgumentException("A chart needs at least one series.", nameof(chart));
        }
        _charts.Add(chart);
        ClosedXmlLog.Information(ClosedXmlEvents.ChartQueued, ClosedXmlCatalog.Subcategories.Chart, "chart queued",
            SessionId, ClosedXmlLog.Props(("sheet", chart.Sheet), ("series", chart.Series.Count.ToString()), ("kind", chart.Kind.ToString())), _appId);
    }

    public IReadOnlyList<string> SheetNames =>
        _workbook.Worksheets.OrderBy(w => w.Position).Select(w => w.Name).ToArray();

    public IReadOnlyList<string> NamedRanges
    {
        get
        {
            ThrowIfDisposed();
            var names = new List<string>();
            foreach (var n in _workbook.DefinedNames)
                names.Add(n.Name);
            foreach (var ws in _workbook.Worksheets)
            {
                foreach (var n in ws.DefinedNames)
                {
                    if (!names.Contains(n.Name, StringComparer.OrdinalIgnoreCase))
                        names.Add(n.Name);
                }
            }
            return names;
        }
    }

    public void MoveSheet(string name, int position)
    {
        ThrowIfDisposed();
        Enter(ClosedXmlCatalog.Subcategories.Sheet, "MoveSheet", name);
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
        {
            ClosedXmlLog.Error(ClosedXmlEvents.SheetRejected, ClosedXmlCatalog.Subcategories.Sheet, "rejected sheet",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("sheet", safe), ("reason", "missing")), appId: _appId);
            throw new KeyNotFoundException($"Sheet '{safe}' was not found.");
        }
        var max = _workbook.Worksheets.Count;
        ws.Position = position > max ? max : position;
    }

    public void ReorderSheets(params string[] names)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(names);
        Enter(ClosedXmlCatalog.Subcategories.Sheet, "ReorderSheets", names.Length.ToString());
        var position = 1;
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var safe = ExcelNames.Sanitize(name);
            if (_workbook.TryGetWorksheet(safe, out var ws))
            {
                ws.Position = position;
                position++;
            }
        }
    }

    public void DefineName(string name, string sheet, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        ThrowIfDisposed();
        Enter(ClosedXmlCatalog.Subcategories.Sheet, "DefineName", name);
        var safe = ExcelNames.SanitizeDefinedName(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstRow, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstColumn, 1);
        if (lastRow < firstRow || lastColumn < firstColumn)
            throw new ArgumentException("Last cell must be at or below the origin.", nameof(lastRow));
        var ws = Sheet(sheet).Worksheet;
        var range = ws.Range(firstRow, firstColumn, lastRow, lastColumn);
        DropDefinedName(safe);
        _workbook.DefinedNames.Add(safe, range);
    }

    public void WriteNamedRange(string name, SheetTable table, SheetWriteOptions? options = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        Enter(ClosedXmlCatalog.Subcategories.Sheet, "WriteNamedRange", name);
        var defined = ResolveName(name);
        var range = defined.Ranges.FirstOrDefault();
        if (range is null)
        {
            ClosedXmlLog.Error(ClosedXmlEvents.SheetRejected, ClosedXmlCatalog.Subcategories.Sheet, "rejected sheet",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("name", defined.Name), ("reason", "empty-range")), appId: _appId);
            throw new InvalidOperationException($"Named range '{defined.Name}' has no cells.");
        }
        var addr = range.RangeAddress;
        var firstRow = addr.FirstAddress.RowNumber;
        var firstCol = addr.FirstAddress.ColumnNumber;
        var sheet = new SheetSession(this, range.Worksheet);
        var opts = options ?? SheetWriteOptions.Letterhead;
        sheet.WriteAt(firstRow, firstCol, table, opts);

        var colCount = table.Headers.Count;
        foreach (var row in table.Rows)
            colCount = Math.Max(colCount, row.Count);
        var lastRow = firstRow + (opts.HasHeaderRow ? table.Rows.Count : Math.Max(0, table.Rows.Count - 1));
        if (lastRow < firstRow)
            lastRow = firstRow;
        var lastCol = firstCol + Math.Max(1, colCount) - 1;
        var grown = range.Worksheet.Range(firstRow, firstCol, lastRow, lastCol);
        var kept = defined.Name;
        defined.Delete();
        _workbook.DefinedNames.Add(kept, grown);
    }

    public void AddPicture(string sheet, string imagePath, int row, int column, int widthPx = 160, int heightPx = 48, string? name = null)
        => Sheet(sheet).AddPicture(imagePath, row, column, widthPx, heightPx, name);

    public void AddPicture(string sheet, Stream image, int row, int column, int widthPx = 160, int heightPx = 48, string? name = null)
        => Sheet(sheet).AddPicture(image, row, column, widthPx, heightPx, name);

    public void Merge(WorkbookSession source)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(source);
        Enter(ClosedXmlCatalog.Subcategories.Session, "Merge", source.SessionId);
        if (ReferenceEquals(source, this) || ReferenceEquals(source.Workbook, _workbook))
        {
            ClosedXmlLog.Error(ClosedXmlEvents.SessionRejected, ClosedXmlCatalog.Subcategories.Session, "rejected session",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("reason", "self-merge")), appId: _appId);
            throw new ArgumentException("Cannot merge a workbook into itself.", nameof(source));
        }
        source.ThrowIfDisposed();
        try
        {
            var copied = 0;
            var appended = 0;
            foreach (var name in source.SheetNames)
            {
                if (_workbook.TryGetWorksheet(name, out _))
                {
                    var incoming = source.Sheet(name).ReadUsedRange();
                    var dest = Sheet(name);
                    var existing = dest.ReadUsedRange();
                    if (existing.Headers.Count == 0 && existing.Rows.Count == 0)
                        dest.WriteTable(incoming, new SheetWriteOptions { OperatorPrint = false, CreateExcelTable = false });
                    else
                        dest.AppendRows(incoming.Rows, new SheetWriteOptions { OperatorPrint = false });
                    appended++;
                }
                else
                {
                    source.Workbook.Worksheet(name).CopyTo(_workbook, name);
                    copied++;
                }
            }

            ClosedXmlLog.Information(ClosedXmlEvents.SessionMerged, ClosedXmlCatalog.Subcategories.Session, "session merged",
                SessionId, ClosedXmlLog.Props(("appended", appended.ToString()), ("copied", copied.ToString())), _appId);
        }
        catch (Exception ex)
        {
            ClosedXmlLog.Unexpected(ClosedXmlEvents.SessionThrown, ClosedXmlCatalog.Subcategories.Session, ex, SessionId, _appId);
            throw;
        }
    }

    public SheetSession Sheet(string name)
    {
        ThrowIfDisposed();
        var safe = ExcelNames.Sanitize(name);
        if (_workbook.TryGetWorksheet(safe, out var existing))
            return new SheetSession(this, existing);
        return AddSheet(safe);
    }

    public SheetSession AddSheet(string name)
    {
        ThrowIfDisposed();
        var safe = UniqueSheetName(ExcelNames.Sanitize(name));
        var ws = _workbook.AddWorksheet(safe);
        ClosedXmlLog.Debug(ClosedXmlEvents.SheetEnter, ClosedXmlCatalog.Subcategories.Sheet, "enter sheet",
            SessionId, ClosedXmlLog.Props(("via", "AddSheet"), ("sheet", safe)), _appId);
        return new SheetSession(this, ws);
    }

    public bool RemoveSheet(string name)
    {
        ThrowIfDisposed();
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
            return false;
        if (_workbook.Worksheets.Count <= 1)
        {
            ClosedXmlLog.Error(ClosedXmlEvents.SheetRejected, ClosedXmlCatalog.Subcategories.Sheet, "rejected sheet",
                correlationId: SessionId, properties: ClosedXmlLog.Props(("reason", "last-sheet")), appId: _appId);
            throw new InvalidOperationException("A workbook must keep at least one worksheet.");
        }
        ws.Delete();
        return true;
    }

    public string Save()
    {
        if (!string.IsNullOrWhiteSpace(_path))
            return SaveAs(_path);
        return SaveAs(WorkbookHelper.NewExportPath(_appId));
    }

    public string SaveAs(string path)
    {
        ThrowIfDisposed();
        var target = ClosedXmlLog.RequireNotBlank(path, nameof(path), ClosedXmlEvents.SessionRejected, ClosedXmlCatalog.Subcategories.Session);
        Enter(ClosedXmlCatalog.Subcategories.Session, "SaveAs", target);
        try
        {
            var dir = System.IO.Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            long bytes;
            if (IncludeCharts && _charts.Count > 0)
            {
                using var ms = new MemoryStream();
                _workbook.SaveAs(ms);
                var packed = PackCharts(ms.ToArray());
                File.WriteAllBytes(target, packed);
                bytes = packed.Length;
            }
            else
            {
                _workbook.SaveAs(target);
                bytes = new FileInfo(target).Length;
            }
            _path = target;
            ClosedXmlLog.Information(
                ClosedXmlEvents.SessionSaved,
                ClosedXmlCatalog.Subcategories.Session,
                "session saved",
                SessionId,
                ClosedXmlLog.Props(
                    ("path", target),
                    ("sheets", _workbook.Worksheets.Count.ToString()),
                    ("charts", (IncludeCharts ? _charts.Count : 0).ToString()),
                    ("bytes", bytes.ToString())),
                _appId);
            return target;
        }
        catch (Exception ex)
        {
            ClosedXmlLog.Unexpected(ClosedXmlEvents.SessionThrown, ClosedXmlCatalog.Subcategories.Session, ex, SessionId, _appId);
            throw;
        }
    }

    public void SaveTo(Stream stream)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(stream);
        Enter(ClosedXmlCatalog.Subcategories.Session, "SaveTo", null);
        if (IncludeCharts && _charts.Count > 0)
        {
            using var ms = new MemoryStream();
            _workbook.SaveAs(ms);
            var packed = PackCharts(ms.ToArray());
            stream.Write(packed, 0, packed.Length);
            return;
        }
        _workbook.SaveAs(stream);
    }

    internal void SetSheetPosition(string name, int position) => MoveSheet(name, position);
    internal XLWorkbook Workbook => _workbook;

    internal void Enter(string subcategory, string method, string? detail)
    {
        var eventId = subcategory == ClosedXmlCatalog.Subcategories.Sheet
            ? ClosedXmlEvents.SheetEnter
            : ClosedXmlEvents.SessionEnter;
        ClosedXmlLog.Debug(eventId, subcategory, subcategory == ClosedXmlCatalog.Subcategories.Sheet ? "enter sheet" : "enter session",
            SessionId, ClosedXmlLog.Props(("via", method), ("detail", detail)), _appId);
    }

    internal void ThrowIfDisposed() => ClosedXmlLog.ThrowIfDisposed(_disposed, this);

    private byte[] PackCharts(byte[] xlsx)
    {
        var sheets = string.Join(",", _charts.Select(c => c.Sheet).Distinct(StringComparer.OrdinalIgnoreCase));
        try
        {
            var bytes = ChartPacker.Embed(xlsx, _charts);
            ClosedXmlLog.Information(
                ClosedXmlEvents.ChartsEmbedded,
                ClosedXmlCatalog.Subcategories.Chart,
                "charts embedded",
                SessionId,
                ClosedXmlLog.Props(("count", _charts.Count.ToString()), ("sheets", sheets), ("bytes", bytes.Length.ToString())),
                _appId);
            return bytes;
        }
        catch (Exception ex)
        {
            ClosedXmlLog.Error(
                ClosedXmlEvents.ChartPackFailed,
                ClosedXmlCatalog.Subcategories.Chart,
                "chart pack failed",
                ex,
                SessionId,
                ClosedXmlLog.Props(("count", _charts.Count.ToString()), ("sheets", sheets)),
                _appId);
            throw;
        }
    }

    private IXLDefinedName ResolveName(string name)
    {
        var safe = ExcelNames.SanitizeDefinedName(name);
        foreach (var n in _workbook.DefinedNames)
        {
            if (string.Equals(n.Name, safe, StringComparison.OrdinalIgnoreCase))
                return n;
        }

        foreach (var ws in _workbook.Worksheets)
        {
            foreach (var n in ws.DefinedNames)
            {
                if (string.Equals(n.Name, safe, StringComparison.OrdinalIgnoreCase))
                    return n;
            }
        }

        ClosedXmlLog.Error(ClosedXmlEvents.SheetRejected, ClosedXmlCatalog.Subcategories.Sheet, "rejected sheet",
            correlationId: SessionId, properties: ClosedXmlLog.Props(("name", safe), ("reason", "missing-name")), appId: _appId);
        throw new KeyNotFoundException($"Named range '{safe}' was not found.");
    }

    private void DropDefinedName(string name)
    {
        foreach (var n in _workbook.DefinedNames.ToArray())
        {
            if (string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase))
                n.Delete();
        }

        foreach (var ws in _workbook.Worksheets)
        {
            foreach (var n in ws.DefinedNames.ToArray())
            {
                if (string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase))
                    n.Delete();
            }
        }
    }

    private string UniqueSheetName(string name)
    {
        if (!_workbook.TryGetWorksheet(name, out _))
            return name;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = ExcelNames.Sanitize($"{name} {i}");
            if (!_workbook.TryGetWorksheet(candidate, out _))
                return candidate;
        }

        ClosedXmlLog.Error(ClosedXmlEvents.SheetRejected, ClosedXmlCatalog.Subcategories.Sheet, "rejected sheet",
            correlationId: SessionId, properties: ClosedXmlLog.Props(("reason", "unique-name")), appId: _appId);
        throw new InvalidOperationException("Could not allocate a unique sheet name.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _workbook.Dispose();
    }
}
