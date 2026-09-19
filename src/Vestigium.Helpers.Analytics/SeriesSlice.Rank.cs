using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>Rank helpers for <see cref="SeriesSlice"/>.</summary>
public static class SeriesSliceRanks
{
    /// <summary>
    /// Sample fraction of this band that is ≤ <paramref name="x"/>.
    /// This is a rank on the snapshot, not a confidence level and not P95-as-SLA.
    /// </summary>
    /// <returns>A value in [0, 1].</returns>
    /// <exception cref="InvalidOperationException">The band is empty.</exception>
    public static double PercentileRank(this SeriesSlice slice, decimal x)
    {
        ArgumentNullException.ThrowIfNull(slice);
        if (slice.IsEmpty)
        {
            AnalyticsLog.Error(
                AnalyticsEvents.PercentileRejectedEmpty,
                VestigiumStatus.Failed,
                AnalyticsCatalog.Subcategories.Series,
                "rejected empty percentile");
            throw new InvalidOperationException("Cannot compute percentiles of an empty slice.");
        }

        var n = slice.Count;
        var hits = 0;
        foreach (var v in slice.Values)
        {
            if (v <= x)
                hits++;
        }

        return hits / (double)n;
    }
}
