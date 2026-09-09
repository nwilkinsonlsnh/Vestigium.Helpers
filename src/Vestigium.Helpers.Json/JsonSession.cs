using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// Owns one working tree and one committed tree. Not thread-safe — one session, one owner.
/// Phase 3 adds Open/Save/SaveAs with atomic replace. JSONL starts at Phase 4.
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
            return SaveAs(JsonHelper.NewExportPath(), Options.Collision);
        return WriteTree(_committed, Path, replaceInPlace: true, Options.Collision, working: false, "Save");
    }

    public string SaveWorking()
    {
        ThrowIfDisposed();
        var target = string.IsNullOrWhiteSpace(Path) ? JsonHelper.NewExportPath() : Path;
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
            var bytes = JsonIO.Write(path, tree, indented: true, Options.AtomicWrite);
            Path = path;
            _saved = tree.DeepClone();
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
