using System.Numerics;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Immutable snapshot of a finite numeric series plus quartile-band descriptors,
/// optional timestamps, and confidence intervals at a caller-chosen level.
/// </summary>
public sealed class NumericSeries
{
    public NumericSeries(IEnumerable<decimal> values, string? name = null)
        : this(
            NumberConvert.ToDecimalList(values ?? throw new ArgumentNullException(nameof(values))),
            times: [],
            name,
            SeriesWindow.None)
    {
    }

    private NumericSeries(
        List<decimal> values,
        IReadOnlyList<DateTimeOffset?> times,
        string? name,
        SeriesWindow window)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Values = values;
        Sorted = values.OrderBy(v => v).ToArray();
        Times = times;
        HasTimestamps = times.Count == values.Count && times.Any(t => t.HasValue);

        DateTimeOffset? first = null;
        DateTimeOffset? last = null;
        if (HasTimestamps)
        {
            foreach (var t in times)
            {
                if (t is not { } at)
                    continue;
                if (first is null || at < first)
                    first = at;
                if (last is null || at > last)
                    last = at;
            }
        }

        FirstAt = first;
        LastAt = last;
        Window = window.Kind != SeriesWindowKind.None
            ? window
            : HasTimestamps
                ? new SeriesWindow(first, last, SeriesWindowKind.InferredFromTimestamps)
                : SeriesWindow.None;

        Full = new SeriesSlice(SliceKind.Full, values);

        var q1 = Full.Q1!.Value;
        var median = Full.Median!.Value;
        var q3 = Full.Q3!.Value;

