using System.Numerics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Immutable snapshot of a finite numeric series plus quartile-band descriptors,
/// optional timestamps, and confidence intervals at a caller-chosen level.
/// </summary>
public sealed class NumericSeries
{
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

    public static NumericSeries FromDecimal(IEnumerable<decimal> values, string? name = null)
        => new(values, name);

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

    public string SeriesId { get; private set; } = "";
    public string? Name { get; private set; }
    public int Count => Values.Count;
    public IReadOnlyList<decimal> Values { get; private set; } = [];
    public IReadOnlyList<decimal> Sorted { get; private set; } = [];
    public IReadOnlyList<DateTimeOffset?> Times { get; private set; } = [];
    public bool HasTimestamps { get; private set; }
    public DateTimeOffset? FirstAt { get; private set; }
    public DateTimeOffset? LastAt { get; private set; }
    public SeriesWindow Window { get; private set; }

    public SeriesSlice Full { get; private set; } = null!;
    public SeriesSlice Q1 { get; private set; } = null!;
    public SeriesSlice Q2 { get; private set; } = null!;
    public SeriesSlice Q3 { get; private set; } = null!;
    public SeriesSlice Q4 { get; private set; } = null!;
    public SeriesSlice Iqr { get; private set; } = null!;

    public IReadOnlyList<SeriesSlice> Bands => [Full, Q1, Q2, Q3, Q4, Iqr];

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

    public double? MeanPValue(double hypothesizedMean)
        => ConfidenceReport.TwoSidedMeanPValue(Full.Statistics, hypothesizedMean);

    public double? MeanConfidenceLevelContaining(double hypothesizedMean)
    {
        var p = MeanPValue(hypothesizedMean);
        return p is null ? null : 1d - p.Value;
    }

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

    public ConfidenceInterval ProportionAbove(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAbove(threshold, level);

    public ConfidenceInterval ProportionAtLeast(decimal threshold, double level = ConfidenceLevel.DefaultValue)
        => Full.ProportionAtLeast(threshold, level);

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
