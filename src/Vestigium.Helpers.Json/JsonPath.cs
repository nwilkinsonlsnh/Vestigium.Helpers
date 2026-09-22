using System.Globalization;
using System.Text.Json.Nodes;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Json;

internal readonly record struct JsonPathSegment(string Token, int? ArrayIndex, bool ExplicitArray = false);

internal sealed class JsonPath
{
    public static JsonPath Root { get; } = new("", []);

    public string Original { get; }
    public IReadOnlyList<JsonPathSegment> Segments { get; }

    private JsonPath(string original, JsonPathSegment[] segments)
    {
        Original = original;
        Segments = segments;
    }

    public static JsonPath Parse(string? path)
    {
        if (path is not null && path.Length == 0)
            return Root;

        var text = HelperGuard.NotNull(path, nameof(path));
        if (!string.IsNullOrWhiteSpace(text)) return text[0] == '/' ? ParsePointer(text) : ParseDotted(text);
        HelperLog.Reject(
            HelperLog.AppIds.Json,
            HelperLog.Subcategories.Query,
            "Parse",
            "path is blank");
        throw new ArgumentException("Value is required.", nameof(path));

    }

    public bool TryEvaluate(JsonNode? root, out JsonNode? node)
    {
        node = root;
        foreach (var segment in Segments)
        {
            switch (node)
            {
                case JsonArray array:
                {
                    if (segment.ArrayIndex is not int index || index < 0 || index >= array.Count)
                    {
                        node = null;
                        return false;
                    }

                    node = array[index];
                    continue;
                }
                case JsonObject obj when obj.TryGetPropertyValue(segment.Token, out var next):
                    node = next;
                    continue;
                default:
                    node = null;
                    return false;
            }
        }

        return true;
    }

    public void Assign(ref JsonNode root, JsonNode? value)
    {
        if (Segments.Count == 0)
        {
            if (value is null)
            {
                HelperLog.Reject("root cannot be JSON null");
                throw new InvalidOperationException("RFC 8259 JSON null is not a document root.");
            }

            root = value;
            return;
        }

        var parent = root;
        for (var i = 0; i < Segments.Count - 1; i++)
            parent = EnsureChild(parent, Segments[i], Segments[i + 1]);

        SetChild(parent, Segments[^1], value);
    }

    private static JsonNode EnsureChild(JsonNode parent, JsonPathSegment segment, JsonPathSegment next)
    {
        switch (parent)
        {
            case JsonArray array:
            {
                if (segment.ArrayIndex is not int index || index < 0 || index >= array.Count)
                {
                    HelperLog.Reject("cannot create array parents");
                    throw new InvalidOperationException("JSON path cannot create array parents.");
                }

                var existing = array[index];
                switch (existing)
                {
                    case null:
                    case JsonValue:
                        HelperLog.Reject("cannot create through a primitive");
                        throw new InvalidOperationException("JSON path cannot create through a primitive.");
                    default:
                        return existing;
                }
            }
            case JsonObject when segment.ExplicitArray:
                HelperLog.Reject("cannot index an object");
                throw new InvalidOperationException("JSON path cannot index an object with [n].");
            case JsonObject obj when obj.TryGetPropertyValue(segment.Token, out var existing) && existing is not null:
            {
                if (existing is not JsonValue) return existing;
                HelperLog.Reject("cannot create through a primitive");
                throw new InvalidOperationException("JSON path cannot create through a primitive.");

            }
            case JsonObject when next.ExplicitArray:
                HelperLog.Reject("cannot create array parents");
                throw new InvalidOperationException("JSON path cannot create array parents.");
            case JsonObject obj:
            {
                var created = new JsonObject();
                obj[segment.Token] = created;
                return created;
            }
            default:
                HelperLog.Reject("cannot create through a primitive");
                throw new InvalidOperationException("JSON path cannot create through a primitive.");
        }
    }

    private static void SetChild(JsonNode parent, JsonPathSegment segment, JsonNode? value)
    {
        switch (parent)
        {
            case JsonArray array:
            {
                if (segment.ArrayIndex is not { } index || index < 0 || index >= array.Count)
                {
                    HelperLog.Reject("array index is missing");
                    throw new InvalidOperationException("JSON path array index is missing.");
                }

                array[index] = value?.DeepClone();
                return;
            }
            case JsonObject when segment.ExplicitArray:
                HelperLog.Reject("cannot index an object");
                throw new InvalidOperationException("JSON path cannot index an object with [n].");
            case JsonObject obj:
                obj[segment.Token] = value?.DeepClone();
                return;
            default:
                HelperLog.Reject("cannot set a member on a primitive");
                throw new InvalidOperationException("JSON path cannot set a member on a primitive.");
        }
    }

