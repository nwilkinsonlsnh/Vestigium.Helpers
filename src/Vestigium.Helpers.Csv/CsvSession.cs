using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// Owns one in-memory table and an optional path. CSV is not thread-safe — one session, one owner.
/// </summary>
public sealed class CsvSession : IDisposable
{
    private CsvTable _table = CsvTable.Empty();
    private string? _path;
    private bool _disposed;
    private bool _hasTable;

    internal CsvSession(CsvTable? table, string? path, string appId, string sessionId, CsvOptions options)
    {
        _path = path;
        AppId = appId;
        SessionId = sessionId;
        Options = options;
        if (table is not null)
        {
            _table = table;
            _hasTable = true;
        }

        HelperLog.Information(
            AppId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"created session={SessionId} cols={_table.Headers.Count} rows={_table.Rows.Count} path={_path ?? "(new)"} delimiter={Options.DescribeDelimiter()}");
    }

    public string AppId { get; }

    public string SessionId { get; }

    public string? Path => _path;

    public CsvOptions Options { get; }

    public void WriteTable(CsvTable table)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(table, nameof(table));
        using var scope = Trace("WriteTable", $"cols={table.Headers.Count} rows={table.Rows.Count}");
        try
        {
            HelperLog.Information(
                AppId,
                VestigiumStatus.Pending,
                HelperLog.Subcategories.Session,
                $"Write start session={SessionId} cols={table.Headers.Count} rows={table.Rows.Count} delimiter={Options.DescribeDelimiter()}");
            _ = CsvCodec.WriteText(table, Options);
            _table = table;
            _hasTable = true;
            HelperLog.Information(
                AppId,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Session,
                $"Table written session={SessionId} cols={table.Headers.Count} rows={table.Rows.Count}");
            HelperLog.Exit(AppId, HelperLog.Subcategories.Session, "WriteTable", $"session={SessionId}");
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(rows, nameof(rows));
        HelperGuard.RequireState(_hasTable, "Write a table before appending rows.");
        using var scope = Trace("AppendRows");
        try
        {
            var extra = rows as IReadOnlyList<IReadOnlyList<object?>> ?? rows.ToArray();
            var combined = new List<IReadOnlyList<object?>>(_table.Rows.Count + extra.Count);
            combined.AddRange(_table.Rows);
            combined.AddRange(extra);
            _table = new CsvTable
            {
                Headers = _table.Headers,
                Rows = combined,
                Name = _table.Name
            };
            HelperLog.Information(
                AppId,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Session,
                $"Appended {extra.Count} rows session={SessionId} total={_table.Rows.Count}");
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public CsvTable Read()
    {
        ThrowIfDisposed();
        using var scope = Trace("Read", $"cols={_table.Headers.Count} rows={_table.Rows.Count}");
        HelperLog.Information(
            AppId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"Table read session={SessionId} cols={_table.Headers.Count} rows={_table.Rows.Count} path={_path ?? "(memory)"}");
        return _table;
    }

    public string Save()
    {
        ThrowIfDisposed();
        if (!string.IsNullOrWhiteSpace(_path))
            return SaveAs(_path);
        return SaveAs(CsvHelper.NewExportPath(AppId));
    }

    public string SaveAs(string path)
    {
        ThrowIfDisposed();
        var target = HelperGuard.NotBlank(path, nameof(path));
        using var scope = Trace("SaveAs", $"path={target}");
        try
        {
            var parent = System.IO.Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            var bytes = CsvCodec.WriteBytes(_hasTable ? _table : CsvTable.Empty(), Options);
            File.WriteAllBytes(target, bytes);
            _path = target;
            HelperLog.Information(
                AppId,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Session,
                $"Saved path={target} bytes={bytes.Length} session={SessionId}");
            HelperLog.Exit(AppId, HelperLog.Subcategories.Session, "SaveAs", $"session={SessionId}");
            return target;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public void WriteTo(Stream stream)
    {
        ThrowIfDisposed();
        HelperGuard.NotNull(stream, nameof(stream));
        using var scope = Trace("WriteTo", "(stream)");
        var bytes = CsvCodec.WriteBytes(_hasTable ? _table : CsvTable.Empty(), Options);
        stream.Write(bytes, 0, bytes.Length);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    internal IDisposable Trace(string method, string? detail = null) =>
        HelperLog.Begin(AppId, HelperLog.Subcategories.Session, method, detail, SessionId);

    private void ThrowIfDisposed() => HelperGuard.NotDisposed(_disposed, this);
}
