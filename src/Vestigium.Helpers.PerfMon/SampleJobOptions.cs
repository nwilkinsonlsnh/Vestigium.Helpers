namespace Vestigium.Helpers.PerfMon;

/// <summary>Bounds for one <see cref="SampleJob"/>. Invalid combinations throw before the first read.</summary>
public sealed class SampleJobOptions
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan BurstThreshold = TimeSpan.FromMilliseconds(200);
    public static readonly TimeSpan BurstFloor = TimeSpan.FromMilliseconds(50);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);
    public const int DefaultInstanceCap = 256;

    public TimeSpan Interval { get; init; } = DefaultInterval;
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public int InstanceCap { get; init; } = DefaultInstanceCap;
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }

    internal bool HasCountLimit => Count is > 0;
    internal bool HasDurationLimit => Duration is { } d && d > TimeSpan.Zero;

    internal static SampleJobOptions Normalize(SampleJobOptions? options)
    {
        var value = options ?? new SampleJobOptions();
        value.RejectIfInvalid();
        return value;
    }

    internal void RejectIfInvalid()
    {
        if (Interval <= TimeSpan.Zero)
            Reject("Interval must be positive.", nameof(Interval), Interval);

        if (Interval < BurstFloor)
            Reject("Burst floor is 50 ms.", nameof(Interval), Interval);

        if (Interval < BurstThreshold && !AllowBurst)
            Reject("Intervals under 200 ms require AllowBurst.", nameof(Interval), Interval);

        if (Duration is { } duration)
        {
            if (duration <= TimeSpan.Zero)
                Reject("Duration must be positive.", nameof(Duration), duration);
            if (duration > MaxDuration)
                Reject("Duration cap is 24 hours.", nameof(Duration), duration);
        }

        if (Count is < 0)
            Reject("Count cannot be negative.", nameof(Count), Count);

        if (InstanceCap < 0)
            Reject("Instance cap cannot be negative.", nameof(InstanceCap), InstanceCap);

        ArgumentNullException.ThrowIfNull(Clock);
    }

    internal void RejectIfUnbounded(CancellationToken token)
    {
        if (HasCountLimit || HasDurationLimit || token.CanBeCanceled)
            return;

        PerfMonLog.Error(
            PerfMonEvents.JobRejected,
            Vestigium.Logging.VestigiumStatus.Failed,
            PerfMonCatalog.Subcategories.Job,
            "unbounded job");
        throw new ArgumentException("Job must have a count, a duration, or a cancellation token.");
    }

    private static void Reject(string message, string paramName, object? actual)
    {
        PerfMonLog.Error(
            PerfMonEvents.JobRejected,
            Vestigium.Logging.VestigiumStatus.Failed,
            PerfMonCatalog.Subcategories.Job,
            "options rejected");
        throw new ArgumentOutOfRangeException(paramName, actual, message);
    }
}
