namespace Vestigium.Helpers.Analytics;

/// <summary>
/// One measured value plus an optional UTC timestamp. Time is never required.
/// </summary>
public readonly record struct Observation(decimal Value, DateTimeOffset? At = null);

public enum SeriesWindowKind
{
    None = 0,
    InferredFromTimestamps = 1,
    CallerSupplied = 2
}

/// <summary>
/// Time window that selected this snapshot. Slice uses [StartInclusive, EndExclusive).
/// </summary>
public readonly record struct SeriesWindow(
    DateTimeOffset? StartInclusive,
    DateTimeOffset? EndExclusive,
    SeriesWindowKind Kind)
{
    public static SeriesWindow None { get; } = new(null, null, SeriesWindowKind.None);
}

public readonly record struct ChartPoint(double X, double Y);

/// <summary>
/// One bar on a Pareto chart: histogram bins sorted by count descending,
/// plus the running share of the sample. Rank is 1-based.
/// </summary>
public readonly record struct ParetoPoint(int Rank, double Midpoint, double Count, double CumulativeShare);


public readonly record struct TimedValue(DateTimeOffset At, decimal Value);

