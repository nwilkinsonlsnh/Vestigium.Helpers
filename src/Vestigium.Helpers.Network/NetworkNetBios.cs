using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace Vestigium.Helpers.Network;

internal static class NetworkNetBios
{
    public static NetBiosInfo Capture()
    {
        var workstation = NetworkInventoryEngine.Capture();
        var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
        var adapters = workstation.Adapters
            .Select(adapter => new NetBiosAdapterStatus(adapter.Name, NetbiosOverTcp.Unknown, adapter.Description))
            .ToArray();
        return new NetBiosInfo(workstation.HostName, string.IsNullOrWhiteSpace(domain) ? null : domain, adapters);
    }

    public static IReadOnlyList<NetworkNetBiosName> Read()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        var rows = new List<NetworkNetBiosName>();
        rows.AddRange(Parse(Run("nbtstat", "-n"), cache: false));
        rows.AddRange(Parse(Run("nbtstat", "-c"), cache: true));
        return rows;
    }

    public static NetworkNetBiosStats ReadStats()
    {
        if (!OperatingSystem.IsWindows())
            return new NetworkNetBiosStats(0, 0, 0, 0, null);

        var broadcast = 0;
        var server = 0;
        var registeredBroadcast = 0;
        var registeredServer = 0;
        string? node = null;
        var registration = false;
        foreach (var line in Lines(Run("nbtstat", "-r")))
        {
            if (line.Contains("Registration", StringComparison.OrdinalIgnoreCase))
                registration = true;
            if (line.StartsWith("Node Type", StringComparison.OrdinalIgnoreCase) || line.StartsWith("NetBIOS Node", StringComparison.OrdinalIgnoreCase))
                node = line.Split('=').LastOrDefault()?.Trim();
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

        return new NetworkNetBiosStats(broadcast, server, registeredBroadcast, registeredServer, node);
    }

    private static int Count(string line)
    {
        var mark = line.LastIndexOf('=');
        if (mark < 0)
            return 0;
        return int.TryParse(line[(mark + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;
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
            var type = parts[0];
            string? address = null;
            string status = string.Empty;
            int? life = null;
            if (cache)
            {
                address = parts.Length > 1 ? parts[1] : null;
                life = parts.Length > 2 && int.TryParse(parts[2], out var seconds) ? seconds : null;
                status = life?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
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
                type,
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
