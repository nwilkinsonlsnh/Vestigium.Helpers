using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>Western Electric 1–4 and Nelson 5–8.</summary>
public enum WesternElectricRule
{
    /// <summary>Western Electric 1: one point beyond 3σ.</summary>
    PointBeyondThreeSigma = 1,
    /// <summary>Western Electric 2: two of three consecutive beyond 2σ, same side.</summary>
    TwoOfThreeBeyondTwoSigma = 2,
    /// <summary>Western Electric 3: four of five consecutive beyond 1σ, same side.</summary>
    FourOfFiveBeyondOneSigma = 3,
    /// <summary>Western Electric 4: eight consecutive on one side of the center line.</summary>
    EightOnOneSideOfCenter = 4,

    /// <summary>Nelson 5: six consecutive strictly increasing or decreasing.</summary>
    SixIncreasingOrDecreasing = 5,

    /// <summary>Nelson 6: fifteen consecutive in zone C (absolute z below 1).</summary>
    FifteenInZoneC = 6,

    /// <summary>Nelson 7: fourteen consecutive alternating up/down.</summary>
    FourteenAlternating = 7,

    /// <summary>Nelson 8: eight consecutive with none in zone C (\|z\| ≥ 1).</summary>
    EightOutsideZoneC = 8
}

/// <summary>Indexes that participated in one rule.</summary>
public sealed class RunRuleHit
{
    /// <summary>Which Western Electric / Nelson rule fired.</summary>
    public required WesternElectricRule Rule { get; init; }
    /// <summary>Encounter-order indexes that participated in the hit.</summary>
    public required IReadOnlyList<int> Indexes { get; init; }
}

/// <summary>Shewhart run-rule evaluation of one Full series.</summary>
public sealed class RunRuleReport
{
    /// <summary>Fences used to score the rules.</summary>
    public required ControlLimits Limits { get; init; }
    /// <summary>Hits in rule-number order. Empty when none fire.</summary>
    public required IReadOnlyList<RunRuleHit> Hits { get; init; }
    /// <summary>Distinct encounter indexes across every hit, ascending.</summary>
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

        var hits = new List<RunRuleHit>();
        Add(hits, WesternElectricRule.PointBeyondThreeSigma, RuleBeyondK(y, center, sigma, 3, 1, 1));
        Add(hits, WesternElectricRule.TwoOfThreeBeyondTwoSigma, RuleBeyondK(y, center, sigma, 2, 3, 2));
        Add(hits, WesternElectricRule.FourOfFiveBeyondOneSigma, RuleBeyondK(y, center, sigma, 1, 5, 4));
        Add(hits, WesternElectricRule.EightOnOneSideOfCenter, RuleSameSide(y, center, 8));
        Add(hits, WesternElectricRule.SixIncreasingOrDecreasing, RuleTrend(y, 6));
        Add(hits, WesternElectricRule.FifteenInZoneC, RuleZoneC(y, center, sigma, 15, inside: true));
        Add(hits, WesternElectricRule.FourteenAlternating, RuleAlternate(y, 14));
        Add(hits, WesternElectricRule.EightOutsideZoneC, RuleZoneC(y, center, sigma, 8, inside: false));

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

    private static List<int> RuleTrend(IReadOnlyList<decimal> y, int window)
    {
        var set = new SortedSet<int>();
        if (y.Count < window)
            return [];

        for (var end = window - 1; end < y.Count; end++)
        {
            var start = end - window + 1;
            var up = true;
            var down = true;
            for (var i = start + 1; i <= end; i++)
            {
                if (y[i] <= y[i - 1]) up = false;
                if (y[i] >= y[i - 1]) down = false;
            }

            if (up || down)
            {
                for (var i = start; i <= end; i++)
                    set.Add(i);
            }
        }

        return [.. set];
    }

    private static List<int> RuleAlternate(IReadOnlyList<decimal> y, int window)
    {
        var set = new SortedSet<int>();
        if (y.Count < window)
            return [];

        for (var end = window - 1; end < y.Count; end++)
        {
            var start = end - window + 1;
            var first = Math.Sign(y[start + 1] - y[start]);
            if (first == 0)
                continue;
            var ok = true;
            var expect = -first;
            for (var i = start + 2; i <= end; i++)
            {
                var step = Math.Sign(y[i] - y[i - 1]);
                if (step != expect)
                {
                    ok = false;
                    break;
                }
                expect = -expect;
            }

            if (ok)
            {
                for (var i = start; i <= end; i++)
                    set.Add(i);
            }
        }

        return [.. set];
    }

    private static List<int> RuleZoneC(
        IReadOnlyList<decimal> y,
        double center,
        double sigma,
        int window,
        bool inside)
    {
        var set = new SortedSet<int>();
        if (y.Count < window || sigma <= 0)
            return [];

        for (var end = window - 1; end < y.Count; end++)
        {
            var start = end - window + 1;
            var ok = true;
            for (var i = start; i <= end; i++)
            {
                var z = Math.Abs((double)y[i] - center);
                var inC = z < sigma;
                if (inside ? !inC : inC)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
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
    /// <summary>
    /// Evaluate Western Electric 1–4 and Nelson 5–8 against the Full band of <paramref name="series"/>.
    /// </summary>
    public static RunRuleReport RunRules(
        this NumericSeries series,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        return RunRuleReport.Evaluate(series.Full, method, k, floor);
    }

    /// <summary>
    /// Evaluate Western Electric 1–4 and Nelson 5–8 against <paramref name="slice"/>.
    /// Legal only on <see cref="SliceKind.Full"/>.
    /// </summary>
    public static RunRuleReport RunRules(
        this SeriesSlice slice,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
        => RunRuleReport.Evaluate(slice, method, k, floor);
}
