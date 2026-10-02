using ScottPlot;
using ScottPlot.TickGenerators;

namespace Vestigium.Helpers.Charts;

public static partial class ChartView
{
    internal static void ApplySpan(Plot plot, ChartSpec spec)
    {
        var options = spec.Options ?? new ChartOptions();
        ApplyCountAxis(plot, spec, options);
        if (options.XMin is double x0 && options.XMax is double x1 && x1 > x0)
            plot.Axes.SetLimitsX(x0, x1);
        if (options.YMin is double y0 && options.YMax is double y1 && y1 > y0)
            plot.Axes.SetLimitsY(y0, y1);
    }

    private static void ApplyCountAxis(Plot plot, ChartSpec spec, ChartOptions options)
    {
        if (!options.CountAxis || spec.Kind != ChartKind.Column)
            return;

        var peak = 0d;
        if (spec.Series is { Count: > 0 } series)
        {
            foreach (var row in series)
            {
                foreach (var value in row.Y)
                {
                    if (value > peak)
                        peak = value;
                }
            }
        }

        var max = peak <= 10 ? 10 : CountCeiling(peak);
        var upper = options.Limits?.Upper ?? spec.Limits?.Upper;
        if (upper is double fence && fence > max)
            max = CountCeiling(fence);

        var step = max <= 10 ? 1 : CountStep(max);
        plot.Axes.SetLimitsY(-1, max);
        plot.Axes.Left.TickGenerator = new NumericFixedInterval(step);
    }

    private static double CountCeiling(double peak)
        => peak <= 10 ? 10 : peak <= 20 ? 20 : peak <= 50 ? 50 : peak <= 100 ? 100 : Math.Ceiling(peak / 50d) * 50d;

    private static double CountStep(double max)
        => max <= 20 ? 2 : max <= 50 ? 5 : max <= 100 ? 10 : 25;
}
