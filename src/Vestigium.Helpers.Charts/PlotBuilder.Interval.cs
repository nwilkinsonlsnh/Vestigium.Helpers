using ScottPlot;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    internal static bool TryFillExtra(Plot plot, ChartSpec spec, ChartOptions options)
    {
        if (spec.Kind != ChartKind.PercentileInterval)
            return false;
        FillPercentileInterval(plot, spec, options);
        return true;
    }

    private static void FillPercentileInterval(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var series = RequireSeries(spec);
        var p = options.PercentileP ?? 0.95;
        var gamma = options.IntervalLevel ?? ConfidenceLevel.DefaultValue;
        var interval = series.PercentileInterval(p, gamma);
        var point = (double)series.Full.Percentile(p);
        var lo = (double)interval.Lower;
        var hi = (double)interval.Upper;
        var sc = plot.Add.Scatter(new[] { 1d }, new[] { point });
        sc.Color = Primary(options);
        sc.LegendText = $"P{p * 100:0}";
        var err = plot.Add.Scatter(new[] { 1d, 1d }, new[] { lo, hi });
        err.Color = Color.FromHex(Palette.Trend);
        err.LegendText = $"{gamma:P0} {interval.Method}";
        plot.Axes.SetLimitsX(0, 2);
    }
}
