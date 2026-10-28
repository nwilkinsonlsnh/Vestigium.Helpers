namespace Vestigium.Helpers.Network;

internal static class NetworkLmHost
{
    public static IReadOnlyList<NetworkLmHostEntry> Read()
    {
        var path = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "lmhosts");
        if (!File.Exists(path))
            return [];

        var rows = new List<NetworkLmHostEntry>();
        foreach (var line in File.ReadLines(path))
        {
            var parsed = Parse(line);
            if (parsed is not null)
                rows.Add(parsed);
        }

        return rows;
    }

    private static NetworkLmHostEntry? Parse(string line)
    {
        var text = line.Trim();
        if (text.Length == 0 || text.StartsWith('#'))
            return null;

        var parts = text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var preload = false;
        var multi = false;
        string? domain = null;
        string? include = null;
        for (var i = 2; i < parts.Length; i++)
        {
            var tag = parts[i];
            if (tag.Equals("#PRE", StringComparison.OrdinalIgnoreCase))
                preload = true;
            else if (tag.Equals("#MH", StringComparison.OrdinalIgnoreCase))
                multi = true;
            else if (tag.StartsWith("#DOM:", StringComparison.OrdinalIgnoreCase))
                domain = tag[5..];
            else if (tag.Equals("#INCLUDE", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
                include = parts[++i];
        }

        return new NetworkLmHostEntry(parts[0], parts[1], preload, multi, domain, include, text);
    }
}
