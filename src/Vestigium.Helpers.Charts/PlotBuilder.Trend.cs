using ScottPlot;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    private static void ApplyTrend(Plot plot, ChartOptions options, double[] xs, double[] ys)
    {
        if (options.Trend != TrendKind.Linear || xs.Length == 0)
            return;

        var fit = TrendFit.Linear(xs, ys);
        if (fit is null)
            return;

        var x0 = xs.Min();
        var x1 = xs.Max();
        var tr = plot.Add.ScatterLine(
            new[] { x0, x1 },
            new[] { fit.Intercept + fit.Slope * x0, fit.Intercept + fit.Slope * x1 });
        tr.Color = Color.FromHex(Palette.Trend);
        tr.LegendText = $"trend R\u00b2={fit.RSquared:F3}";
    }
}
