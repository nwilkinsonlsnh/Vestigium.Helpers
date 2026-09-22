using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Excel's built-in table styles (Table Design → Table Styles).
/// Ids match the gallery: Light1–21, Medium1–28, Dark1–11.
/// </summary>
public static class ExcelTableStyles
{
    public const string DefaultId = "Medium2";
    public const string DefaultExcelName = "TableStyleMedium2";

    private static readonly Regex IdPattern = new(
        "^(Light([1-9]|1[0-9]|2[01])|Medium([1-9]|1[0-9]|2[0-8])|Dark([1-9]|10|11))$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    public static IReadOnlyList<string> Ids { get; } = BuildIds();

    public static string Normalize(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return DefaultId;

        var t = id.Trim();
        if (t.Equals("None", StringComparison.OrdinalIgnoreCase))
            return "None";
        if (t.StartsWith("TableStyle", StringComparison.OrdinalIgnoreCase))
            t = t["TableStyle".Length..];
        t = t.Replace(" ", "", StringComparison.Ordinal);

        var i = 0;
        while (i < t.Length && char.IsLetter(t[i]))
            i++;
        if (i == 0 || i == t.Length)
            throw Unknown(id);

        var group = char.ToUpperInvariant(t[0]) + t[1..i].ToLowerInvariant();
        var key = group + t[i..];
        if (!IsKnown(key))
            throw Unknown(id);
        return key;
    }

    public static string ToExcelName(string? id)
    {
        var key = Normalize(id);
        return key.Equals("None", StringComparison.OrdinalIgnoreCase) ? "None" : "TableStyle" + key;
    }

    public static bool IsKnown(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        if (id.Equals("None", StringComparison.OrdinalIgnoreCase))
            return true;
        return IdPattern.IsMatch(id);
    }

    public static XLTableTheme Resolve(string? id)
    {
        var excel = ToExcelName(id);
        // ClosedXML 0.105 ships these as public static fields, not properties.
        // FromName is the documented lookup (Name == "TableStyleMedium2").
        var theme = XLTableTheme.FromName(excel);
        if (theme is not null)
            return theme;
        throw Unknown(id);
    }

    private static ArgumentOutOfRangeException Unknown(string? id)
        => new(nameof(id), $"Unknown Excel table style '{id}'. Use Light1–21, Medium1–28, Dark1–11, or None.");

    private static string[] BuildIds()
    {
        var list = new List<string>(60);
        for (var i = 1; i <= 21; i++)
            list.Add("Light" + i);
        for (var i = 1; i <= 28; i++)
            list.Add("Medium" + i);
        for (var i = 1; i <= 11; i++)
            list.Add("Dark" + i);
        return [.. list];
    }
}
