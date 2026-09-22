namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Print and parse digests. Default on the helper is lowercase hex.
/// Hex ↔ Base64 converters are the way a host moves a SHA-256 into JSON.
/// </summary>
public static class HashingConvert
{
    public static string Format(ReadOnlySpan<byte> data, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        if (data.IsEmpty)
            return format is HashingTextFormat.HexLower or HashingTextFormat.HexUpper
                ? string.Empty
                : Convert.ToBase64String(Array.Empty<byte>());

        return format switch
        {
            HashingTextFormat.HexLower => Convert.ToHexString(data).ToLowerInvariant(),
            HashingTextFormat.HexUpper => Convert.ToHexString(data),
            HashingTextFormat.Base64 => Convert.ToBase64String(data),
            HashingTextFormat.Base64Url => ToBase64Url(data),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static byte[] Parse(string text, HashingTextFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return [];

        try
        {
            return format switch
            {
                HashingTextFormat.HexLower or HashingTextFormat.HexUpper => Convert.FromHexString(StripHex(trimmed)),
                HashingTextFormat.Base64 => Convert.FromBase64String(trimmed),
                HashingTextFormat.Base64Url => FromBase64Url(trimmed),
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            };
        }
        catch (FormatException ex)
        {
            throw new FormatException("Digest text is not valid for the requested format.", ex);
        }
    }

    public static string HexToBase64(string hex)
        => Format(Parse(hex, HashingTextFormat.HexLower), HashingTextFormat.Base64);

    public static string HexToBase64Url(string hex)
        => Format(Parse(hex, HashingTextFormat.HexLower), HashingTextFormat.Base64Url);

    public static string Base64ToHex(string base64)
        => Format(Parse(base64, HashingTextFormat.Base64), HashingTextFormat.HexLower);

    public static string Base64UrlToHex(string base64Url)
        => Format(Parse(base64Url, HashingTextFormat.Base64Url), HashingTextFormat.HexLower);

    private static string StripHex(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];
        return hex.Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
    }

    private static string ToBase64Url(ReadOnlySpan<byte> data)
    {
        var b64 = Convert.ToBase64String(data);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        var pad = (4 - (s.Length % 4)) % 4;
        if (pad > 0)
            s += new string('=', pad);
        return Convert.FromBase64String(s);
    }
}
