using System.Text.RegularExpressions;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Xml;

public enum XmlMatch
{
    Exact = 0,
    Contains = 1,
    Regex = 2
}

public readonly record struct XmlNs(string Prefix, string Uri);

public sealed class XmlSearch
{
    private XmlSearch(string kind)
    {
        Kind = kind;
    }

    internal string Kind { get; }
    internal string? LocalName { get; private init; }
    internal string? NamespaceUri { get; private init; }
    internal string? AttributeName { get; private init; }
    internal string? AttributeValue { get; private init; }
    internal string? Text { get; private init; }
    internal string? XPath { get; private init; }
    internal string? Ancestor { get; private init; }
    internal XmlMatch Match { get; private init; }
    internal bool CaseInsensitive { get; private init; }
    internal IReadOnlyList<XmlNs> Namespaces { get; private init; } = [];

    public static XmlSearch ByName(string localName, string? namespaceUri = null)
        => new("name")
        {
            LocalName = HelperGuard.NotBlank(localName, nameof(localName)),
            NamespaceUri = namespaceUri
        };

    public static XmlSearch ByAttribute(string name, string? value = null, XmlMatch match = XmlMatch.Exact)
        => new("attribute")
        {
            AttributeName = HelperGuard.NotBlank(name, nameof(name)),
            AttributeValue = value,
            Match = match
        };

    public static XmlSearch ByText(string text, XmlMatch match = XmlMatch.Exact, bool caseInsensitive = false)
        => new("text")
        {
            Text = HelperGuard.NotBlank(text, nameof(text)),
            Match = match,
            CaseInsensitive = caseInsensitive
        };

    public static XmlSearch XPath(string xpath, params XmlNs[] ns)
        => new("xpath")
        {
            XPath = HelperGuard.NotBlank(xpath, nameof(xpath)),
            Namespaces = ns ?? []
        };

    public XmlSearch WhereText(string text, XmlMatch match = XmlMatch.Exact)
        => new(Kind)
        {
            LocalName = LocalName,
            NamespaceUri = NamespaceUri,
            AttributeName = AttributeName,
            AttributeValue = AttributeValue,
            Text = HelperGuard.NotBlank(text, nameof(text)),
            XPath = XPath,
            Ancestor = Ancestor,
            Match = match,
            CaseInsensitive = CaseInsensitive,
            Namespaces = Namespaces
        };

    public XmlSearch InAncestor(string localName)
        => new(Kind)
        {
            LocalName = LocalName,
            NamespaceUri = NamespaceUri,
            AttributeName = AttributeName,
            AttributeValue = AttributeValue,
            Text = Text,
            XPath = XPath,
            Ancestor = HelperGuard.NotBlank(localName, nameof(localName)),
            Match = Match,
            CaseInsensitive = CaseInsensitive,
            Namespaces = Namespaces
        };

    internal string Spelling => XPath ?? LocalName ?? AttributeName ?? (Text is null ? Kind : "(text)");

    internal static bool Matches(string haystack, string needle, XmlMatch match, bool caseInsensitive)
    {
        if (match == XmlMatch.Regex)
        {
            try
            {
                return Regex.IsMatch(
                    haystack,
                    needle,
                    caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None,
                    TimeSpan.FromSeconds(1));
            }
            catch (ArgumentException)
            {
                HelperLog.Reject("regex is not valid");
                throw;
            }
        }

        var cmp = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return match == XmlMatch.Contains
            ? haystack.Contains(needle, cmp)
            : string.Equals(haystack, needle, cmp);
    }
}
