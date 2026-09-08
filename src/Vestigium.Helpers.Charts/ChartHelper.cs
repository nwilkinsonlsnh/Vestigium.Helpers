using System.IO;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Façade. Identity + Probe keep the suite smoke contract.
/// Real widgets live on <see cref="ChartView"/>.
/// </summary>
public static class ChartHelper
{
    public static string Identity => "Vestigium.Helpers.Charts";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Charts;
        using var _ = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Building a demo chart from an Analytics series.");

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

        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"Chart probe complete. Identity={Identity} CL={limits.Center:G4} UCL={limits.Upper:G4} LCL={limits.Lower:G4}");
        return Identity;
    }
}
