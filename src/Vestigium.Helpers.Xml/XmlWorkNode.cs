using System.Globalization;
using System.Xml.Linq;

namespace Vestigium.Helpers.Xml;

public sealed class XmlWorkNode
{
    internal XmlWorkNode(XElement element, string path)
    {
        Element = element;
        Path = path;
        LocalName = element.Name.LocalName;
        NamespaceUri = element.Name.NamespaceName;
        Text = element.HasElements ? string.Empty : (element.Value ?? string.Empty).Trim();
        Attributes = element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value, StringComparer.Ordinal);
    }

    public string Path { get; }

    public string LocalName { get; }

    public string NamespaceUri { get; }

    public string Text { get; }

    public IReadOnlyDictionary<string, string> Attributes { get; }

    internal XElement Element { get; }

    public bool TryGetInt(string? attribute, out int value)
        => int.TryParse(Pick(attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    public bool TryGetBool(string? attribute, out bool value)
    {
        var raw = Pick(attribute);
        if (bool.TryParse(raw, out value))
            return true;
        if (raw is "1" or "true" or "True")
        {
            value = true;
            return true;
        }
        if (raw is "0" or "false" or "False")
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetGuid(string? attribute, out Guid value)
        => Guid.TryParse(Pick(attribute), out value);

    public bool TryGetDateTime(string? attribute, out DateTime value)
        => DateTime.TryParse(Pick(attribute), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);

    public bool TryGetBase64(string? attribute, out byte[] value)
    {
        try
        {
            value = Convert.FromBase64String(Pick(attribute));
            return true;
        }
        catch (FormatException)
        {
            value = [];
            return false;
        }
    }

    private string Pick(string? attribute)
        => string.IsNullOrWhiteSpace(attribute)
            ? Text
            : Attributes.TryGetValue(attribute, out var v) ? v : string.Empty;
}
