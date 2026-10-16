namespace Vestigium.Helpers.SystemInfo;

/// <summary>Network inventory. Raw values. No display strings. Host formats.</summary>
public readonly record struct NetworkInventory(
    Fact<IReadOnlyList<NetworkAdapter>> Adapters,
    Fact<IReadOnlyList<NetworkRoute>> Routes,
    Fact<IReadOnlyList<ArpEntry>> Arp,
    Fact<IReadOnlyList<Listener>> Listeners,
    Fact<IReadOnlyList<TcpConnection>> Tcp);

/// <summary>One adapter with IP and DNS. Speed is bits per second. Addresses are raw strings.</summary>
public readonly record struct NetworkAdapter(
    string Name,
    string Description,
    string Status,
    string Mac,
    IReadOnlyList<string> Addresses,
    IReadOnlyList<string> Gateways,
    IReadOnlyList<string> DnsServers,
    string DnsSuffix,
    long SpeedBitsPerSecond,
    string InterfaceType,
    bool DhcpEnabled);

/// <summary>Win32_IP4RouteTable. Metric is Metric1.</summary>
public readonly record struct NetworkRoute(
    string Destination,
    string Mask,
    string NextHop,
    uint InterfaceIndex,
    uint Metric);

/// <summary>MSFT_NetNeighbor. State is the raw CIM value.</summary>
public readonly record struct ArpEntry(
    string Address,
    string Mac,
    uint State,
    uint InterfaceIndex);

/// <summary>TCP or UDP listener from IPGlobalProperties.</summary>
public readonly record struct Listener(
    string Protocol,
    string LocalAddress,
    int Port);

/// <summary>Active TCP connection. State is the raw TcpState value.</summary>
public readonly record struct TcpConnection(
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort,
    uint State);
