using System.Globalization;
using System.Numerics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Façade for descriptive statistics on a finite numeric series.
/// Real work lives on <see cref="NumericSeries"/>. Logging goes through
/// <c>Vestigium.Logging</c> (APPID Analytics, EVENTID 10500+).
/// This library never calls <see cref="VestigiumLogger.Initialize"/>.
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
        AnalyticsLog.Debug(
            AnalyticsEvents.ProbeEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Probe,
            "enter Probe");

        var series = From([12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 40.2, 12.1], "rtt-ms");
        var ci = series.Confidence(0.95);
        var p95 = series.Full.Percentile(0.95);
        var limits = series.ControlLimits();
        var mr = series.ControlLimits(ControlLimitMethod.MovingRange);

        AnalyticsLog.Information(
            AnalyticsEvents.ProbeComplete,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Probe,
            "probe complete",
            series.SeriesId,
            AnalyticsLog.Props(
                ("n", series.Count.ToString()),
                ("mean", series.Full.Mean?.ToString("G6")),
                ("p95", p95.ToString(CultureInfo.InvariantCulture)),
                ("ciLow", ci.Mean.Lower?.ToString("G6")),
                ("ciHigh", ci.Mean.Upper?.ToString("G6")),
                ("ucl", limits.Upper.ToString("G6")),
                ("mrUcl", mr.Upper.ToString("G6")),
                ("identity", Identity)));

        return Identity;
    }
}
