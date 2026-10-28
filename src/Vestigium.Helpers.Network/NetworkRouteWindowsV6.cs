using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Vestigium.Helpers.Network;

internal static class NetworkRouteWindowsV6
{
    private const uint ErrorNotFound = 1168;
    private const int ProtoNetMgmt = 3;

    [SupportedOSPlatform("windows")]
    public static void Add(NetworkRouteSpec spec) => Apply(spec, create: true, replace: false);

    [SupportedOSPlatform("windows")]
    public static void Change(NetworkRouteSpec spec) => Apply(spec, create: true, replace: true);

    [SupportedOSPlatform("windows")]
    public static void Remove(NetworkRouteSpec spec) => Apply(spec, create: false, replace: false);

    [SupportedOSPlatform("windows")]
    private static void Apply(NetworkRouteSpec spec, bool create, bool replace)
    {
        InitializeIpForwardEntry(out var row);
        row.InterfaceIndex = spec.InterfaceIndex;
        row.DestinationPrefix = Prefix(spec.Destination, (byte)spec.PrefixLength);
        row.NextHop = Inet(spec.Gateway);
        row.Metric = (uint)spec.Metric;
        row.Protocol = ProtoNetMgmt;

        uint code;
        if (!create)
        {
            code = DeleteIpForwardEntry2(ref row);
            if (code != 0 && code != ErrorNotFound)
                throw NetworkRouteMutation.Denied("RemoveRoute", code);
            return;
        }

        code = replace ? SetIpForwardEntry2(ref row) : CreateIpForwardEntry2(ref row);
        if (code != 0 && replace)
            code = CreateIpForwardEntry2(ref row);
        if (code != 0)
            throw NetworkRouteMutation.Denied(replace ? "ChangeRoute" : "AddRoute", code);
    }

    private static IpAddressPrefix Prefix(IPAddress address, byte length)
        => new() { Prefix = Inet(address), PrefixLength = length };

    private static SockaddrInet Inet(IPAddress address)
    {
        var row = new SockaddrInet();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            row.Family = (ushort)AddressFamily.InterNetwork;
            row.Addr0 = bytes[0];
            row.Addr1 = bytes[1];
            row.Addr2 = bytes[2];
            row.Addr3 = bytes[3];
            return row;
        }

        row.Family = (ushort)AddressFamily.InterNetworkV6;
        row.Addr0 = bytes[0];
        row.Addr1 = bytes[1];
        row.Addr2 = bytes[2];
        row.Addr3 = bytes[3];
        row.Addr4 = bytes[4];
        row.Addr5 = bytes[5];
        row.Addr6 = bytes[6];
        row.Addr7 = bytes[7];
        row.Addr8 = bytes[8];
        row.Addr9 = bytes[9];
        row.Addr10 = bytes[10];
        row.Addr11 = bytes[11];
        row.Addr12 = bytes[12];
        row.Addr13 = bytes[13];
        row.Addr14 = bytes[14];
        row.Addr15 = bytes[15];
        if (address.ScopeId is > 0 and <= uint.MaxValue)
            row.ScopeId = (uint)address.ScopeId;
        return row;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SockaddrInet
    {
        public ushort Family;
        public ushort Port;
        public uint FlowInfo;
        public byte Addr0, Addr1, Addr2, Addr3, Addr4, Addr5, Addr6, Addr7;
        public byte Addr8, Addr9, Addr10, Addr11, Addr12, Addr13, Addr14, Addr15;
        public uint ScopeId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IpAddressPrefix
    {
        public SockaddrInet Prefix;
        public byte PrefixLength;
        public byte Pad0, Pad1, Pad2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MibIpForwardRow2
    {
        public ulong InterfaceLuid;
        public int InterfaceIndex;
        public int PadIndex;
        public IpAddressPrefix DestinationPrefix;
        public SockaddrInet NextHop;
        public byte SitePrefixLength;
        public byte PadA, PadB, PadC;
        public uint ValidLifetime;
        public uint PreferredLifetime;
        public uint Metric;
        public int Protocol;
        public byte Loopback;
        public byte AutoconfigureAddress;
        public byte Publish;
        public byte Immortal;
        public uint Age;
        public int Origin;
    }

    [DllImport("iphlpapi.dll")]
    private static extern void InitializeIpForwardEntry(out MibIpForwardRow2 row);

    [DllImport("iphlpapi.dll")]
    private static extern uint CreateIpForwardEntry2(ref MibIpForwardRow2 row);

    [DllImport("iphlpapi.dll")]
    private static extern uint SetIpForwardEntry2(ref MibIpForwardRow2 row);

    [DllImport("iphlpapi.dll")]
    private static extern uint DeleteIpForwardEntry2(ref MibIpForwardRow2 row);
}
