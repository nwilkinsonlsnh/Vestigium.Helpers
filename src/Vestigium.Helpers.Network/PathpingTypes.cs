namespace Vestigium.Helpers.Network;

public sealed class PathpingOptions
{
    public const int DefaultMaxHops = IcmpTraceOptions.DefaultMaxHops;
    public const int DefaultProbesPerHop = IcmpTraceOptions.DefaultProbesPerHop;
    public const int DefaultSamplesPerHop = 10;
    public const int MinSamplesPerHop = 1;
    public const int MaxSamplesPerHop = 100;

    public int MaxHops { get; set; } = DefaultMaxHops;
    public int ProbesPerHop { get; set; } = DefaultProbesPerHop;
    public int SamplesPerHop { get; set; } = DefaultSamplesPerHop;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);
    public TimeSpan SampleInterval { get; set; } = TimeSpan.FromMilliseconds(200);
    public int BufferSize { get; set; } = IcmpEchoOptions.DefaultBufferSize;
    public bool PreferUdp { get; set; }
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
}

public sealed record PathpingHop(
    int Ttl,
    string? Address,
    int Samples,
    int Received,
    int Lost,
    double HopLossPercent,
    double LinkLossPercent,
    long? MinMs,
    long? MaxMs,
    double? AverageMs);

public sealed record PathpingResult(
    string JobId,
    string Target,
    string? ResolvedAddress,
    NetworkJobStatus Status,
    bool Reached,
    ProbeProtocol ProbeProtocol,
    IcmpTraceResult Walk,
    IReadOnlyList<PathpingHop> Hops);
