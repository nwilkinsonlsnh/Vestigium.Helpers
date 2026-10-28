using System.Text.RegularExpressions;

namespace Vestigium.Helpers.FileSystemPaths;

public static class PathCheck
{
    private static readonly Regex Pair = new(@"\{([^{}]*)\}", RegexOptions.Compiled);

    public static bool IsValid(string? template, long number = 0, long letter = 0)
    {
        if (string.IsNullOrWhiteSpace(template))
            return false;
        var text = Unescape(template);
        foreach (Match match in Pair.Matches(text))
        {
            if (!PathTokens.IsKnown(match.Groups[1].Value))
                return false;
        }
        if (text.Contains('{') || text.Contains('}'))
            return false;
        try
        {
            var expanded = PathTokens.Expand(template, number, letter);
            return IsLegal(expanded);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string Unescape(string text)
        => text.Replace("{{", "\u0001").Replace("}}", "\u0002");

    private static bool IsLegal(string path)
    {
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;
        var name = path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..] : path;
        foreach (var part in name.Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part is "." or "..")
                continue;
            if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;
        }
        return true;
    }
}
