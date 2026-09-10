using System.Net.Sockets;
using System.Numerics;

namespace Vestigium.Helpers.Network;

public enum TraditionalClass
{
    None = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5
}

[Flags]
public enum AddressKind
{
    None = 0,
    Unspecified = 1 << 0,
    Loopback = 1 << 1,
    Rfc1918 = 1 << 2,
    LinkLocal = 1 << 3,
    Cgnat = 1 << 4,
    CarrierGradeNat = Cgnat,
    Documentation = 1 << 5,
    Benchmark = 1 << 6,
    Multicast = 1 << 7,
    Broadcast = 1 << 8,
    Unicast = 1 << 9,
    UniqueLocal = 1 << 10,
    GlobalUnicast = 1 << 11,
    Ipv4Mapped = 1 << 12
}

public sealed record AddressClass(
    AddressFamily Family,
    string Address,
    TraditionalClass TraditionalClass,
    AddressKind Kind);

public sealed record PrefixBlock(
    AddressFamily Family,
    string? Address,
    string Network,
    int PrefixLength,
    string? SubnetMask,
    string? WildcardMask,
    string? Broadcast,
    string? FirstUsable,
    string? LastUsable,
    BigInteger TotalAddresses,
    BigInteger UsableHosts,
    bool IsHostRoute,
    bool IsPointToPoint,
    string BinaryMask,
    TraditionalClass TraditionalClass,
    AddressKind Kind,
    string? PtrHint);

public sealed record PrefixPlan(
    PrefixBlock Parent,
    string Rule,
    int ChildPrefix,
    BigInteger TotalNetworks,
    IReadOnlyList<PrefixBlock> Networks,
    IReadOnlyList<PrefixBlock> Unused);

public sealed class SubnetQuery
{
    public int MaxList { get; set; } = 1024;
    public bool CountNetworkAndBroadcast { get; set; }
    public bool PackLargestFirst { get; set; } = true;
}
