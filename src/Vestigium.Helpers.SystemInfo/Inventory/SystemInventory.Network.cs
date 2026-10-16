using System.Management;
using System.Net.NetworkInformation;

namespace Vestigium.Helpers.SystemInfo;

public static partial class SystemInventory
{
    private const string CimScope = @"\\.\root\StandardCimv2";

    /// <summary>
    /// Reads adapters, routes, ARP, listeners, and TCP connections.
    /// Each section is Fact.Unavailable on failure. Partial success is allowed.
    /// No truncation. Host decides the cap.
    /// </summary>
    public static NetworkInventory ReadNetwork()
    {
        return new NetworkInventory(
            ReadAdapters(),
            ReadRoutes(),
            ReadArp(),
            ReadListeners(),
            ReadTcp());
    }

    private static Fact<IReadOnlyList<NetworkAdapter>> ReadAdapters()
    {
        try
        {
            var list = new List<NetworkAdapter>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = nic.GetIPProperties();
                var ipv4 = props.GetIPv4Properties();
                list.Add(new NetworkAdapter(
                    nic.Name,
                    nic.Description,
                    nic.OperationalStatus.ToString(),
                    FormatMac(nic.GetPhysicalAddress()),
                    props.UnicastAddresses.Select(a => a.Address.ToString()).ToArray(),
                    props.GatewayAddresses.Select(g => g.Address.ToString()).ToArray(),
                    props.DnsAddresses.Select(d => d.ToString()).ToArray(),
                    props.DnsSuffix,
                    nic.Speed > 0 ? nic.Speed : 0,
                    nic.NetworkInterfaceType.ToString(),
                    ipv4?.IsDhcpEnabled == true));
            }
            return Fact<IReadOnlyList<NetworkAdapter>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<NetworkAdapter>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<NetworkRoute>> ReadRoutes()
    {
        try
        {
            var list = new List<NetworkRoute>();
            foreach (var mo in Query("SELECT Destination, Mask, NextHop, InterfaceIndex, Metric1 FROM Win32_IP4RouteTable"))
            {
                using (mo)
                {
                    list.Add(new NetworkRoute(
                        String(mo, "Destination") ?? "",
                        String(mo, "Mask") ?? "",
                        String(mo, "NextHop") ?? "",
                        UInt(mo, "InterfaceIndex"),
                        UInt(mo, "Metric1")));
                }
            }
            return Fact<IReadOnlyList<NetworkRoute>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<NetworkRoute>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<ArpEntry>> ReadArp()
    {
        try
        {
            var scope = new ManagementScope(CimScope);
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT IPAddress, LinkLayerAddress, State, InterfaceIndex FROM MSFT_NetNeighbor"));
            using var results = searcher.Get();
            var list = new List<ArpEntry>();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    list.Add(new ArpEntry(
                        String(mo, "IPAddress") ?? "",
                        String(mo, "LinkLayerAddress") ?? "",
                        UInt(mo, "State"),
                        UInt(mo, "InterfaceIndex")));
                }
            }
            return Fact<IReadOnlyList<ArpEntry>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<ArpEntry>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<Listener>> ReadListeners()
    {
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var list = new List<Listener>();
            foreach (var ep in props.GetActiveTcpListeners())
                list.Add(new Listener("TCP", ep.Address.ToString(), ep.Port));
            foreach (var ep in props.GetActiveUdpListeners())
                list.Add(new Listener("UDP", ep.Address.ToString(), ep.Port));
            return Fact<IReadOnlyList<Listener>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<Listener>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<TcpConnection>> ReadTcp()
    {
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var list = new List<TcpConnection>();
            foreach (var c in props.GetActiveTcpConnections())
            {
                list.Add(new TcpConnection(
                    c.LocalEndPoint.Address.ToString(),
                    c.LocalEndPoint.Port,
                    c.RemoteEndPoint.Address.ToString(),
                    c.RemoteEndPoint.Port,
                    (uint)c.State));
            }
            return Fact<IReadOnlyList<TcpConnection>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<TcpConnection>>.Unavailable();
        }
    }

    private static string FormatMac(PhysicalAddress? address)
    {
        if (address is null)
            return "";
        var bytes = address.GetAddressBytes();
        return bytes.Length == 0 ? "" : string.Join("-", bytes.Select(b => b.ToString("X2")));
    }
}
