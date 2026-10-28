using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// One working tree and one committed tree. Not thread-safe: one session, one owner.
/// Get, TryGet, and Record do not write HelperLog. Mutations, snapshot, diff, commit, and save do.
/// </summary>
public sealed class JsonSession : IDisposable
{
    private const string App = HelperLog.AppIds.Json;

    private JsonNode _working;
    private JsonNode _committed;
    private JsonNode _saved;
    private JsonNode? _snapshot;
    private bool _disposed;

    internal JsonSession(JsonNode root, string? path, JsonDocumentKind kind, JsonSessionOptions options, string sessionId)
    {
        var source = HelperGuard.NotNull(root, nameof(root));
        _committed = source.DeepClone();
        _working = source.DeepClone();
        _saved = source.DeepClone();
        Path = path;
        Kind = kind;
        Options = options;
        SessionId = sessionId;
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"created session={SessionId} kind={Kind} path={Path ?? "(new)"}");
    }

    /// <summary>Opaque id written on session log lines.</summary>
    public string SessionId { get; }

    /// <summary>Full path after Open or the first Save. Null for an unsaved Create.</summary>
    public string? Path { get; private set; }

    /// <summary>JSON document or JSONL record list. Fixed for the life of the session.</summary>
    public JsonDocumentKind Kind { get; }

    internal JsonSessionOptions Options { get; }

    /// <summary>Working tree differs from the last Commit.</summary>
    public bool HasUncommittedWork => !JsonNode.DeepEquals(_working, _committed);

    /// <summary>Committed tree differs from the last successful Save.</summary>
    public bool HasUnsavedCommit => !JsonNode.DeepEquals(_committed, _saved);

    /// <summary>Copy the committed tree as the Diff / Revert baseline.</summary>
    public void Snapshot()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Snapshot, "Snapshot", $"session={SessionId}", SessionId);
        _snapshot = _committed.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Snapshot, $"snapshot session={SessionId}");
    }

    /// <summary>
    /// Read a path from the working tree. Returns false on a miss or type mismatch. Does not log.
    /// </summary>
    public bool TryGet<T>(string path, out T? value)
    {
        ThrowIfDisposed();
        value = default;
        var parsed = JsonPath.Parse(path);
        return parsed.TryEvaluate(_working, out var node) && TryConvert(node, out value);
    }

    /// <summary>
    /// Read a path from the working tree. Miss throws <see cref="KeyNotFoundException"/>.
    /// Type mismatch throws <see cref="InvalidOperationException"/>. Does not log.
    /// </summary>
    public T Get<T>(string path)
    {
        ThrowIfDisposed();
        var parsed = JsonPath.Parse(path);
        if (!parsed.TryEvaluate(_working, out var node))
            throw new KeyNotFoundException($"JSON path was not found: {parsed.Original}.");

        if (TryConvert<T>(node, out var value)) return value!;
        throw new InvalidOperationException($"JSON path could not be read as {typeof(T).Name}.");
    }

    /// <summary>Assign a path on the working tree. Logs the path spelling, never the value.</summary>
    public void Set(string path, object? value)
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "Set", "path=" + path, SessionId);
        try
        {
            var parsed = JsonPath.Parse(path);
            var node = JsonCodec.ToNode(value);
            parsed.Assign(ref _working, node);
            HelperLog.Information(
                App,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Query,
                $"Set path={parsed.Original} session={SessionId}");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>Append a cloned record on a JSONL session. JSON sessions throw.</summary>
    public void AppendRecord(JsonNode record)
    {
        ThrowIfDisposed();
        RequireJsonl("AppendRecord");
        var node = HelperGuard.NotNull(record, nameof(record));
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Jsonl, "AppendRecord", $"session={SessionId}", SessionId);
        var array = WorkingArray();
        array.Add(node.DeepClone());
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Jsonl,
            $"AppendRecord index={array.Count - 1} session={SessionId}");
    }

    /// <summary>
    /// Clone the record at <paramref name="index"/>, or null when the index is past the end.
    /// Negative index throws. Does not log.
    /// </summary>
    public JsonNode? Record(int index)
    {
        ThrowIfDisposed();
        RequireJsonl("Record");
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Record index must be at least 0.");

        var array = WorkingArray();
        return index >= array.Count ? null : array[index]?.DeepClone();
    }

    /// <summary>Number of records on a JSONL session. JSON sessions throw.</summary>
    public int RecordCount
    {
        get
        {
            ThrowIfDisposed();
            RequireJsonl("RecordCount");
            return WorkingArray().Count;
        }
    }

    /// <summary>Compare Snapshot-or-committed to working. Add, remove, replace only.</summary>
    public JsonPatch Diff()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Diff, "Diff", $"session={SessionId}", SessionId);
        var baseline = _snapshot ?? _committed;
        var patch = JsonPatch.Compare(baseline, _working);
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Diff,
            $"ops={patch.Count} session={SessionId}");
        return patch;
    }

    /// <summary>Copy working onto committed. Does not write disk.</summary>
    public void Commit()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Commit", $"session={SessionId}", SessionId);
        _committed = _working.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Commit session={SessionId}");
    }

    /// <summary>Replace working with Snapshot if present, otherwise committed.</summary>
    public void Revert()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Revert", $"session={SessionId}", SessionId);
        _working = (_snapshot ?? _committed).DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Revert session={SessionId}");
    }

    /// <summary>Replace working with committed. Does not touch Snapshot.</summary>
    public void Cancel()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Cancel", $"session={SessionId}", SessionId);
        _working = _committed.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Cancel session={SessionId}");
    }

    /// <summary>
    /// Write the committed tree to <see cref="Path"/>, or to a new export file when Path is unset.
    /// </summary>
    public string Save()
    {
        ThrowIfDisposed();
        return string.IsNullOrWhiteSpace(Path) ? SaveAs(JsonHelper.NewExportPath(kind: Kind), Options.Collision) : WriteTree(_committed, Path, replaceInPlace: true, Options.Collision, working: false, "Save");
    }

    /// <summary>
    /// Write the working tree without Commit. Logs a Save warning. Hosts treat this as an escape hatch.
    /// </summary>
    public string SaveWorking()
    {
        ThrowIfDisposed();
        var target = string.IsNullOrWhiteSpace(Path) ? JsonHelper.NewExportPath(kind: Kind) : Path;
        var replace = JsonIo.SamePath(Path, target);
        return WriteTree(_working, target, replace, Options.Collision, working: true, "SaveWorking");
    }

    /// <summary>Write the committed tree to <paramref name="path"/> and point the session at it.</summary>
    public string SaveAs(string path, JsonCollision collision = JsonCollision.Fail)
    {
        ThrowIfDisposed();
        var target = System.IO.Path.GetFullPath(HelperGuard.NotBlank(path, nameof(path)));
        var replace = JsonIo.SamePath(Path, target);
        return WriteTree(_committed, target, replace, collision, working: false, "SaveAs");
    }

    /// <summary>Marks the session disposed. Does not write disk.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"disposed session={SessionId}");
    }

    private string WriteTree(JsonNode tree, string path, bool replaceInPlace, JsonCollision collision, bool working, string method)
    {
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Save, method, $"path={path} session={SessionId}", SessionId);
        try
        {
            JsonIo.RejectCollision(path, collision, replaceInPlace);
            var bytes = Kind == JsonDocumentKind.Jsonl
                ? JsonIo.WriteJsonl(path, AsArray(tree), Options.AtomicWrite)
                : JsonIo.Write(path, tree, indented: true, Options.AtomicWrite);
            Path = path;
            _saved = tree.DeepClone();
            if (Kind == JsonDocumentKind.Jsonl)
            {
                HelperLog.Information(
                    App,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Jsonl,
                    $"rewrite path={path} records={AsArray(tree).Count} bytes={bytes} session={SessionId}");
            }
            if (working)
            {
                HelperLog.Warning(
                    App,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Save,
                    $"SaveWorking path={path} bytes={bytes} collision={collision} atomic={Options.AtomicWrite} session={SessionId}");
            }
            else
            {
                HelperLog.Information(
                    App,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Save,
                    $"Saved path={path} bytes={bytes} collision={collision} atomic={Options.AtomicWrite} session={SessionId}");
            }

            return path;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    private void ThrowIfDisposed() => HelperGuard.NotDisposed(_disposed, this);

    private void RequireJsonl(string method)
    {
        if (Kind == JsonDocumentKind.Jsonl)
            return;
        HelperLog.Reject(App, HelperLog.Subcategories.Jsonl, method, $"{method} is JSONL only", SessionId);
        throw new InvalidOperationException($"{method} is only valid on a JSONL session.");
    }

    private JsonArray WorkingArray() => AsArray(_working);

    private static JsonArray AsArray(JsonNode tree)
    {
        if (tree is JsonArray array)
            return array;
        HelperLog.Reject("jsonl root is not an array");
        throw new InvalidOperationException("A JSONL session root must be an array of records.");
    }

    private static bool TryConvert<T>(JsonNode? node, out T? value)
    {
        value = default;
        if (node is null)
        {
            if (default(T) is not null) return false;
            value = default;
            return true;

        }

        if (typeof(JsonNode).IsAssignableFrom(typeof(T)))
        {
            var clone = node.DeepClone();
            if (clone is not T typed) return false;
            value = typed;
            return true;

        }

        try
        {
            value = node.Deserialize<T>(JsonCodec.Read(null));
            return value is not null
                || default(T) is null
                || Nullable.GetUnderlyingType(typeof(T)) is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
