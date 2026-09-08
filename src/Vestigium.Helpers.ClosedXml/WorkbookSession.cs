using ClosedXML.Excel;
using Vestigium.Helpers;
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
        _appId = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.ClosedXml : appId.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? HelperLog.NewId() : sessionId.Trim();
        if (_workbook.Worksheets.Count == 0)
            _workbook.AddWorksheet("Sheet1");
        HelperLog.Information(
            _appId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"created session={SessionId} sheets={_workbook.Worksheets.Count} path={_path ?? "(new)"}");
    }

    public string AppId => _appId;

    public string SessionId { get; }

    public string? Path => _path;

    /// <summary>Excel table style applied when a sheet does not override it. Default Medium2.</summary>
    public string TableStyle
    {
        get => _tableStyle;
        set => _tableStyle = ExcelTableStyles.Normalize(value);
    }

    /// <summary>
    /// When true, queued <see cref="SheetChart"/> specs are written as native Excel
    /// chart parts on save. ClosedXML itself does not author charts.
    /// </summary>
    public bool IncludeCharts { get; set; } = true;

    public IReadOnlyList<SheetChart> Charts => _charts;

    public void AddChart(SheetChart chart)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(chart, nameof(chart));
        using var scope = Trace(HelperLog.Subcategories.Chart, "AddChart", $"sheet={chart.Sheet}");
        HelperGuard.Require(!string.IsNullOrWhiteSpace(chart.Sheet), nameof(chart), "A chart needs a sheet name.");
        HelperGuard.Require(chart.Series.Count > 0, nameof(chart), "A chart needs at least one series.");
        _charts.Add(chart);
        HelperLog.Information(
            _appId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Chart,
            $"queued chart sheet={chart.Sheet} series={chart.Series.Count} kind={chart.Kind} session={SessionId}");
    }

    public IReadOnlyList<string> SheetNames =>
        _workbook.Worksheets.OrderBy(w => w.Position).Select(w => w.Name).ToArray();

    /// <summary>Workbook- and sheet-scoped defined names, letterhead first.</summary>
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

    /// <summary>1-based Excel tab position. Names are sanitized the same way as <see cref="Sheet"/>.</summary>
    public void MoveSheet(string name, int position)
    {
        ThrowIfDisposed();
        using var scope = Trace(HelperLog.Subcategories.Sheet, "MoveSheet", $"name={name} position={position}");
        HelperGuard.InRange(position, 1, nameof(position));
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
        {
            HelperLog.Reject($"Sheet '{safe}' was not found");
            throw new KeyNotFoundException($"Sheet '{safe}' was not found.");
        }
        var max = _workbook.Worksheets.Count;
        ws.Position = position > max ? max : position;
    }

    /// <summary>
    /// Puts the named sheets first, in this order. Unknown names are ignored.
    /// Sheets not listed keep their relative order after the named ones.
    /// </summary>
    public void ReorderSheets(params string[] names)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(names, nameof(names));
        using var scope = Trace(HelperLog.Subcategories.Sheet, "ReorderSheets", $"count={names.Length}");
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

    /// <summary>Workbook-scoped defined name over a rectangular range. Used by letterhead fill.</summary>
    public void DefineName(string name, string sheet, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        ThrowIfDisposed();
        using var scope = Trace(HelperLog.Subcategories.Sheet, "DefineName", $"name={name} sheet={sheet}");
        var safe = ExcelNames.SanitizeDefinedName(name);
        HelperGuard.InRange(firstRow, 1, nameof(firstRow));
        HelperGuard.InRange(firstColumn, 1, nameof(firstColumn));
        HelperGuard.Require(lastRow >= firstRow && lastColumn >= firstColumn, nameof(lastRow), "Last cell must be at or below the origin.");
        var ws = Sheet(sheet).Worksheet;
        var range = ws.Range(firstRow, firstColumn, lastRow, lastColumn);
        DropDefinedName(safe);
        _workbook.DefinedNames.Add(safe, range);
        HelperLog.Information(
            _appId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Sheet,
            $"defined name={safe} sheet={ws.Name} session={SessionId}");
    }

    /// <summary>
    /// Write a table at the origin of a caller-defined name. Letterhead cells outside
    /// that range are not cleared. Missing names throw <see cref="KeyNotFoundException"/>.
    /// </summary>
    public void WriteNamedRange(string name, SheetTable table, SheetWriteOptions? options = null)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(table, nameof(table));
        using var scope = Trace(HelperLog.Subcategories.Sheet, "WriteNamedRange", $"name={name} rows={table.Rows.Count}");
        var defined = ResolveName(name);
        var range = defined.Ranges.FirstOrDefault();
        if (range is null)
        {
            HelperLog.Reject($"Named range '{defined.Name}' has no cells.");
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

    /// <summary>
    /// Append-only merge by sheet name. Matching sheets get source data rows appended.
    /// Unknown sheets are copied in full. Existing target sheets are never deleted.
    /// </summary>
    public void Merge(WorkbookSession source)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(source, nameof(source));
        using var scope = Trace(HelperLog.Subcategories.Session, "Merge", $"from={source.SessionId}");
        HelperGuard.Require(
            !ReferenceEquals(source, this) && !ReferenceEquals(source.Workbook, _workbook),
            nameof(source),
            "Cannot merge a workbook into itself.");
        source.ThrowIfDisposed();

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

        HelperLog.Information(
            _appId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"Merged sheets appended={appended} copied={copied} total={_workbook.Worksheets.Count} session={SessionId}");
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
        HelperLog.Debug(
            _appId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Sheet,
            $"added sheet={safe} session={SessionId}");
        return new SheetSession(this, ws);
    }

    public bool RemoveSheet(string name)
    {
        ThrowIfDisposed();
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
            return false;
        HelperGuard.RequireState(_workbook.Worksheets.Count > 1, "A workbook must keep at least one worksheet.");
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
        using var scope = Trace(HelperLog.Subcategories.Session, "SaveAs", $"path={path}");
        var target = HelperGuard.NotBlank(path, nameof(path));
        try
        {
            var dir = System.IO.Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            if (IncludeCharts && _charts.Count > 0)
            {
                using var ms = new MemoryStream();
                _workbook.SaveAs(ms);
                var bytes = ChartPacker.Embed(ms.ToArray(), _charts);
                File.WriteAllBytes(target, bytes);
            }
            else
            {
                _workbook.SaveAs(target);
            }
            _path = target;
            HelperLog.Information(
                _appId,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Session,
                $"Saved workbook path={target} sheets={_workbook.Worksheets.Count} charts={(IncludeCharts ? _charts.Count : 0)} session={SessionId}");
            HelperLog.Exit(_appId, HelperLog.Subcategories.Session, "SaveAs", $"path={target} session={SessionId}");
            return target;
        }
        catch (Exception ex) when (ex is not ArgumentException and not ObjectDisposedException)
        {
            HelperLog.Reject(_appId, HelperLog.Subcategories.Session, "SaveAs", $"path={target} session={SessionId}", SessionId, ex);
            throw;
        }
    }

    public void SaveTo(Stream stream)
    {
        ThrowIfDisposed();
        using var scope = Trace(HelperLog.Subcategories.Session, "SaveTo");
        HelperGuard.NotNull(stream, nameof(stream));
        if (IncludeCharts && _charts.Count > 0)
        {
            using var ms = new MemoryStream();
            _workbook.SaveAs(ms);
            var bytes = ChartPacker.Embed(ms.ToArray(), _charts);
            stream.Write(bytes, 0, bytes.Length);
            return;
        }
        _workbook.SaveAs(stream);
    }

    internal void SetSheetPosition(string name, int position) => MoveSheet(name, position);

    internal XLWorkbook Workbook => _workbook;

    internal IDisposable Trace(string subcategory, string method, string? detail = null)
    {
        var text = string.IsNullOrWhiteSpace(detail)
            ? $"session={SessionId}"
            : $"{detail} session={SessionId}";
        return HelperLog.Begin(_appId, subcategory, method, text, SessionId);
    }

    internal void ThrowIfDisposed() => HelperGuard.NotDisposed(_disposed, this);

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

        HelperLog.Reject(_appId, HelperLog.Subcategories.Sheet, "ResolveName", $"named range '{safe}' was not found session={SessionId}", SessionId);
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

        HelperLog.Reject(_appId, HelperLog.Subcategories.Sheet, "AddSheet", $"could not allocate a unique sheet name session={SessionId}", SessionId);
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
