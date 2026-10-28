using System.Diagnostics;
using System.Text;

namespace Vestigium.Helpers.Network;

internal static class NetworkNetBiosFallback
{
    public static IReadOnlyList<NetworkNetBiosName> Read()
    {
        if (!OperatingSystem.IsWindows())
            return [];
        var rows = new List<NetworkNetBiosName>();
        rows.AddRange(Parse(Run("-n"), cache: false));
        rows.AddRange(Parse(Run("-c"), cache: true));
        return rows;
    }

    public static NetworkNetBiosStats Stats()
    {
        if (!OperatingSystem.IsWindows())
            return new NetworkNetBiosStats(0, 0, 0, 0, null);
        var broadcast = 0;
        var server = 0;
        var registeredBroadcast = 0;
        var registeredServer = 0;
        var registration = false;
        foreach (var line in Lines(Run("-r")))
        {
            if (line.Contains("Registration", StringComparison.OrdinalIgnoreCase))
                registration = true;
            var count = Count(line);
            if (line.Contains("Broadcast", StringComparison.OrdinalIgnoreCase))
            {
                if (registration) registeredBroadcast = count;
                else broadcast = count;
            }
            else if (line.Contains("Name Server", StringComparison.OrdinalIgnoreCase) || line.Contains("WINS", StringComparison.OrdinalIgnoreCase))
            {
                if (registration) registeredServer = count;
                else server = count;
            }
        }

        return new NetworkNetBiosStats(broadcast, server, registeredBroadcast, registeredServer, null);
    }

    private static int Count(string line)
    {
        var mark = line.LastIndexOf('=');
        if (mark < 0)
            return 0;
        return int.TryParse(line[(mark + 1)..].Trim(), out var count) ? count : 0;
    }

    private static string Run(string args)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "nbtstat",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(8000))
            {
                try { process.Kill(entireProcessTree: true); } catch (Exception) { }
                return string.Empty;
            }

            return stdout.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static IEnumerable<NetworkNetBiosName> Parse(string text, bool cache)
    {
        var adapter = string.Empty;
        var node = string.Empty;
        foreach (var line in Lines(text))
        {
            if (line.StartsWith("Node IpAddress", StringComparison.OrdinalIgnoreCase))
            {
                node = Between(line, '[', ']');
                continue;
            }

            if (line.EndsWith(':') && !line.Contains('<'))
            {
                adapter = line.TrimEnd(':').Trim();
                node = string.Empty;
                continue;
            }

            var mark = line.IndexOf('<');
            var end = line.IndexOf('>');
            if (mark < 1 || end <= mark)
                continue;
            var name = line[..mark].Trim();
            var suffix = line[(mark + 1)..end].Trim();
            var parts = line[(end + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
                continue;
            string? address = null;
            string status = string.Empty;
            int? life = null;
            if (cache)
            {
                address = parts.Length > 1 ? parts[1] : null;
                life = parts.Length > 2 && int.TryParse(parts[2], out var seconds) ? seconds : null;
                status = life?.ToString() ?? string.Empty;
            }
            else if (parts.Length > 1)
            {
                status = parts[1];
            }

            yield return new NetworkNetBiosName(
                cache ? "Cache" : "Local",
                name,
                suffix,
                SuffixName(suffix),
                parts[0],
                status,
                address,
                life,
                string.IsNullOrWhiteSpace(adapter) ? null : adapter,
                string.IsNullOrWhiteSpace(node) ? null : node,
                cache);
        }
    }

    private static string Between(string line, char open, char close)
    {
        var start = line.IndexOf(open);
        var end = line.IndexOf(close);
        return start < 0 || end <= start ? string.Empty : line[(start + 1)..end];
    }

    private static string SuffixName(string suffix)
        => suffix.ToUpperInvariant() switch
        {
            "00" => "Workstation",
            "01" => "Messenger",
            "03" => "Messenger",
            "06" => "RAS Server",
            "1B" => "Domain Master",
            "1C" => "Domain Controller",
            "1D" => "Master Browser",
            "1E" => "Browser Election",
            "1F" => "NetDDE",
            "20" => "File Server",
            "21" => "RAS Client",
            "BE" => "Network Monitor Agent",
            "BF" => "Network Monitor",
            _ => "Other"
        };

    private static IEnumerable<string> Lines(string text)
        => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
