using System.Text.Json.Nodes;

namespace Vestigium.Helpers.Json;

/// <summary>
/// RFC 6902 JSON Patch. Hosts render the operation list. HelperLog records the count, never values.
/// </summary>
public sealed class JsonPatch
{
    private readonly JsonPatchOperation[] _operations;

    internal JsonPatch(IEnumerable<JsonPatchOperation> operations)
        => _operations = operations.ToArray();

    public IReadOnlyList<JsonPatchOperation> Operations => _operations;

    public int Count => _operations.Length;

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

    public override string ToString() => $"{Count} operation(s)";

    internal static JsonPatch Compare(JsonNode? from, JsonNode? to)
    {
        var ops = new List<JsonPatchOperation>();
        DiffExisting(from, to, "", ops);
        return new JsonPatch(ops);
    }

    private static void DiffExisting(JsonNode? from, JsonNode? to, string pointer, List<JsonPatchOperation> ops)
    {
        if (JsonNode.DeepEquals(from, to))
            return;

        if (from is JsonObject fromObj && to is JsonObject toObj)
        {
            DiffObjects(fromObj, toObj, pointer, ops);
            return;
        }

        if (from is JsonArray fromArr && to is JsonArray toArr)
        {
            DiffArrays(fromArr, toArr, pointer, ops);
            return;
        }

        ops.Add(JsonPatchOperation.Replace(pointer, to?.DeepClone()));
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

        foreach (var key in toKeys)
        {
            if (fromKeys.Contains(key))
                continue;
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

public sealed class JsonPatchOperation
{
    private JsonPatchOperation(string op, string path, JsonNode? value)
    {
        Op = op;
        Path = path;
        Value = value;
    }

    public string Op { get; }
    public string Path { get; }
    public JsonNode? Value { get; }

    internal static JsonPatchOperation Add(string path, JsonNode? value) => new("add", path, value);
    internal static JsonPatchOperation Remove(string path) => new("remove", path, null);
    internal static JsonPatchOperation Replace(string path, JsonNode? value) => new("replace", path, value);
}
