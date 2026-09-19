using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>Western Electric rules 1–4. Nelson 5–8 stay out of PR03.</summary>
public enum WesternElectricRule
{
    /// <summary>One point beyond 3σ.</summary>
    PointBeyondThreeSigma = 1,

    /// <summary>Two of three consecutive points beyond 2σ on the same side.</summary>
    TwoOfThreeBeyondTwoSigma = 2,

    /// <summary>Four of five consecutive points beyond 1σ on the same side.</summary>
    FourOfFiveBeyondOneSigma = 3,

    /// <summary>Eight consecutive points on the same side of the center line.</summary>
    EightOnOneSideOfCenter = 4
}

/// <summary>Indexes that participated in one rule.</summary>
public sealed class RunRuleHit
{
    public required WesternElectricRule Rule { get; init; }
    public required IReadOnlyList<int> Indexes { get; init; }
}

/// <summary>Western Electric evaluation of one Full series.</summary>
public sealed class RunRuleReport
{
    public required ControlLimits Limits { get; init; }
    public required IReadOnlyList<RunRuleHit> Hits { get; init; }
    public required IReadOnlyList<int> AllIndexes { get; init; }

    internal const string RequiresFull =
        "Western Electric rules require SliceKind.Full (encounter order of the process).";

    internal static RunRuleReport Evaluate(
        SeriesSlice slice,
        ControlLimitMethod method,
        double k,
        double? floor)
    {
        ArgumentNullException.ThrowIfNull(slice);
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter run-rules",
            properties: AnalyticsLog.Props(("method", method.ToString()), ("band", slice.Kind.ToString())));

        if (slice.Kind != SliceKind.Full)
        {
            ControlLimits.RejectLimits("run-rules-band", ("band", slice.Kind.ToString()));
            throw new InvalidOperationException(RequiresFull);
        }

        if (method == ControlLimitMethod.CallerSupplied)
        {
            ControlLimits.RejectLimits("run-rules-caller");
            throw new ArgumentException("Run rules need a computed σ. Use MeanPlusKSigma or MovingRange.", nameof(method));
        }

        var limits = ControlLimits.Compute(slice.Values, slice.Mean, slice.StdDev, method, k, floor);
        var sigma = SigmaOf(limits);
        var center = limits.Center;
        var y = slice.Values;
        var n = y.Count;

        var hits = new List<RunRuleHit>();
        Add(hits, WesternElectricRule.PointBeyondThreeSigma, RuleBeyondK(y, center, sigma, 3, 1, 1));
        Add(hits, WesternElectricRule.TwoOfThreeBeyondTwoSigma, RuleBeyondK(y, center, sigma, 2, 3, 2));
        Add(hits, WesternElectricRule.FourOfFiveBeyondOneSigma, RuleBeyondK(y, center, sigma, 1, 5, 4));
        Add(hits, WesternElectricRule.EightOnOneSideOfCenter, RuleSameSide(y, center, 8));

        var all = hits.SelectMany(h => h.Indexes).Distinct().OrderBy(i => i).ToArray();
        var report = new RunRuleReport
        {
            Limits = limits,
            Hits = NumberConvert.Freeze(hits),
            AllIndexes = NumberConvert.Freeze(all)
        };

        AnalyticsLog.Information(
            AnalyticsEvents.LimitsComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Limits,
            "run-rules computed",
            properties: AnalyticsLog.Props(
                ("hits", hits.Count.ToString()),
                ("indexes", ControlLimits.FormatOutOfControlIndexes(all))));
        return report;
    }

    private static double SigmaOf(ControlLimits limits)
        => limits.Method == ControlLimitMethod.MovingRange
            ? limits.MovingRangeBar!.Value / ControlLimits.D2Span2
            : (limits.Upper - limits.Center) / limits.K!.Value;

    private static void Add(List<RunRuleHit> hits, WesternElectricRule rule, List<int> indexes)
    {
        if (indexes.Count == 0)
            return;
        hits.Add(new RunRuleHit { Rule = rule, Indexes = NumberConvert.Freeze(indexes) });
    }

    /// <summary>
    /// Window of <paramref name="window"/> consecutive points; fire when at least
    /// <paramref name="need"/> sit beyond <paramref name="zone"/> σ on the same side.
    /// Rule 1 is window=1 need=1 zone=3.
    /// </summary>
    private static List<int> RuleBeyondK(
        IReadOnlyList<decimal> y,
        double center,
        double sigma,
        double zone,
        int window,
        int need)
    {
        var set = new SortedSet<int>();
        if (y.Count < window || sigma <= 0)
            return [];

        for (var end = window - 1; end < y.Count; end++)
        {
            var start = end - window + 1;
            var high = 0;
            var low = 0;
            for (var i = start; i <= end; i++)
            {
                var v = (double)y[i];
                if (v > center + zone * sigma) high++;
                else if (v < center - zone * sigma) low++;
            }

            if (high >= need)
            {
                for (var i = start; i <= end; i++)
                {
                    if ((double)y[i] > center + zone * sigma)
                        set.Add(i);
                }
            }

            if (low >= need)
            {
                for (var i = start; i <= end; i++)
                {
                    if ((double)y[i] < center - zone * sigma)
                        set.Add(i);
                }
            }
        }

        return [.. set];
    }

    private static List<int> RuleSameSide(IReadOnlyList<decimal> y, double center, int need)
    {
        var set = new SortedSet<int>();
        if (y.Count < need)
            return [];

        for (var end = need - 1; end < y.Count; end++)
        {
            var start = end - need + 1;
            var high = 0;
            var low = 0;
            for (var i = start; i <= end; i++)
            {
                var v = (double)y[i];
                if (v > center) high++;
                else if (v < center) low++;
            }

            if (high == need || low == need)
            {
                for (var i = start; i <= end; i++)
                    set.Add(i);
            }
        }

        return [.. set];
    }
}

/// <summary>Run-rule doors.</summary>
public static class SeriesRunRules
{
    /// <summary>Western Electric 1–4 on <see cref="NumericSeries.Full"/>.</summary>
    public static RunRuleReport RunRules(
        this NumericSeries series,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        return RunRuleReport.Evaluate(series.Full, method, k, floor);
    }

    /// <summary>Western Electric 1–4. Legal only on <see cref="SliceKind.Full"/>.</summary>
    public static RunRuleReport RunRules(
        this SeriesSlice slice,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
        => RunRuleReport.Evaluate(slice, method, k, floor);
}
