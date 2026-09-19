using System.Numerics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Immutable snapshot of a finite numeric series plus quartile-band descriptors,
/// optional timestamps, and confidence intervals at a caller-chosen level.
/// </summary>
public sealed class NumericSeries
{
    /// <summary>
    /// Builds a series from decimal values. Encounter order is kept.
    /// </summary>
    /// <param name="values">Non-empty finite values.</param>
    /// <param name="name">Optional label written to the log.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentException">The sequence is empty or contains a non-finite value.</exception>
    public NumericSeries(IEnumerable<decimal> values, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter series",
            properties: AnalyticsLog.Props(("name", name), ("via", "ctor")));
        try
        {
            Bind(
                NumberConvert.ToDecimalList(values),
                times: [],
                name,
                SeriesWindow.None);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.SeriesThrown, AnalyticsCatalog.Subcategories.Series, ex);
            throw;
        }
    }

    private NumericSeries(
        List<decimal> values,
        IReadOnlyList<DateTimeOffset?> times,
        string? name,
        SeriesWindow window)
    {
        Bind(values, times, name, window);
    }

    private void Bind(
        List<decimal> values,
        IReadOnlyList<DateTimeOffset?> times,
        string? name,
        SeriesWindow window)
    {
        SeriesId = AnalyticsLog.NewId();
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Values = NumberConvert.Freeze(values);
        Sorted = NumberConvert.Freeze(values.OrderBy(v => v));
        Times = NumberConvert.Freeze(times);
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

        Q1 = new SeriesSlice(SliceKind.Q1, [.. values.Where(v => v <= q1)]);
        Q2 = new SeriesSlice(SliceKind.Q2, [.. values.Where(v => v > q1 && v <= median)]);
        Q3 = new SeriesSlice(SliceKind.Q3, [.. values.Where(v => v > median && v <= q3)]);
        Q4 = new SeriesSlice(SliceKind.Q4, [.. values.Where(v => v > q3)]);
        Iqr = new SeriesSlice(SliceKind.Iqr, [.. values.Where(v => v >= q1 && v <= q3)]);

        AnalyticsLog.Information(
            AnalyticsEvents.SeriesConstructed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Series,
            "constructed",
            SeriesId,
            AnalyticsLog.Props(
                ("n", values.Count.ToString()),
                ("name", Name),
                ("window", Window.Kind.ToString())));
    }

    /// <summary>
    /// Builds a series from any <see cref="INumber{T}"/> sequence.
    /// </summary>
    /// <typeparam name="T">A numeric type convertible to finite decimal.</typeparam>
    /// <param name="values">Non-empty finite values.</param>
    /// <param name="name">Optional label written to the log.</param>
    public static NumericSeries From<T>(IEnumerable<T> values, string? name = null)
        where T : INumber<T>
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter series",
            properties: AnalyticsLog.Props(("name", name), ("via", "From")));
        try
        {
            return new NumericSeries(NumberConvert.ToDecimalList(values), [], name, SeriesWindow.None);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.SeriesThrown, AnalyticsCatalog.Subcategories.Series, ex);
            throw;
        }
    }

    /// <summary>Builds a series from decimals. Same as the public constructor.</summary>
    public static NumericSeries FromDecimal(IEnumerable<decimal> values, string? name = null)
        => new(values, name);

    /// <summary>
    /// Builds a series from observations. Null timestamps are kept as gaps on <see cref="Times"/>.
    /// </summary>
    public static NumericSeries FromObservations(IEnumerable<Observation> observations, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter series",
            properties: AnalyticsLog.Props(("name", name), ("via", "FromObservations")));
        try
        {
            var (values, times) = Unpack(observations);
            return new NumericSeries(values, times, name, SeriesWindow.None);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.SeriesThrown, AnalyticsCatalog.Subcategories.Series, ex);
            throw;
        }
    }

    /// <summary>
    /// Builds a series from observations whose <see cref="Observation.At"/> falls in
    /// <c>[startInclusive, endExclusive)</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The window is empty or inverted, or no observation falls inside it.</exception>
    public static NumericSeries FromObservations(
        IEnumerable<Observation> observations,
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        RequireWindow(startInclusive, endExclusive);
        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter series",
            properties: AnalyticsLog.Props(("name", name), ("via", "FromObservationsWindow")));
        try
        {
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
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.SeriesThrown, AnalyticsCatalog.Subcategories.Series, ex);
            throw;
        }
    }

    /// <summary>Short correlation id written on construct and later boundary calls.</summary>
    public string SeriesId { get; private set; } = "";

    /// <summary>Optional caller label. Null when none was given.</summary>
    public string? Name { get; private set; }

    /// <summary>Number of values in encounter order.</summary>
    public int Count => Values.Count;

    /// <summary>Encounter-order values. Frozen copy; do not mutate.</summary>
    public IReadOnlyList<decimal> Values { get; private set; } = [];

    /// <summary>Values sorted ascending. Frozen copy; do not mutate.</summary>
    public IReadOnlyList<decimal> Sorted { get; private set; } = [];

    /// <summary>
    /// Per-observation timestamps aligned with <see cref="Values"/>, or an empty list
    /// when the series was built from values only. Frozen copy.
    /// </summary>
    public IReadOnlyList<DateTimeOffset?> Times { get; private set; } = [];

    /// <summary>True when at least one timestamp is present and the list lines up with <see cref="Values"/>.</summary>
    public bool HasTimestamps { get; private set; }

    /// <summary>Earliest non-null timestamp, if any.</summary>
    public DateTimeOffset? FirstAt { get; private set; }

    /// <summary>Latest non-null timestamp, if any.</summary>
    public DateTimeOffset? LastAt { get; private set; }

    /// <summary>Caller-supplied or inferred UTC window. <see cref="SeriesWindow.None"/> when there is no time.</summary>
    public SeriesWindow Window { get; private set; }

    /// <summary>The whole snapshot. Quartile fences used by Q1–Q4 and IQR come from here.</summary>
    public SeriesSlice Full { get; private set; } = null!;

    /// <summary>Values ≤ the full-series Q1.</summary>
    public SeriesSlice Q1 { get; private set; } = null!;

    /// <summary>Values after Q1 through the median.</summary>
    public SeriesSlice Q2 { get; private set; } = null!;

    /// <summary>Values after the median through Q3.</summary>
    public SeriesSlice Q3 { get; private set; } = null!;

    /// <summary>Values above the full-series Q3 (right tail).</summary>
    public SeriesSlice Q4 { get; private set; } = null!;

    /// <summary>Values from Q1 through Q3 inclusive.</summary>
    public SeriesSlice Iqr { get; private set; } = null!;

    /// <summary>Full, Q1–Q4, and IQR in that order. Empty bands are present and have count 0.</summary>
    public IReadOnlyList<SeriesSlice> Bands => [Full, Q1, Q2, Q3, Q4, Iqr];

    /// <summary>
    /// New snapshot of observations whose timestamp falls in
    /// <c>[startInclusive, endExclusive)</c>. The result is Full of that window.
    /// </summary>
    /// <exception cref="InvalidOperationException">This series has no timestamps.</exception>
    /// <exception cref="ArgumentException">The window is inverted or the slice is empty.</exception>
    public NumericSeries Slice(DateTimeOffset startInclusive, DateTimeOffset endExclusive, string? name = null)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter series",
            SeriesId,
            AnalyticsLog.Props(("via", "Slice")));
        try
        {
            if (!HasTimestamps)
                throw new InvalidOperationException("Slice requires at least one timestamped observation.");
            RequireWindow(startInclusive, endExclusive);

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

            switch (values.Count)
            {
                case 0:
                    AnalyticsLog.Error(
                        AnalyticsEvents.SeriesRejectedEmptySlice,
                        VestigiumStatus.Failed,
                        AnalyticsCatalog.Subcategories.Series,
                        "rejected empty slice",
                        correlationId: SeriesId);
                    throw new ArgumentException("Slice produced an empty series.");
                default:
                    return new NumericSeries(
                        values,
                        times,
                        name ?? Name,
                        new SeriesWindow(startInclusive, endExclusive, SeriesWindowKind.CallerSupplied));
            }
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.SeriesThrown, AnalyticsCatalog.Subcategories.Series, ex, SeriesId);
            throw;
        }
    }

    /// <summary>
    /// Two-sided intervals for mean, median, variance, and standard deviation on <see cref="Full"/>.
    /// </summary>
    /// <param name="level">Coverage target γ in (0, 1). Default 0.95. Not a percentile.</param>
    public ConfidenceReport Confidence(double level = ConfidenceLevel.DefaultValue)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.ConfidenceEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Confidence,
            "enter confidence",
            SeriesId,
            AnalyticsLog.Props(("gamma", level.ToString("G6")), ("n", Count.ToString())));
        try
        {
            var report = Full.Confidence(level);
            AnalyticsLog.Information(
                AnalyticsEvents.ConfidenceComputed,
                VestigiumStatus.Success,
                AnalyticsCatalog.Subcategories.Confidence,
                "confidence computed",
                SeriesId,
                AnalyticsLog.Props(
                    ("gamma", level.ToString("G6")),
                    ("meanLower", Fmt(report.Mean.Lower)),
                    ("meanUpper", Fmt(report.Mean.Upper))));
            return report;
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.ConfidenceThrown, AnalyticsCatalog.Subcategories.Confidence, ex, SeriesId);
            throw;
        }
    }

    /// <summary>
    /// Same as <see cref="Confidence(double)"/> with a finite-population correction on the mean.
    /// </summary>
    /// <param name="level">Coverage target γ in (0, 1).</param>
    /// <param name="populationSize">Known N. Must be ≥ n.</param>
    public ConfidenceReport Confidence(double level, int populationSize)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.ConfidenceEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Confidence,
            "enter confidence",
            SeriesId,
            AnalyticsLog.Props(
                ("gamma", level.ToString("G6")),
                ("N", populationSize.ToString()),
                ("n", Count.ToString())));
        try
        {
            var report = Full.Confidence(level, populationSize);
            AnalyticsLog.Information(
                AnalyticsEvents.ConfidenceComputed,
                VestigiumStatus.Success,
                AnalyticsCatalog.Subcategories.Confidence,
                "confidence computed",
                SeriesId,
                AnalyticsLog.Props(
                    ("gamma", level.ToString("G6")),
                    ("N", populationSize.ToString()),
                    ("meanLower", Fmt(report.Mean.Lower)),
                    ("meanUpper", Fmt(report.Mean.Upper))));
            return report;
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.ConfidenceThrown, AnalyticsCatalog.Subcategories.Confidence, ex, SeriesId);
            throw;
        }
    }

    /// <summary>
    /// Two-sided Student-t p-value that the mean equals <paramref name="hypothesizedMean"/>.
    /// Null when the test is undefined (n &lt; 2 or s = 0).
    /// </summary>
    public double? MeanPValue(double hypothesizedMean)
        => ConfidenceReport.TwoSidedMeanPValue(Full.Statistics, hypothesizedMean);

    /// <summary>
    /// Smallest γ whose two-sided mean interval still contains
    /// <paramref name="hypothesizedMean"/>. Equals 1 − p. Not “sample confidence.”
    /// </summary>
    public double? MeanConfidenceLevelContaining(double hypothesizedMean)
    {
        var p = MeanPValue(hypothesizedMean);
        return p is null ? null : 1d - p.Value;
    }

    /// <summary>
    /// Planned n so the mean interval at <paramref name="level"/> has half-width
    /// ≤ <paramref name="targetMargin"/>, using this snapshot’s s as the planning σ.
    /// Null when s is not positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetMargin"/> is not positive.</exception>
    public int? SampleSizeForMeanMargin(double targetMargin, double level = ConfidenceLevel.DefaultValue)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.ConfidenceEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Confidence,
            "enter confidence",
            SeriesId,
            AnalyticsLog.Props(("margin", targetMargin.ToString("G6")), ("gamma", level.ToString("G6"))));
        try
        {
            return ConfidenceReport.PlanSampleSize(Full.Statistics, targetMargin, ConfidenceLevel.Of(level));
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.ConfidenceThrown, AnalyticsCatalog.Subcategories.Confidence, ex, SeriesId);
            throw;
        }
    }

    /// <summary>Wilson interval for the sample fraction strictly above <paramref name="threshold"/>.</summary>
    public ConfidenceInterval ProportionAbove(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAbove(threshold, level);

    /// <summary>Wilson interval for the sample fraction at or above <paramref name="threshold"/>.</summary>
    public ConfidenceInterval ProportionAtLeast(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAtLeast(threshold, level);

    /// <summary>
    /// Process fences on <see cref="Full"/>. Throws when the method cannot run.
    /// Prefer <see cref="TryControlLimits"/> when walking <see cref="Bands"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">n &lt; 2, missing mean, s not positive, or MR̄ = 0.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> is <see cref="ControlLimitMethod.CallerSupplied"/>.</exception>
    public ControlLimits ControlLimits(
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter limits",
            SeriesId,
            AnalyticsLog.Props(("method", method.ToString()), ("k", k.ToString("G6")), ("n", Count.ToString())));
        try
        {
            return Full.ControlLimits(method, k, floor);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex, SeriesId);
            throw;
        }
    }

    /// <summary>
    /// Same math as <see cref="ControlLimits"/> without throwing for insufficient data.
    /// Returns false and <paramref name="limits"/> = null when the method cannot run.
    /// Still throws on caller error (k ≤ 0, <see cref="ControlLimitMethod.CallerSupplied"/>).
    /// </summary>
    public bool TryControlLimits(
        out ControlLimits? limits,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter limits",
            SeriesId,
            AnalyticsLog.Props(
                ("method", method.ToString()),
                ("k", k.ToString("G6")),
                ("n", Count.ToString()),
                ("via", "Try")));
        try
        {
            return Full.TryControlLimits(out limits, method, k, floor);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex, SeriesId);
            throw;
        }
    }

    /// <summary>Encounter-order points. X is the index, Y is the value.</summary>
    public IReadOnlyList<ChartPoint> SampleOrderPoints()
    {
        var points = new ChartPoint[Values.Count];
        for (var i = 0; i < Values.Count; i++)
            points[i] = new ChartPoint(i, (double)Values[i]);
        return points;
    }

    /// <summary>Ascending-value points. X is the rank index, Y is <see cref="Sorted"/>.</summary>
    public IReadOnlyList<ChartPoint> SortedPoints()
    {
        var points = new ChartPoint[Sorted.Count];
        for (var i = 0; i < Sorted.Count; i++)
            points[i] = new ChartPoint(i, (double)Sorted[i]);
        return points;
    }

    /// <summary>Empirical CDF. X is the sorted value, Y is (i + 1) / n.</summary>
    public IReadOnlyList<ChartPoint> EcdfPoints()
    {
        var n = Sorted.Count;
        var points = new ChartPoint[n];
        for (var i = 0; i < n; i++)
            points[i] = new ChartPoint((double)Sorted[i], (i + 1d) / n);
        return points;
    }

    /// <summary>Freedman–Diaconis histogram. X is bin midpoint, Y is count.</summary>
    public IReadOnlyList<ChartPoint> HistogramPoints()
        => HistogramAsPoints(relative: false);

    /// <summary>Freedman–Diaconis histogram. X is bin midpoint, Y is relative frequency.</summary>
    public IReadOnlyList<ChartPoint> HistogramRelativePoints()
        => HistogramAsPoints(relative: true);

    /// <summary>OLS trend through the histogram counts. Same X as <see cref="HistogramPoints"/>.</summary>
    public IReadOnlyList<ChartPoint> HistogramTrendPoints()
        => Full.Frequency.HistogramTrend();

    /// <summary>Count-descending bins plus cumulative share.</summary>
    public IReadOnlyList<ParetoPoint> ParetoPoints()
        => Full.Frequency.Pareto();

    /// <summary>
    /// Timestamped observations sorted by <c>At.UtcTicks</c>, then original encounter index.
    /// Null timestamps are omitted. Empty list when this series has no times. Does not throw.
    /// Encounter order remains <see cref="SampleOrderPoints"/> plus <see cref="Times"/>.
    /// </summary>
    public IReadOnlyList<TimedValue> TimeSeriesPoints()
    {
        if (!HasTimestamps)
            return [];

        var list = new List<(int Index, DateTimeOffset At, decimal Value)>();
        for (var i = 0; i < Values.Count; i++)
        {
            if (Times[i] is { } at)
                list.Add((i, at, Values[i]));
        }

        list.Sort((a, b) =>
        {
            var byTime = a.At.UtcTicks.CompareTo(b.At.UtcTicks);
            return byTime != 0 ? byTime : a.Index.CompareTo(b.Index);
        });

        var points = new TimedValue[list.Count];
        for (var i = 0; i < list.Count; i++)
            points[i] = new TimedValue(list[i].At, list[i].Value);
        return points;
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

    private static void RequireWindow(DateTimeOffset startInclusive, DateTimeOffset endExclusive)
    {
        if (startInclusive < endExclusive)
            return;
        throw new ArgumentException("Window start must be earlier than end.", nameof(startInclusive));
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
        {
            AnalyticsLog.Error(
                AnalyticsEvents.SeriesRejectedEmptyObservations,
                VestigiumStatus.Failed,
                AnalyticsCatalog.Subcategories.Series,
                "rejected empty observations");
            throw new ArgumentException("A numeric series must contain at least one value.", nameof(observations));
        }

        return (values, times);
    }

    private static string Fmt(double? value) => value is { } v ? v.ToString("G6") : "null";
}
