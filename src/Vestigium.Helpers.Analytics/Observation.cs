namespace Vestigium.Helpers.Analytics;

/// <summary>
/// One measured value plus an optional UTC timestamp. Time is never required.
/// </summary>
/// <param name="Value">The measured quantity.</param>
/// <param name="At">When the observation was taken, or null if the host kept values only.</param>
public readonly record struct Observation(decimal Value, DateTimeOffset? At = null);

/// <summary>How <see cref="SeriesWindow"/> was chosen.</summary>
public enum SeriesWindowKind
{
    /// <summary>Values-only series, or no usable timestamps.</summary>
    None = 0,
    /// <summary>Derived from <see cref="NumericSeries.FirstAt"/> / <see cref="NumericSeries.LastAt"/>.</summary>
    InferredFromTimestamps = 1,
    /// <summary>Host passed an explicit window, or this series is the result of <see cref="NumericSeries.Slice"/>.</summary>
    CallerSupplied = 2
}

/// <summary>
/// Time window that selected this snapshot. <see cref="NumericSeries.Slice"/> uses
/// <c>[StartInclusive, EndExclusive)</c> on <c>At.UtcTicks</c>.
/// </summary>
/// <param name="StartInclusive">Window start, inclusive.</param>
/// <param name="EndExclusive">Window end, exclusive.</param>
/// <param name="Kind">Whether the window was inferred or supplied.</param>
public readonly record struct SeriesWindow(
    DateTimeOffset? StartInclusive,
    DateTimeOffset? EndExclusive,
    SeriesWindowKind Kind)
{
    /// <summary>No window (values-only series).</summary>
    public static SeriesWindow None { get; } = new(null, null, SeriesWindowKind.None);
}

/// <summary>One numeric point Charts can bind. This library does not draw.</summary>
/// <param name="X">Horizontal value (index, midpoint, or sample value).</param>
/// <param name="Y">Vertical value (sample, count, or cumulative fraction).</param>
public readonly record struct ChartPoint(double X, double Y);

/// <summary>
/// One bar on a Pareto chart: histogram bins sorted by count descending,
/// plus the running share of the sample. Rank is 1-based.
/// </summary>
/// <param name="Rank">1-based rank after sorting by count descending.</param>
/// <param name="Midpoint">Bin midpoint on the value axis.</param>
/// <param name="Count">Observations in this bin.</param>
/// <param name="CumulativeShare">Running share of n after this rank (ends at 1).</param>
public readonly record struct ParetoPoint(int Rank, double Midpoint, double Count, double CumulativeShare);

/// <summary>One timestamped observation for a time-series line. Clock order, not encounter order.</summary>
/// <param name="At">Observation time.</param>
/// <param name="Value">Measured quantity.</param>
public readonly record struct TimedValue(DateTimeOffset At, decimal Value);
