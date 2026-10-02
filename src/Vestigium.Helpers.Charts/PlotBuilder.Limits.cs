using ScottPlot;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    private static void DrawLimitLines(Plot plot, ChartOptions options, Vestigium.Helpers.Analytics.ControlLimits? specLimits)
    {
        var limits = options.Limits ?? specLimits;
        if (limits is null)
            return;

        AddHLine(plot, limits.Center, ChartTheme.LimitColor(ChartTheme.AccentPrimary, Palette.Cl), "CL", LinePattern.DenselyDashed, 2.25f);
        AddHLine(plot, limits.Upper, ChartTheme.LimitColor(ChartTheme.StatusError, Palette.Ucl), "UCL", LinePattern.Dashed, 1.5f);
        AddHLine(plot, limits.Lower, ChartTheme.LimitColor(ChartTheme.StatusInfo, Palette.Lcl), "LCL", LinePattern.Dashed, 1.5f);
    }

    private static void AddHLine(Plot plot, double y, string hex, string name, LinePattern pattern, float width)
    {
        var line = plot.Add.HorizontalLine(y);
        line.Color = Color.FromHex(hex);
        line.LegendText = name;
        line.LinePattern = pattern;
        line.LineWidth = width;
    }
}
