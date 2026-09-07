using System.Text;
using ClosedXML.Excel;

namespace Vestigium.Helpers.ClosedXml;

internal static class ExcelNames
{
    private static readonly char[] Illegal = ['\\', '/', '*', '?', ':', '[', ']'];

    public static string Sanitize(string? name, string fallback = "Sheet1")
    {
        var raw = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw)
            builder.Append(Illegal.Contains(ch) ? '_' : ch);
        var cleaned = builder.ToString().Trim('\'', ' ');
        if (cleaned.Length == 0) cleaned = fallback;
        if (cleaned.Length > 31) cleaned = cleaned[..31].TrimEnd();
        if (cleaned.Length == 0) cleaned = fallback;
        return cleaned;
    }

    public static string SanitizeTable(string? name, string fallback)
    {
        var raw = Sanitize(string.IsNullOrWhiteSpace(name) ? fallback : name, fallback);
        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw)
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        var cleaned = builder.ToString().Trim('_');
        if (cleaned.Length == 0 || char.IsDigit(cleaned[0])) cleaned = "T_" + cleaned;
        if (cleaned.Length > 200) cleaned = cleaned[..200];
        return cleaned;
    }

    public static void TryApplyTabColor(IXLWorksheet sheet, string? hex)
    {
        var color = ParseTabColor(hex);
        if (color is null) return;
        sheet.TabColor = color;
    }

    public static XLColor? ParseTabColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var cleaned = hex.Trim().TrimStart('#');
        if (cleaned.Length == 8) cleaned = cleaned[^6..];
        if (cleaned.Length != 6) return null;
        foreach (var ch in cleaned)
        {
            var ok = ch is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
            if (!ok) return null;
        }
        return XLColor.FromHtml("#" + cleaned);
    }
}
