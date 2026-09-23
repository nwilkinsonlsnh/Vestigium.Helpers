using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// Owns one in-memory table and an optional path. CSV is not thread-safe — one session, one owner.
/// </summary>
public sealed class CsvSession : IDisposable
{
    private CsvTable _table = CsvTable.Empty();
    private bool _disposed;
    private bool _hasTable;

    internal CsvSession(CsvTable? table, string? path, string appId, string sessionId, CsvOptions options)
    {
        Path = path;
        AppId = appId;
        SessionId = sessionId;
        Options = options;
        if (table is not null)
        {
            _table = table;
            _hasTable = true;
        }

        CsvLog.Information(
            CsvEvents.SessionCreated,
            CsvCatalog.Subcategories.Session,
            "session created",
            SessionId,
            CsvLog.Props(
                ("cols", _table.Headers.Count.ToString()),
                ("rows", _table.Rows.Count.ToString()),
                ("path", Path),
                ("delimiter", Options.DescribeDelimiter())),
            AppId);
    }

    public string AppId { get; }
    public string SessionId { get; }
    public string? Path { get; private set; }

    public CsvOptions Options { get; }

    public void WriteTable(CsvTable table)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            SessionId, CsvLog.Props(("via", "WriteTable"), ("rows", table.Rows.Count.ToString())), AppId);
        try
        {
            _ = CsvCodec.WriteText(table, Options);
            _table = table;
            _hasTable = true;
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Session, ex, SessionId, AppId);
            throw;
        }
    }

    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(rows);
        if (!_hasTable)
        {
            CsvLog.Error(CsvEvents.WriteRejected, CsvCatalog.Subcategories.Session, "rejected write",
                correlationId: SessionId, properties: CsvLog.Props(("reason", "no-table")), appId: AppId);
            throw new InvalidOperationException("Write a table before appending rows.");
        }

        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            SessionId, CsvLog.Props(("via", "AppendRows")), AppId);
        try
        {
            var extra = rows as IReadOnlyList<IReadOnlyList<object?>> ?? [.. rows];
            var combined = new List<IReadOnlyList<object?>>(_table.Rows.Count + extra.Count);
            combined.AddRange(_table.Rows);
            combined.AddRange(extra);
            _table = new CsvTable
            {
                Headers = _table.Headers,
                Rows = combined,
                Name = _table.Name
            };
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Session, ex, SessionId, AppId);
            throw;
        }
    }

    public CsvTable Read()
    {
        ThrowIfDisposed();
        return _table;
    }

    public string Save()
    {
        ThrowIfDisposed();
        return SaveAs(!string.IsNullOrWhiteSpace(Path) ? Path : CsvHelper.NewExportPath(AppId));
    }

    public string SaveAs(string path)
    {
        ThrowIfDisposed();
        var target = CsvLog.RequireNotBlank(path, nameof(path));
        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            SessionId, CsvLog.Props(("via", "SaveAs"), ("path", target)), AppId);
        try
        {
            var parent = System.IO.Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            var bytes = CsvCodec.WriteBytes(_hasTable ? _table : CsvTable.Empty(), Options);
            File.WriteAllBytes(target, bytes);
            Path = target;
            CsvLog.Information(
                CsvEvents.SessionSaved,
                CsvCatalog.Subcategories.Session,
                "session saved",
                SessionId,
                CsvLog.Props(("path", target), ("bytes", bytes.Length.ToString()), ("rows", _table.Rows.Count.ToString())),
                AppId);
            return target;
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Session, ex, SessionId, AppId);
            throw;
        }
    }

    public void WriteTo(Stream stream)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = CsvCodec.WriteBytes(_hasTable ? _table : CsvTable.Empty(), Options);
        stream.Write(bytes, 0, bytes.Length);
    }

    public void Dispose() => _disposed = true;

    internal void ThrowIfDisposed() => CsvLog.ThrowIfDisposed(_disposed, this);
}
