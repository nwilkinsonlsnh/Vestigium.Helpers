namespace Vestigium.Helpers.Network;

public enum IcmpEchoStatus
{
    Success,
    TimedOut,
    DestinationUnreachable,
    TtlExpired,
    ProtocolForbidden,
    Failed
}

public sealed class IcmpEchoOptions
{
    public const int DefaultCount = 4;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultContinuousDuration = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan MaxJobDuration = TimeSpan.FromHours(24);
    public static readonly TimeSpan ShortContinuousLimit = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MinContinuousInterval = TimeSpan.FromMilliseconds(200);
    public static readonly TimeSpan MinLongContinuousInterval = TimeSpan.FromSeconds(1);
    public const int DefaultBufferSize = 32;
    public const int MinTimeoutMs = 10;
    public const int MaxTimeoutMs = 60_000;
    public const int MinBufferSize = 1;
    public const int MaxBufferSize = 65_500;

    public int Count { get; set; } = DefaultCount;
    public TimeSpan Timeout { get; set; } = DefaultTimeout;
    public TimeSpan Interval { get; set; } = DefaultInterval;
    public int BufferSize { get; set; } = DefaultBufferSize;
    public int Ttl { get; set; } = 128;
    public bool DontFragment { get; set; }
    public TimeSpan? MaxDuration { get; set; }
    public bool AllowBurst { get; set; }
    public string? StatsPath { get; set; }
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
}

public sealed record IcmpEchoReply(
    int Sequence,
    IcmpEchoStatus Status,
    string? Address,
    long RoundtripTimeMs,
    int Ttl,
    bool PayloadRestricted,
    string? Detail);

public sealed record IcmpEchoResult(
    string JobId,
    string Target,
    string? ResolvedAddress,
    NetworkJobStatus Status,
    int Sent,
    int Received,
    int Lost,
    double LossPercent,
    long? MinMs,
    long? MaxMs,
    double? AverageMs,
    bool PayloadRestricted,
    IReadOnlyList<IcmpEchoReply> Replies);
