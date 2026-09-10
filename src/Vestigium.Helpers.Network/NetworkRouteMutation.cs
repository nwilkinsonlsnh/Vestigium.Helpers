using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkRouteMutation
{
    const uint ErrorAccessDenied = 5;
    const uint ErrorInvalidParameter = 87;
    const uint ErrorNotFound = 1168;
    const int ProtoNetMgmt = 3;
    const int TypeIndirect = 4;
    const string PersistentKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\PersistentRoutes";

    public static void Add(NetworkRouteChange change)
    {
        var row = Bind(change);
        if (!OperatingSystem.IsWindows())
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(Add), "Linux route write is not implemented");
            throw new PlatformNotSupportedException("AddRoute is Windows-only in v1. Linux is a typed deny, not ip route.");
        }

        var code = CreateIpForwardEntry(ref row);
        if (code != 0)
            throw Denied("AddRoute", code);
        if (change.Persistent)
            WritePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"AddRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} persist={change.Persistent}");
    }

    public static void Change(NetworkRouteChange change)
    {
        var row = Bind(change);
        if (!OperatingSystem.IsWindows())
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(Change), "Linux route write is not implemented");
            throw new PlatformNotSupportedException("ChangeRoute is Windows-only in v1.");
        }

        var code = SetIpForwardEntry(ref row);
        if (code != 0)
            throw Denied("ChangeRoute", code);
        if (change.Persistent)
            WritePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"ChangeRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway}");
    }

    public static void Remove(NetworkRouteChange change)
    {
        var row = Bind(change);
        if (!OperatingSystem.IsWindows())
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(Remove), "Linux route write is not implemented");
            throw new PlatformNotSupportedException("RemoveRoute is Windows-only in v1.");
        }

        var code = DeleteIpForwardEntry(ref row);
        if (code != 0 && code != ErrorNotFound)
            throw Denied("RemoveRoute", code);
        DeletePersistent(change);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"RemoveRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway}");
    }

    static MibIpForwardRow Bind(NetworkRouteChange change)
    {
        var dest = HelperGuard.NotBlank(change.Destination, nameof(change.Destination));
        var gw = HelperGuard.NotBlank(change.Gateway, nameof(change.Gateway));
        if (change.PrefixLength is < 0 or > 32)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(Bind), $"prefix={change.PrefixLength}");
            throw new ArgumentOutOfRangeException(nameof(change.PrefixLength), "PrefixLength must be 0–32.");
        }

        if (!IPAddress.TryParse(dest, out var destIp) || destIp.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Destination must be IPv4.", nameof(change.Destination));
        if (!IPAddress.TryParse(gw, out var gwIp) || gwIp.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Gateway must be IPv4.", nameof(change.Gateway));

        var ifIndex = change.InterfaceIndex ?? FirstIpv4Index();
        return new MibIpForwardRow
        {
            dwForwardDest = ToUint(destIp),
            dwForwardMask = ToUint(IPAddress.Parse(Ipv4Prefix.MaskFromPrefix(change.PrefixLength))),
            dwForwardNextHop = ToUint(gwIp),
            dwForwardIfIndex = ifIndex,
            dwForwardType = TypeIndirect,
            dwForwardProto = ProtoNetMgmt,
            dwForwardMetric1 = Math.Max(1, change.Metric)
        };
    }

    static int FirstIpv4Index()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                try
                {
                    return nic.GetIPProperties().GetIPv4Properties().Index;
                }
                catch (NetworkInformationException)
                {
                }
            }
        }
        catch (NetworkInformationException)
        {
        }

        return 1;
    }

    static uint ToUint(IPAddress ip)
        => BitConverter.ToUInt32(ip.GetAddressBytes(), 0);

    static NetworkRouteDenied Denied(string verb, uint code)
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

    static void WritePersistent(NetworkRouteChange change)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(PersistentKey, writable: true);
            if (key is null)
                throw new NetworkRouteDenied("Persistent route registry key is not writable.");
            key.SetValue(PersistentName(change), "", RegistryValueKind.String);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            throw new NetworkRouteDenied("Persistent route requires write access to HKLM PersistentRoutes.", ex);
        }
    }

    static void DeletePersistent(NetworkRouteChange change)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(PersistentKey, writable: true);
            key?.DeleteValue(PersistentName(change), throwOnMissingValue: false);
        }
        catch (Exception)
        {
        }
    }

    static string PersistentName(NetworkRouteChange change)
        => $"{change.Destination},{Ipv4Prefix.MaskFromPrefix(change.PrefixLength)},{change.Gateway},{Math.Max(1, change.Metric)}";

    [StructLayout(LayoutKind.Sequential)]
    struct MibIpForwardRow
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
    static extern uint CreateIpForwardEntry(ref MibIpForwardRow row);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint SetIpForwardEntry(ref MibIpForwardRow row);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint DeleteIpForwardEntry(ref MibIpForwardRow row);
}