    private static JsonPath ParsePointer(string path)
    {
        if (path.Length == 1)
            return new JsonPath(path, [SegmentFromToken("")]);

        var raw = path.AsSpan(1);
        var parts = new List<JsonPathSegment>();
        var start = 0;
        for (var i = 0; i <= raw.Length; i++)
        {
            if (i < raw.Length && raw[i] != '/')
                continue;
            var token = Unescape(raw[start..i].ToString());
            parts.Add(SegmentFromToken(token));
            start = i + 1;
        }

        return new JsonPath(path, [.. parts]);
    }

    private static JsonPath ParseDotted(string path)
    {
        var parts = new List<JsonPathSegment>();
        var i = 0;
        parts.Add(path[0] == '[' ? ReadIndex(path, ref i) : ReadIdent(path, ref i));

        while (i < path.Length)
        {
            switch (path[i])
            {
                case '.':
                    i++;
                    parts.Add(ReadIdent(path, ref i));
                    continue;
                case '[':
                    parts.Add(ReadIndex(path, ref i));
                    continue;
                default:
                    Fail(path, "unexpected character");
                    break;
            }
        }

        return new JsonPath(path, [.. parts]);
    }

    private static JsonPathSegment ReadIdent(string path, ref int i)
    {
        if (i >= path.Length || !IsIdentStart(path[i]))
            Fail(path, "expected a name");
        var start = i;
        i++;
        while (i < path.Length && IsIdentContinue(path[i]))
            i++;
        return SegmentFromToken(path[start..i]);
    }

    private static JsonPathSegment ReadIndex(string path, ref int i)
    {
        if (i >= path.Length || path[i] != '[')
            Fail(path, "expected '['");
        i++;
        if (i >= path.Length || !char.IsAsciiDigit(path[i]))
            Fail(path, "expected an array index");
        var start = i;
        if (path[i] == '0')
        {
            i++;
            if (i < path.Length && char.IsAsciiDigit(path[i]))
                Fail(path, "leading zeros are not allowed");
        }
        else
        {
            while (i < path.Length && char.IsAsciiDigit(path[i]))
                i++;
        }

        if (i >= path.Length || path[i] != ']')
            Fail(path, "expected ']'");
        var digits = path[start..i];
        i++;
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            Fail(path, "array index is out of range");
        return new JsonPathSegment(digits, index, ExplicitArray: true);
    }

    private static JsonPathSegment SegmentFromToken(string token)
    {
        if (IsRfcArrayIndex(token)
            && int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            return new JsonPathSegment(token, index);
        return new JsonPathSegment(token, null);
    }

    private static bool IsRfcArrayIndex(string token)
    {
        if (token.Length == 0)
            return false;
        if (token == "0")
            return true;
        if (token[0] == '0')
            return false;
        return token.All(char.IsAsciiDigit);
    }

    private static string Unescape(string token)
    {
        if (!token.Contains('~'))
            return token;
        var chars = new char[token.Length];
        var n = 0;
        for (var i = 0; i < token.Length; i++)
        {
            if (token[i] != '~')
            {
                chars[n++] = token[i];
                continue;
            }

            if (i + 1 >= token.Length)
                Fail(token, "invalid '~' escape");
            chars[n++] = token[i + 1] switch
            {
                '0' => '~',
                '1' => '/',
                _ => InvalidEscape(token)
            };
            i++;
        }

        return new string(chars, 0, n);
    }

    private static bool IsIdentStart(char c)
        => char.IsAsciiLetter(c) || c == '_';

    private static bool IsIdentContinue(char c)
        => char.IsAsciiLetterOrDigit(c) || c is '_' or '-';

    private static char InvalidEscape(string path)
    {
        Fail(path, "invalid '~' escape");
        return '\0';
    }

    private static void Fail(string path, string reason)
    {
        ArgumentNullException.ThrowIfNull(path);

        HelperLog.Reject(
            HelperLog.AppIds.Json,
            HelperLog.Subcategories.Query,
            "Parse",
            $"path is invalid reason={reason}");
        throw new ArgumentException($"JSON path is invalid: {reason}.", nameof(path));
    }
}
