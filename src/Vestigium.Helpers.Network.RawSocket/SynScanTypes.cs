namespace Vestigium.Helpers.Network.RawSocket;

public enum SynScanStatus
{
    Success,
    Denied,
    TimedOut,
    Error
}

public enum SynPortStatus
{
    Open,
    Closed,
    Filtered
}

public sealed class SynScanOptions
{
    public const int MaxHosts = 64;
    public const int DefaultParallelism = 8;
    public const int MaxParallelism = 32;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultOverallTimeout = TimeSpan.FromMinutes(2);

    public TimeSpan Timeout { get; set; } = DefaultTimeout;
    public TimeSpan OverallTimeout { get; set; } = DefaultOverallTimeout;
    public int Parallelism { get; set; } = DefaultParallelism;
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
}

public sealed record SynScanHit(
    string Host,
    int Port,
    SynPortStatus Status,
    long ElapsedMs,
    string? Detail);

public sealed record SynScanResult(
    string JobId,
    SynScanStatus Status,
    string? Detail,
    int HostCount,
    int PortCount,
    int OpenCount,
    IReadOnlyList<SynScanHit> Hits);
