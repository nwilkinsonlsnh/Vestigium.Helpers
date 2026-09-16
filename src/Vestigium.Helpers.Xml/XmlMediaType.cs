namespace Vestigium.Helpers.Xml;

public sealed class XmlMediaType : IEquatable<XmlMediaType>
{
    public XmlMediaType(string type, string? charset = "utf-8")
    {
        Type = Normalize(type);
        Charset = string.IsNullOrWhiteSpace(charset) ? null : charset.Trim();
    }

    public string Type { get; }

    public string? Charset { get; }

    public static XmlMediaType ApplicationXml { get; } = new("application/xml", "utf-8");

    public static bool IsXmlFamily(string mediaType)
    {
        var type = Normalize(mediaType);
        return type is "application/xml" or "text/xml" or "application/xml-dtd"
            || type.EndsWith("+xml", StringComparison.Ordinal);
    }

    public static bool AreAliases(string left, string right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a == b)
            return true;
        return (a is "application/xml" or "text/xml") && (b is "application/xml" or "text/xml");
    }

    public string ToContentType()
        => string.IsNullOrWhiteSpace(Charset) ? Type : Type + "; charset=" + Charset;

    public bool Equals(XmlMediaType? other)
        => other is not null && AreAliases(Type, other.Type);

    public override bool Equals(object? obj) => Equals(obj as XmlMediaType);

    public override int GetHashCode()
        => Type is "text/xml" ? "application/xml".GetHashCode(StringComparison.Ordinal) : Type.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => ToContentType();

    public static XmlMediaType Guess(string? hint, bool looksLikeDtd)
    {
        if (!string.IsNullOrWhiteSpace(hint) && IsXmlFamily(hint))
        {
            var type = Normalize(hint);
            if (type == "text/xml")
                type = "application/xml";
            return new XmlMediaType(type, "utf-8");
        }

        if (looksLikeDtd)
            return new XmlMediaType("application/xml-dtd", "utf-8");

        return ApplicationXml;
    }

    private static string Normalize(string mediaType)
    {
        var raw = mediaType.Trim();
        var semi = raw.IndexOf(';');
        if (semi >= 0)
            raw = raw[..semi];
        return raw.Trim().ToLowerInvariant();
    }
}
