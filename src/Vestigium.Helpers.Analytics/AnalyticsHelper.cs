using System.Numerics;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Façade for descriptive statistics on a finite numeric series.
/// Real work lives on <see cref="NumericSeries"/>; this type keeps the suite identity contract.
/// Libraries never call Initialize — Probe writes through HelperLog, a no-op until the host starts logging.
/// </summary>
public static class AnalyticsHelper
{
    public static string Identity => "Vestigium.Helpers.Analytics";

    public static NumericSeries From<T>(IEnumerable<T> values, string? name = null)
        where T : INumber<T>
        => NumericSeries.From(values, name);

    public static NumericSeries FromDecimal(IEnumerable<decimal> values, string? name = null)
        => NumericSeries.FromDecimal(values, name);

    public static NumericSeries FromObservations(IEnumerable<Observation> observations, string? name = null)
        => NumericSeries.FromObservations(observations, name);

    public static string Probe()
    {
        var app = HelperLog.AppIds.Analytics;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening an in-process numeric series snapshot.");

        var series = From(new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 40.2, 12.1 }, "rtt-ms");
        var ci = series.Confidence(0.95);
        var p95 = series.Full.Percentile(0.95);

        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"n={series.Count} mean={series.Full.Mean:F2} P50={series.Full.Median} P95={p95} Q4={series.Q4.Count} highOutliers={series.Full.HighOutliers.Count} meanCI=[{ci.Mean.Lower:F2},{ci.Mean.Upper:F2}] γ=0.95 Identity={Identity}");

        return Identity;
    }
}
