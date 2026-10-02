using ScottPlot;

namespace Vestigium.Helpers.Charts;

public static partial class ChartView
{
    internal static void ApplySpan(Plot plot, ChartOptions options)
    {
        if (options.XMin is double x0 && options.XMax is double x1 && x1 > x0)
            plot.Axes.SetLimitsX(x0, x1);
        if (options.YMin is double y0 && options.YMax is double y1 && y1 > y0)
            plot.Axes.SetLimitsY(y0, y1);
    }
}
