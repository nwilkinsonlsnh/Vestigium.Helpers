namespace Vestigium.Helpers.Network;

public enum ProbeProtocol
{
    Icmp,
    Udp
}

public sealed class IcmpTraceOptions
{
    public const int DefaultMaxHops = 30;
    public const int DefaultProbesPerHop = 3;

    public int MaxHops { get; set; } = DefaultMaxHops;
    public int ProbesPerHop { get; set; } = DefaultProbesPerHop;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);
    public int BufferSize { get; set; } = IcmpEchoOptions.DefaultBufferSize;
    public bool PreferUdp { get; set; }
}

public sealed record IcmpTraceProbe(
    int Ttl,
    int Probe,
    ProbeProtocol Protocol,
    IcmpEchoStatus Status,
    string? Address,
    long RoundtripTimeMs,
    string? Detail);

public sealed record IcmpTraceHop(
    int Ttl,
    string? Address,
    IReadOnlyList<IcmpTraceProbe> Probes);

public sealed record IcmpTraceResult(
    string JobId,
    string Target,
    string? ResolvedAddress,
    NetworkJobStatus Status,
    bool Reached,
    ProbeProtocol ProbeProtocol,
    int HopCount,
    IReadOnlyList<IcmpTraceHop> Hops);
