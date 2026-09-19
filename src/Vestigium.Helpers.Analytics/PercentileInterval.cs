using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Interval for a sample percentile from two order statistics.
/// Not a mean interval and not an SLA.
/// </summary>
public sealed class PercentileInterval
{
    public const string SampleRangeMethod = "SampleRange";
    public const string OrderStatisticMethod = "OrderStatistic";

    public required double P { get; init; }
    public required double Level { get; init; }
    public required decimal Lower { get; init; }
    public required decimal Upper { get; init; }
    public required string Method { get; init; }
    public required bool ReachedCoverage { get; init; }
    public required int LowerRank { get; init; }
    public required int UpperRank { get; init; }

    /// <summary>Binomial coverage of the published (j, k) pair.</summary>
    public double Coverage { get; init; }

    internal static PercentileInterval For(SeriesSlice slice, double p, double level)
    {
        ArgumentNullException.ThrowIfNull(slice);
        var sorted = slice.Sorted;
        if (sorted.Count == 0)
            Quantiles.RejectEmpty();

        _ = Quantiles.Inclusive(sorted, p);
        var gamma = ConfidenceLevel.Of(level);
        var n = sorted.Count;

        AnalyticsLog.Debug(
            AnalyticsEvents.ConfidenceEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Confidence,
            "enter percentile-interval",
            properties: AnalyticsLog.Props(
                ("p", p.ToString("G6")),
                ("gamma", gamma.ToString("G6")),
                ("n", n.ToString())));

        int j;
        int k;
        double coverage;
        var reached = true;

        if (p == 0)
        {
            j = 1;
            k = 1;
            coverage = 1;
        }
        else if (p == 1)
        {
            j = n;
            k = n;
            coverage = 1;
        }
        else
        {
            (j, k, coverage, reached) = TightestPair(sorted, p, gamma);
        }

        var interval = new PercentileInterval
        {
            P = p,
            Level = gamma,
            Lower = sorted[j - 1],
            Upper = sorted[k - 1],
            Method = reached ? OrderStatisticMethod : SampleRangeMethod,
            ReachedCoverage = reached,
            LowerRank = j,
            UpperRank = k,
            Coverage = coverage
        };

        AnalyticsLog.Information(
            AnalyticsEvents.ConfidenceComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Confidence,
            "percentile-interval computed",
            properties: AnalyticsLog.Props(
                ("method", interval.Method),
                ("reached", reached ? "true" : "false"),
                ("j", j.ToString()),
                ("k", k.ToString())));
        return interval;
    }

    private static (int J, int K, double Coverage, bool Reached) TightestPair(
        IReadOnlyList<decimal> sorted,
        double p,
        double gamma)
    {
        var n = sorted.Count;
        var full = CoverageOf(n, p, 1, n);
        var bestJ = 1;
        var bestK = n;
        var bestCov = full;
        var bestSpan = n - 1;
        var bestWidth = sorted[^1] - sorted[0];
        var reached = full >= gamma;

        for (var j = 1; j <= n; j++)
        {
            for (var k = j; k <= n; k++)
            {
                var cov = CoverageOf(n, p, j, k);
                if (cov < gamma)
                    continue;
                var span = k - j;
                var width = sorted[k - 1] - sorted[j - 1];
                if (span < bestSpan || (span == bestSpan && width < bestWidth) || (span == bestSpan && width == bestWidth && j < bestJ))
                {
                    bestJ = j;
                    bestK = k;
                    bestCov = cov;
                    bestSpan = span;
                    bestWidth = width;
                    reached = true;
                }
            }
        }

        return (bestJ, bestK, bestCov, reached);
    }

    /// <summary>Σ_{i=j}^{k} C(n,i) p^i (1-p)^{n-i} with 1-based j,k in 1..n.</summary>
    internal static double CoverageOf(int n, double p, int j, int k)
        => QuantileFunctions.BinomialCdf(p, n, k) - QuantileFunctions.BinomialCdf(p, n, j - 1);
}

/// <summary>Percentile-interval doors.</summary>
public static class SeriesPercentileInterval
{
    public static PercentileInterval PercentileInterval(
        this SeriesSlice slice,
        double p,
        double level = ConfidenceLevel.DefaultValue)
        => Analytics.PercentileInterval.For(slice, p, level);

    public static PercentileInterval PercentileInterval(
        this NumericSeries series,
        double p,
        double level = ConfidenceLevel.DefaultValue)
    {
        ArgumentNullException.ThrowIfNull(series);
        return Analytics.PercentileInterval.For(series.Full, p, level);
    }
}
