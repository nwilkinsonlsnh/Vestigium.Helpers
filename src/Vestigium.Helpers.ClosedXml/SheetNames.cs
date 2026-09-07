using System.Globalization;
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
        if (cleaned.Length == 0)
            cleaned = fallback;
        if (cleaned.Length > 31)
            cleaned = cleaned[..31].TrimEnd();
        if (cleaned.Length == 0)
            cleaned = fallback;
        return cleaned;
    }

    public static string SanitizeTable(string? name, string fallback)
    {
        var raw = Sanitize(string.IsNullOrWhiteSpace(name) ? fallback : name, fallback);
        var builder = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                builder.Append(ch);
            else
                builder.Append('_');
        }

        var cleaned = builder.ToString().Trim('_');
        if (cleaned.Length == 0 || char.IsDigit(cleaned[0]))
            cleaned = "T_" + cleaned;
        if (cleaned.Length > 200)
            cleaned = cleaned[..200];
        return cleaned;
    }

    public static XLColor? ParseTabColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        var t = hex.Trim().TrimStart('#');
        if (t.Length == 8)
            t = t[^6..];
        if (t.Length != 6)
            return null;
        if (!int.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return null;
        return XLColor.FromHtml("#" + t);
    }

    public static string ColumnLetter(int index1Based)
    {
        if (index1Based < 1)
            throw new ArgumentOutOfRangeException(nameof(index1Based));
        var n = index1Based;
        var s = "";
        while (n > 0)
        {
            var rem = (n - 1) % 26;
            s = (char)('A' + rem) + s;
            n = (n - 1) / 26;
        }
        return s;
    }

    public static string QuoteSheet(string name)
    {
        var safe = Sanitize(name);
        var quote = safe.Length == 0 || char.IsDigit(safe[0]);
        if (!quote)
        {
            foreach (var ch in safe)
            {
                if (!(char.IsLetterOrDigit(ch) || ch is '_' or '.'))
                {
                    quote = true;
                    break;
                }
            }
        }
        var escaped = safe.Replace("'", "''", StringComparison.Ordinal);
        return quote ? $"'{escaped}'" : escaped;
    }

    public static string A1Range(string sheet, int col1, int row1, int col2, int row2)
        => $"{QuoteSheet(sheet)}!${ColumnLetter(col1)}${row1}:${ColumnLetter(col2)}${row2}";
}
