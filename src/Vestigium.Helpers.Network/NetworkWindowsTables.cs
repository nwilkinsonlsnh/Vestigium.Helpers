using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Network;

internal static class NetworkWindowsTables
{
    private const int AfInet = 2;
    private const int AfInet6 = 23;
    private const int TcpTableOwnerPidAll = 5;
    private const int UdpTableOwnerPid = 1;
    private const int Ipv6RowSize = 104;
    private const int Ipv6NeighborRowSize = 88;

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
        if (family is RouteFamily.All or RouteFamily.Pv4)
            rows.AddRange(ReadIpv4Routes());
        if (family is RouteFamily.All or RouteFamily.Pv6)
            rows.AddRange(ReadIpv6Routes());
        return rows;
    }

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors()
    {
        var rows = new List<NetworkNeighbor>();
        rows.AddRange(ReadIpv4Neighbors());
        rows.AddRange(ReadIpv6Neighbors());
        return rows;
    }

    private static IEnumerable<NetworkNeighbor> ReadIpv4Neighbors()
    {
        var size = 0;
        GetIpNetTable(IntPtr.Zero, ref size, true);
        if (size <= 0)
            yield break;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetIpNetTable(buffer, ref size, true) != 0)
                yield break;
            var count = Marshal.ReadInt32(buffer);
            var offset = buffer + 4;
            for (var i = 0; i < count; i++)
            {
                var index = Marshal.ReadInt32(offset);
                var physLen = Marshal.ReadInt32(offset + 4);
                var mac = ReadMac(offset + 8, physLen);
                var addr = ReadIpv4(offset + 16);
                var type = Marshal.ReadInt32(offset + 20);
                yield return new NetworkNeighbor(
                    AddressFamily.InterNetwork,
                    addr,
                    mac,
                    InterfaceName(index),
                    NeighborType(type));
                offset += 24;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IEnumerable<NetworkNeighbor> ReadIpv6Neighbors()
    {
        if (GetIpNetTable2(AfInet6, out var table) != 0 || table == 0)
            yield break;

        try
        {
            var count = Marshal.ReadInt32(table);
            var row = table + 8;
            for (var i = 0; i < count; i++)
            {
                var address = ReadSockAddress(row);
                var index = Marshal.ReadInt32(row + 28);
                var physLen = Marshal.ReadInt32(row + 72);
                var mac = ReadMac(row + 40, physLen);
                var state = Marshal.ReadInt32(row + 76);
                yield return new NetworkNeighbor(
                    AddressFamily.InterNetworkV6,
                    address,
                    mac,
                    InterfaceName(index),
                    NeighborState(state));
                row += Ipv6NeighborRowSize;
            }
        }
        finally
        {
            FreeMibTable(table);
        }
    }

    private static IEnumerable<(int Local, int Remote, int Pid)> ReadTcpOwners()
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

    private static IEnumerable<(int Local, int Pid)> ReadUdpOwners()
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

    private static IEnumerable<NetworkRoute> ReadIpv4Routes()
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
                    ProtocolName(proto));
                offset += 56;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IEnumerable<NetworkRoute> ReadIpv6Routes()
    {
        if (GetIpForwardTable2(AfInet6, out var table) != 0 || table == 0)
            yield break;

        try
        {
            var count = Marshal.ReadInt32(table);
            var row = table + 8;
            for (var i = 0; i < count; i++)
            {
                var ifIndex = Marshal.ReadInt32(row + 8);
                var prefixLength = Marshal.ReadByte(row + 40);
                var dest = ReadSockAddress(row + 12);
                var gateway = ReadSockAddress(row + 44);
                var metric = Marshal.ReadInt32(row + 84);
                var proto = Marshal.ReadInt32(row + 88);
                yield return new NetworkRoute(
                    AddressFamily.InterNetworkV6,
                    dest,
                    prefixLength,
                    prefixLength.ToString(),
                    gateway,
                    InterfaceName(ifIndex),
                    ifIndex,
                    metric,
                    proto == 3,
                    ProtocolName(proto));
                row += Ipv6RowSize;
            }
        }
        finally
        {
            FreeMibTable(table);
        }
    }

    private static string ReadSockAddress(nint ptr)
    {
        var family = Marshal.ReadInt16(ptr);
        if (family == AfInet)
            return ReadIpv4(ptr + 4);
        if (family != AfInet6)
            return string.Empty;

        var bytes = new byte[16];
        Marshal.Copy(ptr + 8, bytes, 0, 16);
        return new IPAddress(bytes).ToString();
    }

    private static string ProtocolName(int proto) => proto switch
    {
        2 => "local",
        3 => "netmgmt",
        4 => "icmp",
        8 => "rip",
        13 => "ospf",
        14 => "bgp",
        19 => "dhcp",
        _ => proto.ToString()
    };

    private static string ReadIpv4(nint ptr)
    {
        var raw = Marshal.ReadInt32(ptr);
        var bytes = BitConverter.GetBytes(unchecked((uint)raw));
        return new IPAddress(bytes).ToString();
    }

    private static string? ReadMac(nint ptr, int length)
    {
        if (length <= 0)
            return null;
        var bytes = new byte[Math.Min(length, 8)];
        Marshal.Copy(ptr, bytes, 0, bytes.Length);
        return bytes.All(b => b == 0) ? null : string.Join(":", bytes.Take(6).Select(b => b.ToString("X2")));
    }

    private static int PortFromNetwork(int networkOrder)
    {
        var bytes = BitConverter.GetBytes(networkOrder);
        return (bytes[0] << 8) | bytes[1];
    }

    private static string? InterfaceName(int index)
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => InterfaceIndex(n) == index)?.Name;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    private static int InterfaceIndex(NetworkInterface nic)
    {
        try
        {
            var properties = nic.GetIPProperties();
            try
            {
                var v4 = properties.GetIPv4Properties();
                if (v4.Index == 0)
                    return properties.GetIPv6Properties().Index;
                return v4.Index;
            }
            catch (NetworkInformationException)
            {
                return properties.GetIPv6Properties().Index;
            }
        }
        catch (NetworkInformationException)
        {
            return -1;
        }
    }

    internal static string NeighborType(int type) => type switch
    {
        3 => "Dynamic",
        4 => "Static",
        2 => "Invalid",
        _ => "Other"
    };

    private static string NeighborState(int state) => state switch
    {
        1 => "Unreachable",
        2 => "Incomplete",
        3 => "Probe",
        4 => "Delay",
        5 => "Stale",
        6 => "Reachable",
        7 => "Permanent",
        _ => "Other"
    };

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(nint table, ref int size, bool order, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(nint table, ref int size, bool order, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetIpForwardTable(nint table, ref int size, bool order);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetIpForwardTable2(ushort family, out nint table);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetIpNetTable2(ushort family, out nint table);

    [DllImport("iphlpapi.dll")]
    private static extern void FreeMibTable(nint table);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetIpNetTable(nint table, ref int size, bool order);
}
