using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Xml;

internal sealed class XmlEncodingResult
{
    public required Encoding Encoding { get; init; }
    public required string Name { get; init; }
    public required string Source { get; init; }
    public string? Bom { get; init; }
    public string? Declaration { get; init; }
    public string? Charset { get; init; }
    public bool Conflict { get; init; }
    public string? Warning { get; init; }
    public int BomLength { get; init; }
}

internal static class XmlEncoding
{
    public static XmlEncodingResult Detect(ReadOnlySpan<byte> bytes, string? mimeCharset)
    {
        string? bom = null;
        var bomLength = 0;
        if (StartsWith(bytes, [0x00, 0x00, 0xFE, 0xFF])) { bom = "utf-32be"; bomLength = 4; }
        else if (StartsWith(bytes, [0xFF, 0xFE, 0x00, 0x00])) { bom = "utf-32le"; bomLength = 4; }
        else if (StartsWith(bytes, [0xEF, 0xBB, 0xBF])) { bom = "utf-8"; bomLength = 3; }
        else if (StartsWith(bytes, [0xFE, 0xFF])) { bom = "utf-16be"; bomLength = 2; }
        else if (StartsWith(bytes, [0xFF, 0xFE])) { bom = "utf-16le"; bomLength = 2; }

        var charset = Normalize(mimeCharset);
        var declaration = Normalize(ReadDeclaration(bytes));

        string name;
        string source;
        if (bom is not null)
        {
            name = bom.StartsWith("utf-32", StringComparison.Ordinal) ? "utf-8" : bom;
            source = "bom";
        }
        else if (charset is not null)
        {
            name = charset;
            source = "charset";
        }
        else if (declaration is not null)
        {
            name = declaration;
            source = "declaration";
        }
        else
        {
            name = "utf-8";
            source = "utf-8";
        }

        var claimed = new[] { bom, charset, declaration }.Where(v => v is not null).Cast<string>().ToArray();
        var conflict = claimed.Length > 1 && claimed.Any(v => !string.Equals(Normalize(v), name, StringComparison.Ordinal));
        string? warning = null;
        if (conflict)
            warning = $"encoding conflict bom={bom ?? "-"} charset={charset ?? "-"} declaration={declaration ?? "-"} winner={source}";
        else if (bom is not null && bom.StartsWith("utf-32", StringComparison.Ordinal))
            warning = "utf-32 bom is not an advertised write encoding";

        return new XmlEncodingResult
        {
            Encoding = ForName(name),
            Name = name,
            Source = source,
            Bom = bom,
            Declaration = declaration,
            Charset = charset,
            Conflict = conflict,
            Warning = warning,
            BomLength = bomLength
        };
    }

    public static Encoding ForWrite(bool emitBom)
        => new UTF8Encoding(encoderShouldEmitUTF8Identifier: emitBom);

    public static string Decode(ReadOnlySpan<byte> bytes, XmlEncodingResult detected)
    {
        var slice = detected.BomLength > 0 && bytes.Length >= detected.BomLength
            ? bytes[detected.BomLength..]
            : bytes;
        return detected.Encoding.GetString(slice);
    }

    private static string? ReadDeclaration(ReadOnlySpan<byte> bytes)
    {
        var n = Math.Min(bytes.Length, 512);
        var chars = new char[n];
        var written = 0;
        for (var i = 0; i < n; i++)
        {
            var b = bytes[i];
            if (b == 0)
                continue;
            chars[written++] = (char)b;
        }
        var head = new string(chars, 0, written);
        var match = System.Text.RegularExpressions.Regex.Match(
            head,
            @"<\?xml\b[^>]*\bencoding\s*=\s*[""']\s*([^""']+)\s*[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var n = name.Trim().ToLowerInvariant().Replace('_', '-');
        if (n is "utf8") return "utf-8";
        if (n is "utf16" or "utf-16" or "utf-16le" or "unicode") return "utf-16le";
        if (n is "utf-16be") return "utf-16be";
        return n;
    }

    internal static Encoding ForName(string name)
        => name switch
        {
            "utf-16le" => Encoding.Unicode,
            "utf-16be" => Encoding.BigEndianUnicode,
            _ => new UTF8Encoding(false)
        };

    private static bool StartsWith(ReadOnlySpan<byte> bytes, byte[] sig)
    {
        if (bytes.Length < sig.Length)
            return false;
        for (var i = 0; i < sig.Length; i++)
        {
            if (bytes[i] != sig[i])
                return false;
        }
        return true;
    }
}
