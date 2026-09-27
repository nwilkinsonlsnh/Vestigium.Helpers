using System.Net.NetworkInformation;

namespace Vestigium.Helpers.Network;

public sealed class TrafficWatchOptions
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(10);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(1);

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
}

public sealed record TrafficSample(
    DateTimeOffset At,
    OperationalStatus Status,
    long ReceiveBitsPerSecond,
    long SendBitsPerSecond,
    long ErrorsIn,
    long ErrorsOut,
    long DiscardsIn,
    long DiscardsOut);

public sealed record TrafficWatchResult(
    string JobId,
    string AdapterName,
    string AdapterId,
    TimeSpan Elapsed,
    OperationalStatus FirstStatus,
    OperationalStatus LastStatus,
    IReadOnlyList<TrafficSample> Samples);
