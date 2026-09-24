using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Network;

internal static class NeighborResolve
{
    private const int AfInet = 2;
    private const int AfInet6 = 23;
    private const int PhysLen = 32;

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

    private static List<int> LocalIndexes(AddressFamily family)
    {
        var indexes = new List<int>();
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
                        indexes.Add(v6.Index);
                }
                else
                {
                    var v4 = nic.GetIPProperties().GetIPv4Properties();
                    if (v4 is not null)
                        indexes.Add(v4.Index);
                }
            }
            catch (NetworkInformationException)
            {
            }
        }

        return indexes;
    }

    private static string? ResolveIpNet(IPAddress address, int index)
    {
        var row = new MibIpNetRow2 { InterfaceIndex = index, PhysicalAddress = new byte[PhysLen] };
        FillAddress(ref row, address);
        var status = ResolveIpNetEntry2(ref row, 0);
        if (status != 0)
            status = GetIpNetEntry2(ref row);
        if (status != 0)
            return null;
        return FormatMac(row.PhysicalAddress, row.PhysicalAddressLength);
    }

    internal static string? FormatMac(byte[]? physical, uint length)
    {
        if (physical is null || length < 6 || physical.Length < 6)
            return null;
        if (physical[0] == 0 && physical[1] == 0 && physical[2] == 0 && physical[3] == 0 && physical[4] == 0 && physical[5] == 0)
            return null;
        return string.Join(':', physical.Take(6).Select(b => b.ToString("X2")));
    }

    private static void FillAddress(ref MibIpNetRow2 row, IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            row.Address.Family = AfInet;
            row.Address.Ipv4Address = BitConverter.ToUInt32(bytes, 0);
            return;
        }

        row.Address.Family = AfInet6;
        if (bytes.Length >= 16)
        {
            row.Address.Ipv6B0 = BitConverter.ToUInt64(bytes, 0);
            row.Address.Ipv6B1 = BitConverter.ToUInt64(bytes, 8);
        }
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

    [StructLayout(LayoutKind.Sequential)]
    private struct SockAddrInet
    {
        public ushort Family;
        public ushort Port;
        public uint Ipv4Address;
        public uint FlowInfo;
        public ulong Ipv6B0;
        public ulong Ipv6B1;
        public uint ScopeId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibIpNetRow2
    {
        public SockAddrInet Address;
        public ulong InterfaceLuid;
        public int InterfaceIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = PhysLen)]
        public byte[] PhysicalAddress;
        public uint PhysicalAddressLength;
        public int State;
        public byte Flags;
        public uint ReachabilityTime;
    }

    [DllImport("iphlpapi.dll")]
    private static extern uint ResolveIpNetEntry2(ref MibIpNetRow2 row, nint sourceAddress);

    [DllImport("iphlpapi.dll")]
    private static extern uint GetIpNetEntry2(ref MibIpNetRow2 row);
}
