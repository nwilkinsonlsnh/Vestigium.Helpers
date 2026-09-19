using ScottPlot;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    private static void FillHistogram(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var series = RequireSeries(spec);
        var bins = series.Full.Frequency.Histogram;
        var bars = new List<Bar>(bins.Count);
        foreach (var bin in bins)
        {
            var width = (double)(bin.UpperInclusive - bin.LowerInclusive);
            if (width <= 0)
                width = 1;
            bars.Add(new Bar
            {
                Position = (double)((bin.LowerInclusive + bin.UpperInclusive) / 2m),
                Value = bin.Count,
                Size = width,
                FillColor = Primary(options)
            });
        }

        if (bars.Count > 0)
            plot.Add.Bars(bars);

        var min = series.Full.Min is { } mn ? (double)mn : 0;
        var max = series.Full.Max is { } mx ? (double)mx : 1;
        var binWidth = bins.Count == 0
            ? 1
            : (double)(bins[0].UpperInclusive - bins[0].LowerInclusive);
        if (binWidth <= 0)
            binWidth = Math.Max(1e-9, (max - min) / Math.Max(1, bins.Count));

        if (options.ShowBellCurve && series.Full.Mean is { } mu && series.Full.StdDev is { } s and not 0)
        {
            var lo = Math.Min(min, mu - 3.5 * s);
            var hi = Math.Max(max, mu + 3.5 * s);
            var xs = new double[80];
            var ys = new double[80];
            for (var i = 0; i < 80; i++)
            {
                var x = lo + (hi - lo) * i / 79d;
                var z = (x - mu) / s;
                var pdf = Math.Exp(-0.5 * z * z) / (s * Math.Sqrt(2 * Math.PI));
                xs[i] = x;
                ys[i] = series.Count * binWidth * pdf;
            }

            var bell = plot.Add.ScatterLine(xs, ys);
            bell.Color = Color.FromHex(Palette.Bell);
            bell.LegendText = "N(μ, s)";
        }

        if (options.ShowKde)
        {
            var pts = series.PdfPoints();
            if (pts.Count > 0)
            {
                var scale = series.Count * binWidth;
                var xs = pts.Select(p => p.X).ToArray();
                var ys = pts.Select(p => p.Y * scale).ToArray();
                var kde = plot.Add.ScatterLine(xs, ys);
                kde.Color = Color.FromHex(Palette.Kde);
                kde.LegendText = "KDE";
            }
        }
    }
}
