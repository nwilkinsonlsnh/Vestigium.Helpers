using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// Owns one working tree and one committed tree. Not thread-safe — one session, one owner.
/// Phase 4 adds JSONL (one RFC 8259 value per line) with full-file rewrite on Save.
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

    public string SessionId { get; }

    public string? Path { get; private set; }

    public JsonDocumentKind Kind { get; }

    internal JsonSessionOptions Options { get; }

    public bool HasUncommittedWork => !JsonNode.DeepEquals(_working, _committed);

    public bool HasUnsavedCommit => !JsonNode.DeepEquals(_committed, _saved);

    public void Snapshot()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Snapshot, "Snapshot", $"session={SessionId}", SessionId);
        _snapshot = _committed.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Snapshot, $"snapshot session={SessionId}");
    }

    public bool TryGet<T>(string path, out T? value)
    {
        ThrowIfDisposed();
        value = default;
        var parsed = JsonPath.Parse(path);
        if (!parsed.TryEvaluate(_working, out var node))
            return false;
        return TryConvert(node, out value);
    }

    public T Get<T>(string path)
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "Get", "path=" + path, SessionId);
        var parsed = JsonPath.Parse(path);
        if (!parsed.TryEvaluate(_working, out var node))
        {
            HelperLog.Reject($"path not found path={parsed.Original}");
            throw new KeyNotFoundException($"JSON path was not found: {parsed.Original}.");
        }

        if (!TryConvert<T>(node, out var value))
        {
            HelperLog.Reject($"path type mismatch path={parsed.Original} type={typeof(T).Name}");
            throw new InvalidOperationException($"JSON path could not be read as {typeof(T).Name}.");
        }

        return value!;
    }

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

    public JsonNode? Record(int index)
    {
        ThrowIfDisposed();
        RequireJsonl("Record");
        if (index < 0)
        {
            HelperLog.Reject($"index={index} is below 0");
            throw new ArgumentOutOfRangeException(nameof(index), "Record index must be at least 0.");
        }

        var array = WorkingArray();
        if (index >= array.Count)
            return null;
        return array[index]?.DeepClone();
    }

    public int RecordCount
    {
        get
        {
            ThrowIfDisposed();
            RequireJsonl("RecordCount");
            return WorkingArray().Count;
        }
    }

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

    public void Commit()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Commit", $"session={SessionId}", SessionId);
        _committed = _working.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Commit session={SessionId}");
    }

    public void Revert()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Revert", $"session={SessionId}", SessionId);
        _working = (_snapshot ?? _committed).DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Revert session={SessionId}");
    }

    public void Cancel()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Cancel", $"session={SessionId}", SessionId);
        _working = _committed.DeepClone();
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Cancel session={SessionId}");
    }

    public string Save()
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(Path))
            return SaveAs(JsonHelper.NewExportPath(kind: Kind), Options.Collision);
        return WriteTree(_committed, Path, replaceInPlace: true, Options.Collision, working: false, "Save");
    }

    public string SaveWorking()
    {
        ThrowIfDisposed();
        var target = string.IsNullOrWhiteSpace(Path) ? JsonHelper.NewExportPath(kind: Kind) : Path;
        var replace = JsonIO.SamePath(Path, target);
        return WriteTree(_working, target, replace, Options.Collision, working: true, "SaveWorking");
    }

    public string SaveAs(string path, JsonCollision collision = JsonCollision.Fail)
    {
        ThrowIfDisposed();
        var target = System.IO.Path.GetFullPath(HelperGuard.NotBlank(path, nameof(path)));
        var replace = JsonIO.SamePath(Path, target);
        return WriteTree(_committed, target, replace, collision, working: false, "SaveAs");
    }

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
            JsonIO.RejectCollision(path, collision, replaceInPlace);
            var bytes = Kind == JsonDocumentKind.Jsonl
                ? JsonIO.WriteJsonl(path, AsArray(tree), Options.AtomicWrite)
                : JsonIO.Write(path, tree, indented: true, Options.AtomicWrite);
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
        HelperLog.Reject($"{method} is JSONL only");
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
            if (default(T) is null)
            {
                value = default;
                return true;
            }

            return false;
        }

        if (typeof(JsonNode).IsAssignableFrom(typeof(T)))
        {
            var clone = node.DeepClone();
            if (clone is T typed)
            {
                value = typed;
                return true;
            }

            return false;
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
