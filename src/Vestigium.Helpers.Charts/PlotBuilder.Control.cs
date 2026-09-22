using ScottPlot;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    private static void FillControl(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var limits = spec.Limits ?? options.Limits
            ?? throw new ArgumentNullException(nameof(spec), "Control chart requires ControlLimits from Analytics.");
        var (xs, ys) = Xy(spec);
        var inX = new List<double>();
        var inY = new List<double>();
        var outX = new List<double>();
        var outY = new List<double>();
        for (var i = 0; i < ys.Length; i++)
        {
            if (limits.IsOutOfControl(ys[i]))
            {
                outX.Add(xs[i]);
                outY.Add(ys[i]);
            }
            else
            {
                inX.Add(xs[i]);
                inY.Add(ys[i]);
            }
        }

        var line = plot.Add.ScatterLine(xs, ys);
        line.Color = Primary(options);
        line.LegendText = spec.Source?.Name ?? "sample";
        if (inX.Count > 0)
        {
            var ok = plot.Add.Scatter(inX, inY);
            ok.Color = Primary(options);
            ok.LegendText = "in control";
        }

        if (outX.Count > 0)
        {
            var bad = plot.Add.Scatter(outX, outY);
            bad.Color = Color.FromHex(Palette.Outlier);
            bad.LegendText = "outside";
        }

        var rules = spec.RunRules ?? options.RunRules;
        var (rx, ry) = ChartControlOverlay.RulePoints(xs, ys, rules);
        if (rx.Length > 0)
        {
            var marks = plot.Add.Scatter(rx, ry);
            marks.Color = Color.FromHex(Palette.Rule);
            marks.LegendText = "run rule";
        }

        AddHLine(plot, limits.Center, Palette.Cl, "CL");
        AddHLine(plot, limits.Upper, Palette.Ucl, "UCL");
        AddHLine(plot, limits.Lower, Palette.Lcl, "LCL");

        foreach (var (y, name, hex) in ChartSpecOverlay.Lines(spec.Spec ?? options.Spec))
            AddHLine(plot, y, hex, name);
    }
}
