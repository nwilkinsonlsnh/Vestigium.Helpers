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

    internal WorkbookSession(XLWorkbook workbook, string? path, string? appId)
    {
        _workbook = workbook;
        _path = path;
        _appId = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.ClosedXml : appId.Trim();
        if (_workbook.Worksheets.Count == 0)
            _workbook.AddWorksheet("Sheet1");
    }

    public string AppId => _appId;

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
        ArgumentNullException.ThrowIfNull(chart);
        if (string.IsNullOrWhiteSpace(chart.Sheet))
            throw new ArgumentException("A chart needs a sheet name.", nameof(chart));
        if (chart.Series.Count == 0)
            throw new ArgumentException("A chart needs at least one series.", nameof(chart));
        _charts.Add(chart);
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
        if (position < 1)
            throw new ArgumentOutOfRangeException(nameof(position), "Sheet position is 1-based.");
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
            throw new KeyNotFoundException($"Sheet '{safe}' was not found.");
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
        ArgumentNullException.ThrowIfNull(names);
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
        var safe = ExcelNames.SanitizeDefinedName(name);
        if (firstRow < 1 || firstColumn < 1)
            throw new ArgumentOutOfRangeException(nameof(firstRow), "Range origin is 1-based.");
        if (lastRow < firstRow || lastColumn < firstColumn)
            throw new ArgumentOutOfRangeException(nameof(lastRow), "Last cell must be at or below the origin.");
        var ws = Sheet(sheet).Worksheet;
        var range = ws.Range(firstRow, firstColumn, lastRow, lastColumn);
        DropDefinedName(safe);
        _workbook.DefinedNames.Add(safe, range);
    }

    /// <summary>
    /// Write a table at the origin of a caller-defined name. Letterhead cells outside
    /// that range are not cleared. Missing names throw <see cref="KeyNotFoundException"/>.
    /// </summary>
    public void WriteNamedRange(string name, SheetTable table, SheetWriteOptions? options = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        var defined = ResolveName(name);
        var range = defined.Ranges.FirstOrDefault()
            ?? throw new InvalidOperationException($"Named range '{defined.Name}' has no cells.");
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
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(source, this) || ReferenceEquals(source.Workbook, _workbook))
            throw new ArgumentException("Cannot merge a workbook into itself.", nameof(source));
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
            HelperLog.AppIds.ClosedXml,
            $"Merged sheets appended={appended} copied={copied} total={_workbook.Worksheets.Count}");
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
        return new SheetSession(this, ws);
    }

    public bool RemoveSheet(string name)
    {
        ThrowIfDisposed();
        var safe = ExcelNames.Sanitize(name);
        if (!_workbook.TryGetWorksheet(safe, out var ws))
            return false;
        if (_workbook.Worksheets.Count == 1)
            throw new InvalidOperationException("A workbook must keep at least one worksheet.");
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
                HelperLog.AppIds.ClosedXml,
                $"Saved workbook path={target} sheets={_workbook.Worksheets.Count} charts={(IncludeCharts ? _charts.Count : 0)}");
            return target;
        }
        catch (Exception ex)
        {
            HelperLog.Error(
                _appId,
                VestigiumStatus.Failed,
                HelperLog.AppIds.ClosedXml,
                $"Save failed path={target}",
                ex);
            throw;
        }
    }

    public void SaveTo(Stream stream)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(stream);
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

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

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
