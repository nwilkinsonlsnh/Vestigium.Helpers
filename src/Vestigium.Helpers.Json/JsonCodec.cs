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
        switch (depth)
        {
            case DefaultMaxDepth:
                return Compact;
            case < 1:
                HelperLog.Reject($"maxDepth={depth} is below 1");
                throw new ArgumentOutOfRangeException(nameof(options), "MaxDepth must be at least 1.");
            default:
                return Create(writeIndented: false, depth);
        }
    }

    internal static JsonNode? ToNode(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            JsonElement element => JsonNode.Parse(element.GetRawText(), NodeOptions, DocumentOptions),
            _ => JsonSerializer.SerializeToNode(value, Compact)
        };
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
