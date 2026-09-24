using System.Text.Json.Nodes;

namespace Vestigium.Helpers.Json;

/// <summary>
/// RFC 6902 JSON Patch. Hosts render the operation list. HelperLog records the count, never values.
/// Diff only: add, remove, replace. No Apply.
/// </summary>
public sealed class JsonPatch
{
    private readonly JsonPatchOperation[] _operations;

    internal JsonPatch(IEnumerable<JsonPatchOperation> operations)
        => _operations = [.. operations];

    /// <summary>Operations in document order. Array removes are high index first.</summary>
    public IReadOnlyList<JsonPatchOperation> Operations => _operations;

    /// <summary>Number of operations.</summary>
    public int Count => _operations.Length;

    /// <summary>
    /// RFC 6902 array. <c>remove</c> omits <c>value</c>.
    /// </summary>
    public JsonArray ToJsonArray()
    {
        var array = new JsonArray();
        foreach (var op in _operations)
        {
            var item = new JsonObject
            {
                ["op"] = op.Op,
                ["path"] = op.Path
            };
            if (op.Op != "remove")
                item["value"] = op.Value?.DeepClone();
            array.Add(item);
        }

        return array;
    }

    /// <summary>Short count label for logs and tests.</summary>
    public override string ToString() => $"{Count} operation(s)";

    /// <summary>
    /// Diff <paramref name="from"/> to <paramref name="to"/>. Add, remove, replace only.
    /// Does not apply the patch.
    /// </summary>
    public static JsonPatch Compare(JsonNode? from, JsonNode? to)
    {
        var ops = new List<JsonPatchOperation>();
        DiffExisting(from, to, "", ops);
        return new JsonPatch(ops);
    }

    private static void DiffExisting(JsonNode? from, JsonNode? to, string pointer, List<JsonPatchOperation> ops)
    {
        if (JsonNode.DeepEquals(from, to))
            return;

        switch (from)
        {
            case JsonObject fromObj when to is JsonObject toObj:
                DiffObjects(fromObj, toObj, pointer, ops);
                return;
            case JsonArray fromArr when to is JsonArray toArr:
                DiffArrays(fromArr, toArr, pointer, ops);
                return;
            default:
                ops.Add(JsonPatchOperation.Replace(pointer, to?.DeepClone()));
                break;
        }
    }

    private static void DiffObjects(JsonObject from, JsonObject to, string pointer, List<JsonPatchOperation> ops)
    {
        var fromKeys = new SortedSet<string>(from.Select(p => p.Key), StringComparer.Ordinal);
        var toKeys = new SortedSet<string>(to.Select(p => p.Key), StringComparer.Ordinal);

        foreach (var key in fromKeys)
        {
            var child = Child(pointer, key);
            if (!toKeys.Contains(key))
            {
                ops.Add(JsonPatchOperation.Remove(child));
                continue;
            }

            from.TryGetPropertyValue(key, out var fromChild);
            to.TryGetPropertyValue(key, out var toChild);
            DiffExisting(fromChild, toChild, child, ops);
        }

        foreach (var key in toKeys.Where(key => !fromKeys.Contains(key)))
        {
            to.TryGetPropertyValue(key, out var toChild);
            ops.Add(JsonPatchOperation.Add(Child(pointer, key), toChild?.DeepClone()));
        }
    }

    private static void DiffArrays(JsonArray from, JsonArray to, string pointer, List<JsonPatchOperation> ops)
    {
        var shared = Math.Min(from.Count, to.Count);
        for (var i = 0; i < shared; i++)
            DiffExisting(from[i], to[i], Child(pointer, i.ToString()), ops);

        for (var i = from.Count - 1; i >= to.Count; i--)
            ops.Add(JsonPatchOperation.Remove(Child(pointer, i.ToString())));

        for (var i = from.Count; i < to.Count; i++)
            ops.Add(JsonPatchOperation.Add(Child(pointer, i.ToString()), to[i]?.DeepClone()));
    }

    private static string Child(string pointer, string token)
        => pointer + "/" + Escape(token);

    private static string Escape(string token)
        => token.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}

/// <summary>
/// One RFC 6902 operation produced by <see cref="JsonPatch.Compare"/>.
/// </summary>
public sealed class JsonPatchOperation
{
    private JsonPatchOperation(string op, string path, JsonNode? value)
    {
        Op = op;
        Path = path;
        Value = value;
    }

    /// <summary><c>add</c>, <c>remove</c>, or <c>replace</c>.</summary>
    public string Op { get; }

    /// <summary>JSON Pointer to the target.</summary>
    public string Path { get; }

    /// <summary>Cloned value for add and replace. Null on remove.</summary>
    public JsonNode? Value { get; }

    internal static JsonPatchOperation Add(string path, JsonNode? value) => new("add", path, value);
    internal static JsonPatchOperation Remove(string path) => new("remove", path, null);
    internal static JsonPatchOperation Replace(string path, JsonNode? value) => new("replace", path, value);
}
