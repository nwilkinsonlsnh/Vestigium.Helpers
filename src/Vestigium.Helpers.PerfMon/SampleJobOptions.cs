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
            throw new ArgumentOutOfRangeException(nameof(Interval), Interval, "Interval must be positive.");

        if (Interval < BurstFloor)
            throw new ArgumentOutOfRangeException(nameof(Interval), Interval, "Burst floor is 50 ms.");

        if (Interval < BurstThreshold && !AllowBurst)
            throw new ArgumentOutOfRangeException(nameof(Interval), Interval, "Intervals under 200 ms require AllowBurst.");

        if (Duration is { } duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Duration), duration, "Duration must be positive.");
            if (duration > MaxDuration)
                throw new ArgumentOutOfRangeException(nameof(Duration), duration, "Duration cap is 24 hours.");
        }

        if (Count is < 0)
            throw new ArgumentOutOfRangeException(nameof(Count), Count, "Count cannot be negative.");

        if (InstanceCap < 0)
            throw new ArgumentOutOfRangeException(nameof(InstanceCap), InstanceCap, "Instance cap cannot be negative.");

        ArgumentNullException.ThrowIfNull(Clock);
    }

    internal void RejectIfUnbounded(CancellationToken token)
    {
        if (HasCountLimit || HasDurationLimit || token.CanBeCanceled)
            return;

        throw new ArgumentException("Job must have a count, a duration, or a cancellation token.");
    }
}
