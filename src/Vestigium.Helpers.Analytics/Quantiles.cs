namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Excel PERCENTILE.INC / Hyndman-Fan type 7.
/// </summary>
internal static class Quantiles
{
    public static decimal Inclusive(IReadOnlyList<decimal> sorted, double p)
    {
        if (sorted.Count == 0)
            throw new ArgumentException("Cannot compute a percentile of an empty sample.", nameof(sorted));
        if (p is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(p), "Percentile p must be in [0, 1].");
        if (sorted.Count == 1)
            return sorted[0];

        var position = 1d + p * (sorted.Count - 1);
        var lo = (int)Math.Floor(position);
        var hi = (int)Math.Ceiling(position);
        if (lo == hi)
            return sorted[lo - 1];

        var a = sorted[lo - 1];
        var b = sorted[hi - 1];
        var t = (decimal)(position - lo);
        return a + t * (b - a);
    }
}
