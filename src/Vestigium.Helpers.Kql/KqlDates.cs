using System.Globalization;

namespace Vestigium.Helpers.Kql;

/// <summary>
/// Date literals for <c>date(...)</c>. Relative days are from today. Month-only is the first of that month.
/// </summary>
public static class KqlDates
{
    private static readonly string[] Exact =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd",
        "M/d/yyyy",
        "MM/dd/yyyy",
        "d-MMM-yyyy",
        "dd-MMM-yyyy",
        "dMMMyyyy",
        "ddMMMyyyy",
        "d MMM yyyy",
        "dd MMM yyyy",
        "d MMMM yyyy",
        "dd MMMM yyyy",
        "MMM d yyyy",
        "MMMM d yyyy",
        "MMM d, yyyy",
        "MMMM d, yyyy"
    ];

    private static readonly string[] Month =
    [
        "MMM yyyy",
        "MMMM yyyy",
        "yyyy-MM",
        "yyyy/MM"
    ];

    public static DateTimeOffset Parse(string text)
    {
        if (!TryParse(text, out var value))
            throw new FormatException($"'{text}' is not a date.");
        return value;
    }

    public static bool TryParse(string? text, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var body = text.Trim().Trim('"').Trim('\'');
        if (body.Length == 0)
            return false;

        if (body.Equals("today", StringComparison.OrdinalIgnoreCase) || body.Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            value = new DateTimeOffset(DateTime.Today);
            return true;
        }

        if (body.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            value = new DateTimeOffset(DateTime.Today.AddDays(-1));
            return true;
        }

        if (body[0] is '+' or '-')
        {
            if (!int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
                return false;
            value = new DateTimeOffset(DateTime.Today.AddDays(days));
            return true;
        }

        if (DateTime.TryParseExact(body, Exact, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)
            || DateTime.TryParseExact(body, Exact, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out exact))
        {
            value = new DateTimeOffset(exact);
            return true;
        }

        if (DateTime.TryParseExact(body, Month, CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
            || DateTime.TryParseExact(body, Month, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out month))
        {
            value = new DateTimeOffset(new DateTime(month.Year, month.Month, 1));
            return true;
        }

        if (DateTime.TryParse(body, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var loose))
        {
            value = new DateTimeOffset(loose.Date);
            return true;
        }

        return false;
    }
}
