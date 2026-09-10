using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

internal static class NetworkLinuxTables
{
    public static IReadOnlyDictionary<(TransportProtocol, int, int), int> GetOwnerPids()
    {
        var map = new Dictionary<(TransportProtocol, int, int), int>();
        ReadProcNet("tcp", TransportProtocol.Tcp, map);
        ReadProcNet("tcp6", TransportProtocol.Tcp, map);
        ReadProcNet("udp", TransportProtocol.Udp, map);
        ReadProcNet("udp6", TransportProtocol.Udp, map);
        return map;
    }

    public static IReadOnlyList<NetworkRoute> GetRoutes(RouteFamily family)
    {
        var rows = new List<NetworkRoute>();
        if (family is RouteFamily.All or RouteFamily.IPv4)
            ReadIpv4Routes(rows);
        if (family is RouteFamily.All or RouteFamily.IPv6)
            ReadIpv6Routes(rows);
        return rows;
    }

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors()
    {
        var rows = new List<NetworkNeighbor>();
        var path = "/proc/net/arp";
        if (!File.Exists(path))
            return rows;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                continue;
            var flags = ParseHex(parts[2]);
            var state = flags switch
            {
                0x02 => "Reachable",
                0x04 => "Permanent",
                0x08 => "Failed",
                _ => "Incomplete"
            };
            rows.Add(new NetworkNeighbor(
                AddressFamily.InterNetwork,
                parts[0],
                parts[3] == "00:00:00:00:00:00" ? null : parts[3],
                parts[5],
                state));
        }

        return rows;
    }

    static void ReadProcNet(string file, TransportProtocol protocol, Dictionary<(TransportProtocol, int, int), int> map)
    {
        var path = "/proc/net/" + file;
        if (!File.Exists(path))
            return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10)
                continue;
            if (!TryParseHexEndpoint(parts[1], out var localPort))
                continue;
            TryParseHexEndpoint(parts[2], out var remotePort);
            if (!int.TryParse(parts[9], out var inode))
                continue;
            var pid = FindPidByInode(inode);
            if (pid > 0)
                map[(protocol, localPort, remotePort)] = pid;
        }
    }

    static bool TryParseHexEndpoint(string token, out int port)
    {
        port = 0;
        var colon = token.LastIndexOf(':');
        if (colon < 0)
            return false;
        return int.TryParse(token[(colon + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out port);
    }

    static int FindPidByInode(int inode)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories("/proc"))
            {
                var name = Path.GetFileName(dir);
                if (!int.TryParse(name, out var pid))
                    continue;
                var fd = Path.Combine(dir, "fd");
                if (!Directory.Exists(fd))
                    continue;
                foreach (var link in Directory.EnumerateFileSystemEntries(fd))
                {
                    try
                    {
                        var target = File.ResolveLinkTarget(link, false)?.Name ?? string.Empty;
                        if (target.Contains("socket:[" + inode + "]", StringComparison.Ordinal))
                            return pid;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        return 0;
    }

    static void ReadIpv4Routes(List<NetworkRoute> rows)
    {
        var path = "/proc/net/route";
        if (!File.Exists(path))
            return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = line.Split('	', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
                parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
                continue;
            var dest = HexIpv4(parts[1]);
            var gateway = HexIpv4(parts[2]);
            var mask = HexIpv4(parts[7]);
            var metric = int.TryParse(parts[6], out var m) ? m : 0;
            var prefix = Ipv4Prefix.PrefixFromMask(IPAddress.Parse(mask));
            rows.Add(new NetworkRoute(
                AddressFamily.InterNetwork,
                dest,
                prefix,
                mask,
                gateway,
                parts[0],
                null,
                metric,
                false,
                "kernel"));
        }
    }

    static void ReadIpv6Routes(List<NetworkRoute> rows)
    {
        var path = "/proc/net/ipv6_route";
        if (!File.Exists(path))
            return;
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10)
                continue;
            var destHex = parts[0];
            var prefix = int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var p) ? p : 0;
            var gateway = FormatIpv6Hex(parts[4]);
            var metric = int.TryParse(parts[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var m) ? m : 0;
            rows.Add(new NetworkRoute(
                AddressFamily.InterNetworkV6,
                FormatIpv6Hex(destHex),
                prefix,
                null,
                gateway,
                parts[^1],
                null,
                metric,
                false,
                "kernel"));
        }
    }

    static string HexIpv4(string hex)
    {
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return "0.0.0.0";
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes).ToString();
    }

    static string FormatIpv6Hex(string hex)
    {
        if (hex.Length != 32)
            return hex;
        try
        {
            var bytes = new byte[16];
            for (var i = 0; i < 16; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return new IPAddress(bytes).ToString();
        }
        catch (FormatException)
        {
            return hex;
        }
    }

    static int ParseHex(string value)
        => int.TryParse(value.Replace("0x", "", StringComparison.OrdinalIgnoreCase), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var n)
            ? n
            : 0;
}
