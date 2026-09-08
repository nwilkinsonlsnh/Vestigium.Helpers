using Vestigium.Helpers;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Excel PERCENTILE.INC / Hyndman-Fan type 7.
/// Rank arithmetic is decimal so interpolated cutoffs do not pick up binary dust.
/// </summary>
internal static class Quantiles
{
    public static decimal Inclusive(IReadOnlyList<decimal> sorted, double p)
    {
        if (sorted.Count == 0)
        {
            HelperLog.Reject("cannot compute a percentile of an empty sample");
            throw new ArgumentException("Cannot compute a percentile of an empty sample.", nameof(sorted));
        }
        if (p is < 0 or > 1)
        {
            HelperLog.Reject($"p={p} is not in [0, 1]");
            throw new ArgumentOutOfRangeException(nameof(p), "Percentile p must be in [0, 1].");
        }
        if (sorted.Count == 1)
            return sorted[0];

        var n = sorted.Count;
        var position = 1m + Convert.ToDecimal(p) * (n - 1);
        var lo = (int)decimal.Floor(position);
        var hi = (int)decimal.Ceiling(position);
        if (lo < 1)
            lo = 1;
        if (hi > n)
            hi = n;
        if (lo == hi)
            return sorted[lo - 1];

        var a = sorted[lo - 1];
        var b = sorted[hi - 1];
        var t = position - lo;
        return a + t * (b - a);
    }
}
