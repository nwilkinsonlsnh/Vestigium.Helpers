using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Two snapshots compared as numbers. No host names.
/// Welch two-sample t is always attempted. A paired difference series exists only when counts match.
/// </summary>
public sealed class SeriesCompare
{
    public int Count1 { get; init; }
    public int Count2 { get; init; }
    public double? Mean1 { get; init; }
    public double? Mean2 { get; init; }

    /// <summary>Mean1 − Mean2 when both means exist.</summary>
    public double? MeanDelta { get; init; }

    public double? WelchT { get; init; }
    public double? WelchDegreesOfFreedom { get; init; }

    /// <summary>Two-sided Welch p-value. Null when the test is undefined.</summary>
    public double? WelchTwoSidedP { get; init; }

    /// <summary>New snapshot of x−y in encounter order when n1 = n2. Null otherwise.</summary>
    public NumericSeries? Paired { get; init; }

    public static SeriesCompare Of(NumericSeries left, NumericSeries right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        AnalyticsLog.Debug(
            AnalyticsEvents.SeriesEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Series,
            "enter compare",
            properties: AnalyticsLog.Props(
                ("n1", left.Count.ToString()),
                ("n2", right.Count.ToString())));

        var mean1 = left.Full.Mean;
        var mean2 = right.Full.Mean;
        double? delta = mean1 is { } a && mean2 is { } b ? a - b : null;

        NumericSeries? paired = null;
        if (left.Count == right.Count)
        {
            var diffs = new decimal[left.Count];
            for (var i = 0; i < left.Count; i++)
                diffs[i] = left.Values[i] - right.Values[i];
            paired = NumericSeries.From(diffs, name: "delta");
        }

        var welch = Welch(left.Full, right.Full);
        var report = new SeriesCompare
        {
            Count1 = left.Count,
            Count2 = right.Count,
            Mean1 = mean1,
            Mean2 = mean2,
            MeanDelta = delta,
            WelchT = welch.T,
            WelchDegreesOfFreedom = welch.Df,
            WelchTwoSidedP = welch.P,
            Paired = paired
        };

        AnalyticsLog.Information(
            AnalyticsEvents.SeriesConstructed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Series,
            "compare computed",
            properties: AnalyticsLog.Props(
                ("paired", paired is null ? "false" : "true"),
                ("p", welch.P?.ToString("G6"))));
        return report;
    }

    private static (double? T, double? Df, double? P) Welch(SeriesSlice left, SeriesSlice right)
    {
        if (left.Count < 2 || right.Count < 2)
            return default;
        if (left.Mean is not { } m1 || right.Mean is not { } m2)
            return default;
        if (left.StdDev is not { } s1 || right.StdDev is not { } s2)
            return default;

        var n1 = (double)left.Count;
        var n2 = (double)right.Count;
        var v1 = (s1 * s1) / n1;
        var v2 = (s2 * s2) / n2;
        var se2 = v1 + v2;
        if (se2 <= 0)
            return (0d, n1 + n2 - 2d, 1d);

        var t = (m1 - m2) / Math.Sqrt(se2);
        var dfNum = se2 * se2;
        var dfDen = (v1 * v1) / (n1 - 1d) + (v2 * v2) / (n2 - 1d);
        if (dfDen <= 0)
            return default;
        var df = dfNum / dfDen;
        var p = 2d * QuantileFunctions.StudentTCdf(df, -Math.Abs(t));
        if (p < 0) p = 0;
        if (p > 1) p = 1;
        return (t, df, p);
    }
}

/// <summary>Two-series door.</summary>
public static class SeriesCompareExtensions
{
    /// <summary>Compare this snapshot to <paramref name="other"/>.</summary>
    public static SeriesCompare Compare(this NumericSeries series, NumericSeries other)
        => SeriesCompare.Of(series, other);
}
