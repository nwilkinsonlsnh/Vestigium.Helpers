using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>One KDE sample: x on the value axis, y = density.</summary>
public readonly record struct DensityPoint(double X, double Y);

/// <summary>
/// Gaussian KDE. Bandwidth is Silverman 1.06 s n^{-1/5}.
/// Empty grid when s is not positive. No extra NuGet.
/// </summary>
public static class KernelDensity
{
    /// <summary>
    /// Gets the default number of points for the PDF.
    /// </summary>
    public const int DefaultCount = 64;

    /// <summary>
    /// Gets the maximum number of points for the PDF.
    /// </summary>
    public const int MaxCount = 512;
    /// <summary>
    /// Gets the Silverman bandwidth factor.
    /// </summary>
    public const double Silverman = 1.06;

    public static IReadOnlyList<DensityPoint> PdfPoints(this SeriesSlice slice, int count = DefaultCount)
    {
        ArgumentNullException.ThrowIfNull(slice);
        if (count is < 1 or > MaxCount)
        {
            AnalyticsLog.Error(
                AnalyticsEvents.LimitsRejected,
                VestigiumStatus.Failed,
                AnalyticsCatalog.Subcategories.Series,
                "rejected kde count",
                properties: AnalyticsLog.Props(("count", count.ToString())));
            throw new ArgumentOutOfRangeException(nameof(count), $"PdfPoints count must be in 1..{MaxCount}.");
        }

        if (slice.Count == 0 || slice.StdDev is not > 0 || slice.Min is null || slice.Max is null)
            return NumberConvert.Freeze(Array.Empty<DensityPoint>());

        var n = slice.Count;
        var s = slice.StdDev.Value;
        var h = Silverman * s * Math.Pow(n, -0.2);
        if (h <= 0 || double.IsNaN(h) || double.IsInfinity(h))
            return NumberConvert.Freeze(Array.Empty<DensityPoint>());

        var min = (double)slice.Min.Value;
        var max = (double)slice.Max.Value;
        var lo = min - 3d * h;
        var hi = max + 3d * h;
        var points = new DensityPoint[count];
        var inv = 1d / (n * h);
        var invSqrt2Pi = 1d / Math.Sqrt(2d * Math.PI);

        for (var i = 0; i < count; i++)
        {
            var x = count == 1 ? 0.5 * (lo + hi) : lo + (hi - lo) * i / (count - 1);
            var sum = slice.Values.Select(raw => (x - (double)raw) / h).Select(u => Math.Exp(-0.5 * u * u) * invSqrt2Pi).Sum();

            points[i] = new DensityPoint(x, inv * sum);
        }

        AnalyticsLog.Information(
            AnalyticsEvents.SeriesConstructed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Series,
            "kde computed",
            properties: AnalyticsLog.Props(
                ("n", n.ToString()),
                ("count", count.ToString()),
                ("h", h.ToString("G6"))));

        return NumberConvert.Freeze(points);
    }

    public static IReadOnlyList<DensityPoint> PdfPoints(this NumericSeries series, int count = DefaultCount)
    {
        ArgumentNullException.ThrowIfNull(series);
        return series.Full.PdfPoints(count);
    }
}
