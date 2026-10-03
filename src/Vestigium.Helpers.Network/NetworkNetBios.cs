using System.Diagnostics;
using System.Text;

namespace Vestigium.Helpers.Network;

internal static class NetworkNetBios
{
    public static IReadOnlyList<NetworkNetBiosName> Read()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        var rows = new List<NetworkNetBiosName>();
        rows.AddRange(ParseLocal(Run("nbtstat", "-n")));
        rows.AddRange(ParseCache(Run("nbtstat", "-c")));
        return rows;
    }

    private static string Run(string file, string args)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            process.Start();
            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit(8000);
            return text;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static IEnumerable<NetworkNetBiosName> ParseLocal(string text)
    {
        foreach (var line in Lines(text))
        {
            var mark = line.IndexOf('<');
            var end = line.IndexOf('>');
            if (mark < 1 || end <= mark)
                continue;
            var name = line[..mark].Trim();
            var suffix = line[(mark + 1)..end].Trim();
            var rest = line[(end + 1)..].Trim();
            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;
            yield return new NetworkNetBiosName("Local", name, suffix, parts[0], parts[1], null, null);
        }
    }

    private static IEnumerable<NetworkNetBiosName> ParseCache(string text)
    {
        foreach (var line in Lines(text))
        {
            var mark = line.IndexOf('<');
            var end = line.IndexOf('>');
            if (mark < 1 || end <= mark)
                continue;
            var name = line[..mark].Trim();
            var suffix = line[(mark + 1)..end].Trim();
            var rest = line[(end + 1)..].Trim();
            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;
            var address = parts.Length > 1 ? parts[1] : null;
            int? life = parts.Length > 2 && int.TryParse(parts[2], out var seconds) ? seconds : null;
            yield return new NetworkNetBiosName("Cache", name, suffix, parts[0], life?.ToString() ?? string.Empty, address, life);
        }
    }

    private static IEnumerable<string> Lines(string text)
        => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
