using System.IO;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Façade. Identity + Probe keep the suite smoke contract.
/// Real widgets live on <see cref="ChartView"/>.
/// Logging goes through Vestigium.Logging (APPID Charts, EVENTID 16500+).
/// This library never calls <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class ChartHelper
{
    public static string Identity => "Vestigium.Helpers.Charts";

    public static string Probe()
    {
        ChartsLog.Debug(
            ChartsEvents.ProbeEnter,
            VestigiumStatus.Pending,
            ChartsCatalog.Subcategories.Probe,
            "enter Probe");

        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "probe");
        var limits = series.ControlLimits();
        var path = Path.Combine(Path.GetTempPath(), "Vestigium", "Charts", $"probe-{series.SeriesId}.png");
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Control, Source = series, Limits = limits, Title = "Probe control" },
            path,
            640,
            240);
        if (File.Exists(path))
            File.Delete(path);

        ChartsLog.Information(
            ChartsEvents.ProbeComplete,
            VestigiumStatus.Success,
            ChartsCatalog.Subcategories.Probe,
            "probe complete",
            series.SeriesId,
            ChartsLog.Props(
                ("identity", Identity),
                ("cl", limits.Center.ToString("G6")),
                ("ucl", limits.Upper.ToString("G6")),
                ("lcl", limits.Lower.ToString("G6"))));
        return Identity;
    }
}