        Q1 = new SeriesSlice(SliceKind.Q1, values.Where(v => v <= q1).ToArray());
        Q2 = new SeriesSlice(SliceKind.Q2, values.Where(v => v > q1 && v <= median).ToArray());
        Q3 = new SeriesSlice(SliceKind.Q3, values.Where(v => v > median && v <= q3).ToArray());
        Q4 = new SeriesSlice(SliceKind.Q4, values.Where(v => v > q3).ToArray());
        Iqr = new SeriesSlice(SliceKind.Iqr, values.Where(v => v >= q1 && v <= q3).ToArray());
    }

    public static NumericSeries From<T>(IEnumerable<T> values, string? name = null)
        where T : INumber<T>
        => new(NumberConvert.ToDecimalList(values), [], name, SeriesWindow.None);

    public static NumericSeries FromDecimal(IEnumerable<decimal> values, string? name = null)
        => new(values, name);

    public static NumericSeries FromObservations(IEnumerable<Observation> observations, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var (values, times) = Unpack(observations);
        return new NumericSeries(values, times, name, SeriesWindow.None);
    }

    public static NumericSeries FromObservations(
        IEnumerable<Observation> observations,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (startInclusive >= endExclusive)
            throw new ArgumentException("Window start must be earlier than end.", nameof(startInclusive));

        var filtered = observations.Where(o =>
            o.At is { } at &&
            at.UtcTicks >= startInclusive.UtcTicks &&
            at.UtcTicks < endExclusive.UtcTicks);

        var (values, times) = Unpack(filtered);
        return new NumericSeries(
            values,
            times,
            name,
            new SeriesWindow(startInclusive, endExclusive, SeriesWindowKind.CallerSupplied));
    }

    public string? Name { get; }
    public int Count => Values.Count;
    public IReadOnlyList<decimal> Values { get; }
    public IReadOnlyList<decimal> Sorted { get; }
    public IReadOnlyList<DateTimeOffset?> Times { get; }
    public bool HasTimestamps { get; }
    public DateTimeOffset? FirstAt { get; }
    public DateTimeOffset? LastAt { get; }
    public SeriesWindow Window { get; }

    public SeriesSlice Full { get; }
    public SeriesSlice Q1 { get; }
    public SeriesSlice Q2 { get; }
    public SeriesSlice Q3 { get; }
    public SeriesSlice Q4 { get; }
    public SeriesSlice Iqr { get; }

    public IReadOnlyList<SeriesSlice> Bands => [Full, Q1, Q2, Q3, Q4, Iqr];

    public NumericSeries Slice(DateTimeOffset startInclusive, DateTimeOffset endExclusive, string? name = null)
    {
        if (!HasTimestamps)
            throw new InvalidOperationException("Slice requires at least one timestamped observation.");
        if (startInclusive >= endExclusive)
            throw new ArgumentException("Window start must be earlier than end.", nameof(startInclusive));

        var values = new List<decimal>();
        var times = new List<DateTimeOffset?>();
        for (var i = 0; i < Values.Count; i++)
        {
            if (Times[i] is not { } at)
                continue;
            if (at.UtcTicks >= startInclusive.UtcTicks && at.UtcTicks < endExclusive.UtcTicks)
            {
                values.Add(Values[i]);
                times.Add(at);
            }
        }

        if (values.Count == 0)
            throw new ArgumentException("Slice produced an empty series.");

        return new NumericSeries(
            values,
            times,
            name ?? Name,
            new SeriesWindow(startInclusive, endExclusive, SeriesWindowKind.CallerSupplied));
    }

    public ConfidenceReport Confidence(double level = ConfidenceLevel.DefaultValue)
        => Full.Confidence(level);

    public ConfidenceReport Confidence(double level, int populationSize)
        => Full.Confidence(level, populationSize);

    /// <summary>
    /// Two-sided p-value of H0: mean = <paramref name="hypothesizedMean"/>.
    /// </summary>
    public double? MeanPValue(double hypothesizedMean)
        => ConfidenceReport.TwoSidedMeanPValue(Full.Statistics, hypothesizedMean);

    /// <summary>
    /// Just-covering two-sided level for the mean: the smallest γ whose interval
    /// contains <paramref name="hypothesizedMean"/>. Equal to 1 − p. This is the
    /// dual of a t-test, not an estimated "confidence of the sample."
    /// </summary>
    public double? MeanConfidenceLevelContaining(double hypothesizedMean)
    {
        var p = MeanPValue(hypothesizedMean);
        return p is null ? null : 1d - p.Value;
    }

    public int? SampleSizeForMeanMargin(double targetMargin, double level = ConfidenceLevel.DefaultValue)
        => ConfidenceReport.PlanSampleSize(Full.Statistics, targetMargin, ConfidenceLevel.Of(level));

    public ConfidenceInterval ProportionAbove(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAbove(threshold, level);

    public ConfidenceInterval ProportionAtLeast(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAtLeast(threshold, level);

    public IReadOnlyList<ChartPoint> SampleOrderPoints()
    {
        var points = new ChartPoint[Values.Count];
        for (var i = 0; i < Values.Count; i++)
            points[i] = new ChartPoint(i, (double)Values[i]);
        return points;
    }

    public IReadOnlyList<ChartPoint> SortedPoints()
    {
        var points = new ChartPoint[Sorted.Count];
        for (var i = 0; i < Sorted.Count; i++)
            points[i] = new ChartPoint(i, (double)Sorted[i]);
        return points;
    }

    public IReadOnlyList<ChartPoint> EcdfPoints()
    {
        var n = Sorted.Count;
        var points = new ChartPoint[n];
        for (var i = 0; i < n; i++)
            points[i] = new ChartPoint((double)Sorted[i], (i + 1d) / n);
        return points;
    }

    public IReadOnlyList<ChartPoint> HistogramPoints()
        => HistogramAsPoints(relative: false);

    public IReadOnlyList<ChartPoint> HistogramRelativePoints()
        => HistogramAsPoints(relative: true);

    public IReadOnlyList<ChartPoint> HistogramTrendPoints()
        => Full.Frequency.HistogramTrend();

    public IReadOnlyList<ParetoPoint> ParetoPoints()
        => Full.Frequency.Pareto();

    public IReadOnlyList<TimedValue> TimeSeriesPoints()
    {
        if (!HasTimestamps)
            return [];

        var list = new List<TimedValue>();
        for (var i = 0; i < Values.Count; i++)
        {
            if (Times[i] is { } at)
                list.Add(new TimedValue(at, Values[i]));
        }

        return list;
    }

    private IReadOnlyList<ChartPoint> HistogramAsPoints(bool relative)
    {
        var bins = Full.Frequency.Histogram;
        var points = new ChartPoint[bins.Count];
        for (var i = 0; i < bins.Count; i++)
        {
            var bin = bins[i];
            var mid = (double)((bin.LowerInclusive + bin.UpperInclusive) / 2m);
            points[i] = new ChartPoint(mid, relative ? bin.RelativeFrequency : bin.Count);
        }

        return points;
    }

    private static (List<decimal> Values, List<DateTimeOffset?> Times) Unpack(IEnumerable<Observation> observations)
    {
        var values = new List<decimal>();
        var times = new List<DateTimeOffset?>();
        foreach (var observation in observations)
        {
            values.Add(observation.Value);
            times.Add(observation.At);
        }

        if (values.Count == 0)
            throw new ArgumentException("A numeric series must contain at least one value.", nameof(observations));

        return (values, times);
    }
}
