using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkInventoryEngine
{
    public static WorkstationNetwork Capture(NetworkAdapterQuery? query = null)
    {
        query ??= new NetworkAdapterQuery();
        var host = Dns.GetHostName();
        string? domain = null;
        try
        {
            domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (string.IsNullOrWhiteSpace(domain))
                domain = null;
        }
        catch (NetworkInformationException)
        {
        }

        var adapters = new List<NetworkAdapter>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!query.IncludeDown && nic.OperationalStatus != OperationalStatus.Up)
                continue;
            if (!Matches(nic, query))
                continue;
            adapters.Add(Map(nic));
        }

        return new WorkstationNetwork(
            host,
            domain,
            DateTimeOffset.UtcNow,
            adapters);
    }

    public static NetworkAdapter CaptureOne(string nameOrId)
    {
        var key = HelperGuard.NotBlank(nameOrId, nameof(nameOrId)).Trim();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (string.Equals(nic.Name, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nic.Id, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(nic.Description, key, StringComparison.OrdinalIgnoreCase))
            {
                return Map(nic);
            }
        }

        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(CaptureOne), $"adapter not found name={key}");
        throw new ArgumentException("Adapter was not found.", nameof(nameOrId));
    }

    private static bool Matches(NetworkInterface nic, NetworkAdapterQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Name)
            && !string.Equals(nic.Name, query.Name, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(nic.Description, query.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Id)
            && !string.Equals(nic.Id, query.Id, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static NetworkAdapter Map(NetworkInterface nic)
    {
        IPInterfaceProperties? props = null;
        try
        {
            props = nic.GetIPProperties();
        }
        catch (NetworkInformationException)
        {
        }

        var unicast = new List<UnicastAddress>();
        if (props is not null)
        {
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily is not AddressFamily.InterNetwork and not AddressFamily.InterNetworkV6)
                    continue;
                unicast.Add(MapUnicast(addr));
            }
        }

        var gateways = props is null
            ? Array.Empty<string>()
            : props.GatewayAddresses
                .Select(g => g.Address?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var dns = props is null
            ? Array.Empty<string>()
            : props.DnsAddresses
                .Select(a => a.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new NetworkAdapter(
            nic.Id,
            nic.Name,
            nic.Description,
            nic.NetworkInterfaceType,
            nic.OperationalStatus,
            FormatMac(nic),
            ReadSpeed(nic),
            nic.SupportsMulticast,
            unicast,
            gateways,
            dns,
            ReadDhcp(nic, props),
            ReadNetbios(nic));
    }

    private static UnicastAddress MapUnicast(UnicastIPAddressInformation addr)
    {
        var family = addr.Address.AddressFamily;
        var isDhcp = false;
        try
        {
            isDhcp = addr.PrefixOrigin == PrefixOrigin.Dhcp;
        }
        catch (PlatformNotSupportedException)
        {
        }

        if (family == AddressFamily.InterNetwork)
        {
            int? prefix = null;
            try
            {
                var p = addr.PrefixLength;
                if (p is >= 0 and <= 32)
                    prefix = p;
            }
            catch (PlatformNotSupportedException)
            {
            }

            IPAddress? mask = null;
            try
            {
                mask = addr.IPv4Mask;
            }
            catch (PlatformNotSupportedException)
            {
            }

            Ipv4Prefix.Agree(prefix, mask, out var agreedPrefix, out var dotted);
            return new UnicastAddress(family, addr.Address.ToString(), agreedPrefix, dotted, isDhcp);
        }

        var v6Prefix = 0;
        try
        {
            v6Prefix = addr.PrefixLength;
        }
        catch (PlatformNotSupportedException)
        {
        }

        return new UnicastAddress(family, addr.Address.ToString(), v6Prefix, null, isDhcp);
    }

    private static DhcpInfo ReadDhcp(NetworkInterface nic, IPInterfaceProperties? props)
    {
        bool? enabled = null;
        string? server = null;
        if (props is not null)
        {
            try
            {
                var v4 = props.GetIPv4Properties();
                enabled = v4.IsDhcpEnabled;
            }
            catch (NetworkInformationException)
            {
            }
            catch (PlatformNotSupportedException)
            {
            }

            try
            {
                server = props.DhcpServerAddresses
                    .Select(a => a.ToString())
                    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s) && s != "255.255.255.255");
            }
            catch (NetworkInformationException)
            {
            }
        }

        _ = nic;
        return new DhcpInfo(enabled, server, null, null);
    }

    private static NetbiosOverTcp ReadNetbios(NetworkInterface nic)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return NetbiosOverTcp.Unknown;

        try
        {
            var id = nic.Id.Trim('{', '}');
            var path = $@"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces\Tcpip_{{{id}}}";
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key?.GetValue("NetbiosOptions") is int option)
            {
                return option switch
                {
                    1 => NetbiosOverTcp.Enabled,
                    2 => NetbiosOverTcp.Disabled,
                    _ => NetbiosOverTcp.Unknown
                };
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
        }

        return NetbiosOverTcp.Unknown;
    }

    private static string? FormatMac(NetworkInterface nic)
    {
        try
        {
            var bytes = nic.GetPhysicalAddress().GetAddressBytes();
            if (bytes.Length == 0)
                return null;
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    private static long? ReadSpeed(NetworkInterface nic)
    {
        try
        {
            var speed = nic.Speed;
            return speed > 0 ? speed : null;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }
}
