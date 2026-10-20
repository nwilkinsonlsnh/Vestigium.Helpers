namespace Vestigium.Helpers.FileSystemPaths;

public static class PathResolve
{
    public static string Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        var text = raw.Trim().Trim('"');
        if (text.StartsWith(@"\??\", StringComparison.Ordinal))
            text = text[4..];
        var comma = text.IndexOf(',');
        if (comma > 0 && text[..comma].Contains('\\'))
            text = text[..comma].Trim().Trim('"');
        return Environment.ExpandEnvironmentVariables(text);
    }
}
