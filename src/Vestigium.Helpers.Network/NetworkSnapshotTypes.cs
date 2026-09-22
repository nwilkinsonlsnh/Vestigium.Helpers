namespace Vestigium.Helpers.Network;

public sealed record NetworkSnapshot(
    WorkstationNetwork Workstation,
    IReadOnlyList<NetworkRoute> Routes,
    IReadOnlyList<NetworkConnection> Connections,
    IReadOnlyList<NetworkNeighbor> Neighbors,
    NetworkStackStatistics Statistics,
    DateTimeOffset CapturedUtc);

public sealed class NetworkRouteChange
{
    public string Destination { get; set; } = "";
    public int PrefixLength { get; set; }
    public string Gateway { get; set; } = "";
    public int? InterfaceIndex { get; set; }
    public int Metric { get; set; } = 1;
    public bool Persistent { get; set; }
}

public sealed record NetBiosAdapterStatus(
    string AdapterName,
    NetbiosOverTcp OverTcp,
    string? Description);

public sealed record NetBiosInfo(
    string HostName,
    string? DomainName,
    IReadOnlyList<NetBiosAdapterStatus> Adapters);
