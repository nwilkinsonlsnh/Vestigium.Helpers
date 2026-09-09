using System.Globalization;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

internal readonly record struct JsonPathSegment(string Token, int? ArrayIndex);

internal sealed class JsonPath
{
    public static JsonPath Root { get; } = new("", Array.Empty<JsonPathSegment>());

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
        if (string.IsNullOrWhiteSpace(text))
        {
            HelperLog.Reject("path is blank");
            throw new ArgumentException("Value is required.", nameof(path));
        }

        var app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Query, "Parse", "path=" + text);
        try
        {
            return text[0] == '/' ? ParsePointer(text) : ParseDotted(text);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public bool TryEvaluate(JsonNode? root, out JsonNode? node)
    {
        node = root;
        foreach (var segment in Segments)
        {
            if (node is JsonArray array)
            {
                if (segment.ArrayIndex is not int index || index < 0 || index >= array.Count)
                {
                    node = null;
                    return false;
                }

                node = array[index];
                continue;
            }

            if (node is JsonObject obj && obj.TryGetPropertyValue(segment.Token, out var next))
            {
                node = next;
                continue;
            }

            node = null;
            return false;
        }

        return true;
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
        if (path[0] == '[')
            parts.Add(ReadIndex(path, ref i));
        else
            parts.Add(ReadIdent(path, ref i));

        while (i < path.Length)
        {
            if (path[i] == '.')
            {
                i++;
                parts.Add(ReadIdent(path, ref i));
                continue;
            }

            if (path[i] == '[')
            {
                parts.Add(ReadIndex(path, ref i));
                continue;
            }

            Fail(path, "unexpected character");
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
        return new JsonPathSegment(digits, index);
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
        foreach (var c in token)
        {
            if (!char.IsAsciiDigit(c))
                return false;
        }

        return true;
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
        HelperLog.Reject($"path is invalid reason={reason}");
        throw new ArgumentException($"JSON path is invalid: {reason}.", nameof(path));
    }
}
