using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Json;

internal static class JsonCodec
{
    internal const int DefaultMaxDepth = 64;

    private static readonly JsonSerializerOptions Indented = Create(writeIndented: true, DefaultMaxDepth);
    private static readonly JsonSerializerOptions Compact = Create(writeIndented: false, DefaultMaxDepth);

    internal static JsonDocumentOptions DocumentOptions { get; } = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = DefaultMaxDepth
    };

    internal static JsonNodeOptions NodeOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = false
    };

    internal static JsonSerializerOptions Write(bool indented)
        => indented ? Indented : Compact;

    internal static JsonSerializerOptions Read(JsonReadOptions? options)
    {
        var depth = options?.MaxDepth ?? DefaultMaxDepth;
        if (depth == DefaultMaxDepth)
            return Compact;
        if (depth < 1)
        {
            HelperLog.Reject($"maxDepth={depth} is below 1");
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDepth must be at least 1.");
        }

        return Create(writeIndented: false, depth);
    }

    private static JsonSerializerOptions Create(bool writeIndented, int maxDepth) => new()
    {
        WriteIndented = writeIndented,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        MaxDepth = maxDepth
    };
}
