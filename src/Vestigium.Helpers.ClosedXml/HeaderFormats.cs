namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Column number formats from header names: <c>ms</c>, <c>pct</c>, <c>utc</c>.
/// </summary>
internal static class HeaderFormats
{
    public const string Milliseconds = "0.0";
    public const string Percent = "0.00%";
    public const string Utc = "yyyy-mm-dd hh:mm:ss";

    public static string? For(string header)
    {
        var t = Compact(header);
        if (t.Length == 0)
            return null;
        if (IsUtc(t))
            return Utc;
        if (IsPct(t))
            return Percent;
        if (IsMs(t))
            return Milliseconds;
        return null;
    }

    private static string Compact(string header)
    {
        var builder = new System.Text.StringBuilder(header.Length);
        foreach (var ch in header.Trim().ToLowerInvariant())
        {
            if (ch is not (' ' or '_' or '-'))
                builder.Append(ch);
        }

        return builder.ToString();
    }

    private static bool IsUtc(string t) =>
        t is "utc" or "timestamp" or "datetime" ||
        t.EndsWith("utc", StringComparison.Ordinal) ||
        t.Contains("timestamp", StringComparison.Ordinal);

    private static bool IsPct(string t) =>
        t is "pct" or "percent" or "percentage" or "relative" ||
        t.EndsWith("pct", StringComparison.Ordinal) ||
        t.EndsWith("percent", StringComparison.Ordinal) ||
        t.Contains('%');

    private static bool IsMs(string t) =>
        t is "ms" or "msec" or "millis" or "milliseconds" or "rtt" ||
        t.EndsWith("ms", StringComparison.Ordinal);
}
