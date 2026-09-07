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
        _workbook.Worksheets.Select(w => w.Name).ToArray();

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

    internal void SetSheetPosition(string name, int position)
    {
        ThrowIfDisposed();
        var safe = ExcelNames.Sanitize(name);
        if (_workbook.TryGetWorksheet(safe, out var ws))
            ws.Position = position;
    }

    internal XLWorkbook Workbook => _workbook;

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

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
