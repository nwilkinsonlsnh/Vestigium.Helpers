using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Network;

internal static class NeighborResolve
{
    public static string? Resolve(IPAddress address)
    {
        if (OperatingSystem.IsWindows())
        {
            var resolved = ResolveWindows(address);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
            return null;
        }

        return ResolveLinuxArpLine(address);
    }

    private static string? ResolveWindows(IPAddress address)
    {
        foreach (var index in LocalIndexes(address.AddressFamily))
        {
            var mac = ResolveIpNet(address, index);
            if (!string.IsNullOrWhiteSpace(mac))
                return mac;
        }

        return ResolveIpNet(address, 0);
    }

    private static IEnumerable<int> LocalIndexes(AddressFamily family)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;
            try
            {
                if (family == AddressFamily.InterNetworkV6)
                {
                    var v6 = nic.GetIPProperties().GetIPv6Properties();
                    if (v6 is not null)
                        yield return v6.Index;
                }
                else
                {
                    var v4 = nic.GetIPProperties().GetIPv4Properties();
                    if (v4 is not null)
                        yield return v4.Index;
                }
            }
            catch (NetworkInformationException)
            {
            }
        }
    }

    private static string? ResolveIpNet(IPAddress address, int index)
    {
        var row = new byte[128];
        WriteAddress(row, address);
        BitConverter.GetBytes(index).CopyTo(row, 36);
        var status = ResolveIpNetEntry2(row, 0);
        if (status != 0)
            status = GetIpNetEntry2(row);
        if (status != 0)
            return null;
        var length = BitConverter.ToInt32(row, 72);
        if (length < 6)
            return null;
        var mac = row.AsSpan(40, 6);
        if (mac.ToArray().All(b => b == 0))
            return null;
        return string.Join(':', mac.ToArray().Select(b => b.ToString("X2")));
    }

    private static void WriteAddress(byte[] row, IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            BitConverter.GetBytes((ushort)2).CopyTo(row, 0);
            bytes.CopyTo(row, 4);
            return;
        }

        BitConverter.GetBytes((ushort)23).CopyTo(row, 0);
        bytes.CopyTo(row, 8);
    }

    private static string? ResolveLinuxArpLine(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return null;
        var path = NetworkTestHooks.ProcPath("/proc/net/arp");
        if (!File.Exists(path))
            return null;
        var needle = address.ToString();
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                continue;
            if (!parts[0].Equals(needle, StringComparison.Ordinal))
                continue;
            var mac = parts[3];
            if (mac.Length < 11 || mac.All(c => c is '0' or ':'))
                return null;
            return mac.ToUpperInvariant();
        }

        return null;
    }

    [DllImport("iphlpapi.dll")]
    private static extern uint ResolveIpNetEntry2(byte[] row, nint sourceAddress);

    [DllImport("iphlpapi.dll")]
    private static extern uint GetIpNetEntry2(byte[] row);
}
