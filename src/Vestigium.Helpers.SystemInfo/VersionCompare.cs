namespace Vestigium.Helpers.SystemInfo;

public static class VersionCompare
{
    public static string Status(string installed, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return "none";
        if (string.Equals(installed.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            return "match";
        if (Version.TryParse(Normalize(installed), out var current) && Version.TryParse(Normalize(expected), out var baseline))
            return current > baseline ? "newer" : current < baseline ? "older" : "match";
        return "unknown";
    }

    private static string Normalize(string version)
    {
        var text = version.Trim();
        var end = 0;
        while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.'))
            end++;
        return end == 0 ? text : text[..end].TrimEnd('.');
    }
}
