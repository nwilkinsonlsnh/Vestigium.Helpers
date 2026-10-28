using System.Net.NetworkInformation;

namespace Vestigium.Helpers.Network;

public sealed class AdapterWatchOptions
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(10);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(1);

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Interval { get; set; }
}

public sealed record AdapterWatchSample(
    DateTimeOffset At,
    OperationalStatus Status,
    long SpeedBitsPerSecond);

public sealed record AdapterWatchResult(
    string JobId,
    string AdapterName,
    string AdapterId,
    TimeSpan Elapsed,
    OperationalStatus FirstStatus,
    OperationalStatus LastStatus,
    IReadOnlyList<AdapterWatchSample> Samples);
