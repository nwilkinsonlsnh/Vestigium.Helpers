using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

internal static class NetworkLinuxTables
{
    public static IReadOnlyDictionary<(TransportProtocol, int, int), int> GetOwnerPids()
    {
        return new Dictionary<(TransportProtocol, int, int), int>();
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
        var path = NetworkTestHooks.ProcPath("/proc/net/arp");
        if (!File.Exists(path))
            return rows;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (TryParseNeighbor(line, out var row))
                rows.Add(row);
        }

        return rows;
    }

    static void ReadIpv4Routes(List<NetworkRoute> rows)
    {
        var path = NetworkTestHooks.ProcPath("/proc/net/route");
        if (!File.Exists(path))
            return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (TryParseIpv4Route(line, out var row))
                rows.Add(row);
        }
    }

    static void ReadIpv6Routes(List<NetworkRoute> rows)
    {
        var path = NetworkTestHooks.ProcPath("/proc/net/ipv6_route");
        if (!File.Exists(path))
            return;
        foreach (var line in File.ReadLines(path))
        {
            if (TryParseIpv6Route(line, out var row))
                rows.Add(row);
        }
    }

    internal static string NeighborState(int flags) => flags switch
    {
        0x02 => "Reachable",
        0x04 => "Permanent",
        0x08 => "Failed",
        _ => "Incomplete"
    };

    internal static bool TryParseNeighbor(string line, out NetworkNeighbor neighbor)
    {
        neighbor = default!;
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 6)
            return false;
        neighbor = new NetworkNeighbor(
            AddressFamily.InterNetwork,
            parts[0],
            parts[3] == "00:00:00:00:00:00" ? null : parts[3],
            parts[5],
            NeighborState(ParseHex(parts[2])));
        return true;
    }

    internal static bool TryParseIpv4Route(string line, out NetworkRoute route)
    {
        route = default!;
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8)
            parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8)
            return false;
        var dest = HexIpv4(parts[1]);
        var gateway = HexIpv4(parts[2]);
        var mask = HexIpv4(parts[7]);
        var metric = int.TryParse(parts[6], out var m) ? m : 0;
        var prefix = Ipv4Prefix.PrefixFromMask(IPAddress.Parse(mask));
        route = new NetworkRoute(
            AddressFamily.InterNetwork, dest, prefix, mask, gateway, parts[0], null, metric, false, "kernel");
        return true;
    }

    internal static bool TryParseIpv6Route(string line, out NetworkRoute route)
    {
        route = default!;
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 10)
            return false;
        var prefix = int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var p) ? p : 0;
        var metric = int.TryParse(parts[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var m) ? m : 0;
        route = new NetworkRoute(
            AddressFamily.InterNetworkV6, FormatIpv6Hex(parts[0]), prefix, null, FormatIpv6Hex(parts[4]), parts[^1], null, metric, false, "kernel");
        return true;
    }

    internal static string HexIpv4(string hex)
    {
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return "0.0.0.0";
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes).ToString();
    }

    internal static string FormatIpv6Hex(string hex)
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
        catch (OverflowException)
        {
            return hex;
        }
    }

    internal static int ParseHex(string value)
        => int.TryParse(value.Replace("0x", "", StringComparison.OrdinalIgnoreCase), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var n)
            ? n : 0;
}
