using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

public enum NetbiosOverTcp
{
    Unknown = 0,
    Disabled = 1,
    Enabled = 2
}

public sealed record UnicastAddress(
    AddressFamily Family,
    string Address,
    int PrefixLength,
    string? SubnetMask,
    bool IsDhcpAssigned);

public sealed record DhcpInfo(
    bool? IsEnabled,
    string? Server,
    DateTimeOffset? LeaseObtained,
    DateTimeOffset? LeaseExpires);

public sealed record NetworkAdapter(
    string Id,
    string Name,
    string Description,
    NetworkInterfaceType Type,
    OperationalStatus Status,
    string? MacAddress,
    long? SpeedBitsPerSecond,
    bool SupportsMulticast,
    IReadOnlyList<UnicastAddress> UnicastAddresses,
    IReadOnlyList<string> Gateways,
    IReadOnlyList<string> DnsServers,
    DhcpInfo Dhcp,
    NetbiosOverTcp NetbiosOverTcp);

public sealed record WorkstationNetwork(
    string HostName,
    string? DomainName,
    DateTimeOffset CapturedUtc,
    IReadOnlyList<NetworkAdapter> Adapters);

public sealed record NetworkAdapterQuery(
    string? Name = null,
    string? Id = null,
    bool IncludeDown = true);
