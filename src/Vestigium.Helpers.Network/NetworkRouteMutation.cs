using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkRouteMutation
{
    private const uint ErrorAccessDenied = 5;
    private const uint ErrorInvalidParameter = 87;
    private const uint ErrorNotFound = 1168;
    private const int ProtoNetMgmt = 3;
    private const int TypeIndirect = 4;

    public static void Add(NetworkRouteChange change)
    {
        var spec = NetworkRouteSpec.Parse(change);
        if (!OperatingSystem.IsWindows())
        {
            NetworkRouteNetlink.Add(change);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"AddRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} linux-netlink");
            return;
        }

        if (spec.IsIPv6)
        {
            if (spec.InterfaceIndex < 1)
                spec = spec with { InterfaceIndex = ResolveInterfaceIndex(change.InterfaceIndex, TryFirstIpv4Index()) };
            NetworkRouteWindowsV6.Add(spec);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"AddRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6");
            return;
        }

        var row = BindV4(change, spec);
        var code = CreateIpForwardEntry(ref row);
        if (code != 0)
            throw Denied("AddRoute", code);
        if (change.Persistent)
            WritePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"AddRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} persist={change.Persistent}");
    }

    public static void Change(NetworkRouteChange change)
    {
        var spec = NetworkRouteSpec.Parse(change);
        if (!OperatingSystem.IsWindows())
        {
            NetworkRouteNetlink.Change(change);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"ChangeRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} linux-netlink");
            return;
        }

        if (spec.IsIPv6)
        {
            if (spec.InterfaceIndex < 1)
                spec = spec with { InterfaceIndex = ResolveInterfaceIndex(change.InterfaceIndex, TryFirstIpv4Index()) };
            NetworkRouteWindowsV6.Change(spec);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"ChangeRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6");
            return;
        }

        var row = BindV4(change, spec);
        var code = SetIpForwardEntry(ref row);
        if (code != 0)
            throw Denied("ChangeRoute", code);
        if (change.Persistent)
            WritePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"ChangeRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway}");
    }

    public static void Remove(NetworkRouteChange change)
    {
        var spec = NetworkRouteSpec.Parse(change);
        if (!OperatingSystem.IsWindows())
        {
            NetworkRouteNetlink.Remove(change);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"RemoveRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} linux-netlink");
            return;
        }

        if (spec.IsIPv6)
        {
            if (spec.InterfaceIndex < 1)
                spec = spec with { InterfaceIndex = ResolveInterfaceIndex(change.InterfaceIndex, TryFirstIpv4Index()) };
            NetworkRouteWindowsV6.Remove(spec);
            NetworkLog.Success(HelperLog.Subcategories.Route, $"RemoveRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6");
            return;
        }

        var row = BindV4(change, spec);
        var code = DeleteIpForwardEntry(ref row);
        if (code != 0 && code != ErrorNotFound)
            throw Denied("RemoveRoute", code);
        DeletePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"RemoveRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway}");
    }

    private static MibIpForwardRow BindV4(NetworkRouteChange change, NetworkRouteSpec spec)
    {
        int ifIndex;
        if (change.InterfaceIndex is { } specified)
            ifIndex = ResolveInterfaceIndex(specified, specified);
        else
            ifIndex = ResolveInterfaceIndex(null, TryFirstIpv4Index());

        return new MibIpForwardRow
        {
            dwForwardDest = ToUint(spec.Destination),
            dwForwardMask = ToUint(IPAddress.Parse(Ipv4Prefix.MaskFromPrefix(spec.PrefixLength))),
            dwForwardNextHop = ToUint(spec.Gateway),
            dwForwardIfIndex = ifIndex,
            dwForwardType = TypeIndirect,
            dwForwardProto = ProtoNetMgmt,
            dwForwardMetric1 = spec.Metric
        };
    }

    internal static int ResolveInterfaceIndex(int? callerIndex, int? discoveredIndex)
    {
        if (callerIndex is { } specified)
        {
            switch (specified)
            {
                case < 1:
                    HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(ResolveInterfaceIndex), $"ifIndex={specified}");
                    throw new ArgumentOutOfRangeException(nameof(NetworkRouteChange.InterfaceIndex), "InterfaceIndex must be 1 or greater.");
                default:
                    return specified;
            }
        }

        if (discoveredIndex is { } found and >= 1)
            return found;

        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(ResolveInterfaceIndex), "interface required");
        throw new ArgumentException("Route mutation requires InterfaceIndex when no up IPv4 interface is present.", nameof(NetworkRouteChange.InterfaceIndex));
    }

    internal static int? TryFirstIpv4Index()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                try
                {
                    var index = nic.GetIPProperties().GetIPv4Properties().Index;
                    if (index >= 1)
                        return index;
                }
                catch (NetworkInformationException)
                {
                }
            }
        }
        catch (NetworkInformationException)
        {
        }

        return null;
    }

    internal static int FirstIpv4Index()
        => ResolveInterfaceIndex(null, TryFirstIpv4Index());

    private static uint ToUint(IPAddress ip)
        => BitConverter.ToUInt32(ip.GetAddressBytes(), 0);

    internal static NetworkRouteDenied LinuxWriteDenied(string verb)
    {
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, verb, "Linux route write requires CAP_NET_ADMIN");
        return new NetworkRouteDenied("Linux route write requires CAP_NET_ADMIN.");
    }

    internal static NetworkRouteDenied Denied(string verb, uint code)
    {
        var message = code switch
        {
            ErrorAccessDenied => verb + " denied. Administrator rights required.",
            ErrorInvalidParameter => verb + " rejected. Destination, mask, gateway, or interface is invalid.",
            _ => verb + " failed. Win32=" + code
        };
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, verb, message);
        return new NetworkRouteDenied(message);
    }

    internal static NetworkRouteDenied PersistentAccessDenied(Exception ex)
    {
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(DeletePersistent), "persistent route access denied");
        return new NetworkRouteDenied("Persistent route requires write access to HKLM PersistentRoutes.", ex);
    }

    [SupportedOSPlatform("windows")]
    private static void WritePersistent(NetworkRouteChange change)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(NetworkRouteKeys.PersistentRoutes, writable: true);
            if (key is null)
                throw new NetworkRouteDenied("Persistent route registry key is not writable.");
            key.SetValue(PersistentName(change), "", RegistryValueKind.String);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            throw PersistentAccessDenied(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DeletePersistent(NetworkRouteChange change)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(NetworkRouteKeys.PersistentRoutes, writable: true);
            key?.DeleteValue(PersistentName(change), throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            throw PersistentAccessDenied(ex);
        }
    }

    internal static string PersistentName(NetworkRouteChange change)
        => $"{change.Destination},{Ipv4Prefix.MaskFromPrefix(change.PrefixLength)},{change.Gateway},{Math.Max(1, change.Metric)}";

    [StructLayout(LayoutKind.Sequential)]
    private struct MibIpForwardRow
    {
        public uint dwForwardDest;
        public uint dwForwardMask;
        public uint dwForwardPolicy;
        public uint dwForwardNextHop;
        public int dwForwardIfIndex;
        public int dwForwardType;
        public int dwForwardProto;
        public uint dwForwardAge;
        public uint dwForwardNextHopAS;
        public int dwForwardMetric1;
        public int dwForwardMetric2;
        public int dwForwardMetric3;
        public int dwForwardMetric4;
        public int dwForwardMetric5;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint CreateIpForwardEntry(ref MibIpForwardRow row);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint SetIpForwardEntry(ref MibIpForwardRow row);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint DeleteIpForwardEntry(ref MibIpForwardRow row);
}
