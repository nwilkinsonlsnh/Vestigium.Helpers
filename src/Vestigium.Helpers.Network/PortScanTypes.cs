namespace Vestigium.Helpers.Network;

public enum PortScanStatus
{
    Open,
    Closed,
    TimedOut,
    Error
}

public enum PortScanTransport
{
    Tcp,
    Udp
}

public sealed class PortScanOptions
{
    public const int DefaultParallelism = 16;
    public const int MaxParallelism = 64;
    public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan DefaultOverallTimeout = TimeSpan.FromMinutes(2);

    public int Parallelism { get; set; } = DefaultParallelism;
    public TimeSpan ConnectTimeout { get; set; } = DefaultConnectTimeout;
    public TimeSpan OverallTimeout { get; set; } = DefaultOverallTimeout;
    public TimeSpan PerHostDelay { get; set; }
    public bool IncludeTcp { get; set; } = true;
    public bool IncludeUdp { get; set; }
    public bool Infer { get; set; }
    public bool Discover { get; set; }
    public int? DiscoverPort { get; set; }
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
}

public sealed record PortScanHit(
    string Host,
    int Port,
    PortScanTransport Transport,
    PortScanStatus Status,
    string? Address,
    long ElapsedMs,
    string? Detail,
    int? Mss = null,
    string? Banner = null,
    string? Service = null);

public sealed record PortScanOpenPort(
    int Port,
    PortScanTransport Transport,
    string? Service,
    int? Mss,
    string? Banner,
    long ElapsedMs);

public sealed record PortScanHostRow(
    string Host,
    IReadOnlyList<PortScanOpenPort> OpenTcp,
    IReadOnlyList<PortScanOpenPort> OpenUdp,
    InferenceResult? Inference);

public sealed record PortScanResult(
    string JobId,
    NetworkJobStatus Status,
    int HostCount,
    int PortCount,
    int OpenCount,
    IReadOnlyList<PortScanHit> Hits,
    IReadOnlyList<PortScanHostRow> Hosts,
    IReadOnlyList<string> Skipped);
