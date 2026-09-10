using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Network;

internal static class NetworkWindowsTables
{
    const int AfInet = 2;
    const int TcpTableOwnerPidAll = 5;
    const int UdpTableOwnerPid = 1;

    public static IReadOnlyDictionary<(TransportProtocol, int, int), int> GetOwnerPids()
    {
        var map = new Dictionary<(TransportProtocol, int, int), int>();
        foreach (var (local, remote, pid) in ReadTcpOwners())
            map[(TransportProtocol.Tcp, local, remote)] = pid;
        foreach (var (local, pid) in ReadUdpOwners())
            map[(TransportProtocol.Udp, local, 0)] = pid;
        return map;
    }

    public static IReadOnlyList<NetworkRoute> GetRoutes(RouteFamily family)
    {
        var rows = new List<NetworkRoute>();
        if (family is RouteFamily.All or RouteFamily.IPv4)
            rows.AddRange(ReadIpv4Routes());
        return rows;
    }

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors()
    {
        var rows = new List<NetworkNeighbor>();
        var size = 0;
        GetIpNetTable(IntPtr.Zero, ref size, true);
        if (size <= 0)
            return rows;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetIpNetTable(buffer, ref size, true) != 0)
                return rows;
            var count = Marshal.ReadInt32(buffer);
            var offset = buffer + 4;
            for (var i = 0; i < count; i++)
            {
                var index = Marshal.ReadInt32(offset);
                var physLen = Marshal.ReadInt32(offset + 4);
                var mac = ReadMac(offset + 8, physLen);
                var addr = ReadIpv4(offset + 16);
                var type = Marshal.ReadInt32(offset + 20);
                rows.Add(new NetworkNeighbor(
                    AddressFamily.InterNetwork,
                    addr,
                    mac,
                    InterfaceName(index),
                    NeighborType(type)));
                offset += 24;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return rows;
    }

    static IEnumerable<(int Local, int Remote, int Pid)> ReadTcpOwners()
    {
        var size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet, TcpTableOwnerPidAll, 0);
        if (size <= 0)
            yield break;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, true, AfInet, TcpTableOwnerPidAll, 0) != 0)
                yield break;
            var count = Marshal.ReadInt32(buffer);
            var offset = buffer + 4;
            for (var i = 0; i < count; i++)
            {
                var localPort = PortFromNetwork(Marshal.ReadInt32(offset + 8));
                var remotePort = PortFromNetwork(Marshal.ReadInt32(offset + 16));
                var pid = Marshal.ReadInt32(offset + 20);
                yield return (localPort, remotePort, pid);
                offset += 24;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static IEnumerable<(int Local, int Pid)> ReadUdpOwners()
    {
        var size = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref size, true, AfInet, UdpTableOwnerPid, 0);
        if (size <= 0)
            yield break;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedUdpTable(buffer, ref size, true, AfInet, UdpTableOwnerPid, 0) != 0)
                yield break;
            var count = Marshal.ReadInt32(buffer);
            var offset = buffer + 4;
            for (var i = 0; i < count; i++)
            {
                var localPort = PortFromNetwork(Marshal.ReadInt32(offset + 8));
                var pid = Marshal.ReadInt32(offset + 12);
                yield return (localPort, pid);
                offset += 16;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static IEnumerable<NetworkRoute> ReadIpv4Routes()
    {
        var size = 0;
        GetIpForwardTable(IntPtr.Zero, ref size, true);
        if (size <= 0)
            yield break;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetIpForwardTable(buffer, ref size, true) != 0)
                yield break;
            var count = Marshal.ReadInt32(buffer);
            var offset = buffer + 4;
            for (var i = 0; i < count; i++)
            {
                var dest = ReadIpv4(offset);
                var mask = ReadIpv4(offset + 4);
                var gateway = ReadIpv4(offset + 12);
                var ifIndex = Marshal.ReadInt32(offset + 16);
                var proto = Marshal.ReadInt32(offset + 24);
                var metric = Marshal.ReadInt32(offset + 36);
                var prefix = Ipv4Prefix.PrefixFromMask(IPAddress.Parse(mask));
                yield return new NetworkRoute(
                    AddressFamily.InterNetwork,
                    dest,
                    prefix,
                    mask,
                    gateway,
                    InterfaceName(ifIndex),
                    ifIndex,
                    metric,
                    proto == 3,
                    proto switch
                    {
                        2 => "local",
                        3 => "netmgmt",
                        4 => "icmp",
                        _ => proto.ToString()
                    });
                offset += 56;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static string ReadIpv4(nint ptr)
    {
        var raw = Marshal.ReadInt32(ptr);
        var bytes = BitConverter.GetBytes(unchecked((uint)raw));
        return new IPAddress(bytes).ToString();
    }

    static string? ReadMac(nint ptr, int length)
    {
        if (length <= 0)
            return null;
        var bytes = new byte[Math.Min(length, 8)];
        Marshal.Copy(ptr, bytes, 0, bytes.Length);
        if (bytes.All(b => b == 0))
            return null;
        return string.Join(":", bytes.Take(6).Select(b => b.ToString("X2")));
    }

    static int PortFromNetwork(int networkOrder)
    {
        var bytes = BitConverter.GetBytes(networkOrder);
        return (bytes[0] << 8) | bytes[1];
    }

    static string? InterfaceName(int index)
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n =>
                {
                    try { return n.GetIPProperties().GetIPv4Properties().Index == index; }
                    catch { return false; }
                })?.Name;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    internal static string NeighborType(int type) => type switch
    {
        3 => "Dynamic",
        4 => "Static",
        2 => "Invalid",
        _ => "Other"
    };

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetExtendedTcpTable(nint table, ref int size, bool order, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetExtendedUdpTable(nint table, ref int size, bool order, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetIpForwardTable(nint table, ref int size, bool order);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetIpNetTable(nint table, ref int size, bool order);
}
