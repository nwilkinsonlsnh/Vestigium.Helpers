namespace Vestigium.Helpers.FileSystemPaths;

public static class PathSanitize
{
    public static string Name(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "_";
        var built = new System.Text.StringBuilder(raw.Length);
        var lastWasSafe = false;
        foreach (var ch in raw.Trim())
        {
            if (IsRejected(ch))
            {
                if (!lastWasSafe)
                {
                    built.Append('_');
                    lastWasSafe = true;
                }
                continue;
            }
            built.Append(ch);
            lastWasSafe = false;
        }
        var text = built.ToString().Trim().TrimEnd('.', ' ');
        return text.Length == 0 ? "_" : text;
    }

    private static bool IsRejected(char ch)
        => ch < ' ' || ch is '<' or '>' or ':' or '"' or '/' or '\' or '|' or '?' or '*';
}
