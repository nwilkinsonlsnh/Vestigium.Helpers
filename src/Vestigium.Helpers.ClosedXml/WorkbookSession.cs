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
            _workbook.SaveAs(target);
            _path = target;
            HelperLog.Information(
                _appId,
                VestigiumStatus.Success,
                HelperLog.AppIds.ClosedXml,
                $"Saved workbook path={target} sheets={_workbook.Worksheets.Count}");
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
        _workbook.SaveAs(stream);
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
        if (_disposed) return;
        _disposed = true;
        _workbook.Dispose();
    }
}
