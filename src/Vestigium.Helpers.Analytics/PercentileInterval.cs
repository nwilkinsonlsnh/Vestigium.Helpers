using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Interval for a sample percentile. PR04.001 is the sample range (honest, wide).
/// PR04.002 tightens to an order-statistic pair that meets γ.
/// This is not a confidence interval for the mean and not an SLA.
/// </summary>
public sealed class PercentileInterval
{
    public const string SampleRangeMethod = "SampleRange";
    public const string OrderStatisticMethod = "OrderStatistic";

    /// <summary>Percentile p in [0, 1].</summary>
    public required double P { get; init; }

    /// <summary>Requested coverage γ in (0, 1).</summary>
    public required double Level { get; init; }

    /// <summary>Lower bound — an order statistic, never an interpolated cut.</summary>
    public required decimal Lower { get; init; }

    /// <summary>Upper bound — an order statistic, never an interpolated cut.</summary>
    public required decimal Upper { get; init; }

    /// <summary><see cref="SampleRangeMethod"/> until PR04.002.</summary>
    public required string Method { get; init; }

    /// <summary>True when the chosen (j, k) pair meets γ. False for the sample-range stand-in.</summary>
    public required bool ReachedCoverage { get; init; }

    /// <summary>1-based rank of <see cref="Lower"/>.</summary>
    public required int LowerRank { get; init; }

    /// <summary>1-based rank of <see cref="Upper"/>.</summary>
    public required int UpperRank { get; init; }

    internal static PercentileInterval For(SeriesSlice slice, double p, double level)
    {
        ArgumentNullException.ThrowIfNull(slice);
        var sorted = slice.Sorted;
        if (sorted.Count == 0)
            Quantiles.RejectEmpty();

        _ = Quantiles.Inclusive(sorted, p);
        var gamma = ConfidenceLevel.Of(level);

        AnalyticsLog.Debug(
            AnalyticsEvents.ConfidenceEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Confidence,
            "enter percentile-interval",
            properties: AnalyticsLog.Props(
                ("p", p.ToString("G6")),
                ("gamma", gamma.ToString("G6")),
                ("n", sorted.Count.ToString())));

        var interval = new PercentileInterval
        {
            P = p,
            Level = gamma,
            Lower = sorted[0],
            Upper = sorted[^1],
            Method = SampleRangeMethod,
            ReachedCoverage = false,
            LowerRank = 1,
            UpperRank = sorted.Count
        };

        AnalyticsLog.Information(
            AnalyticsEvents.ConfidenceComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Confidence,
            "percentile-interval computed",
            properties: AnalyticsLog.Props(
                ("method", interval.Method),
                ("reached", "false")));
        return interval;
    }
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
