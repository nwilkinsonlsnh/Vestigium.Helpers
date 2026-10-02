using ScottPlot;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static partial class PlotBuilder
{
    internal readonly record struct XyLine(string Name, double[] X, double[] Y);

    internal static IReadOnlyList<XyLine> ResolveXy(ChartSpec spec)
    {
        if (spec.Series is { Count: > 0 } raw)
        {
            if (raw.Count > 2)
                throw new ArgumentException("Line and Scatter accept at most two series.");

            var lines = new XyLine[raw.Count];
            for (var i = 0; i < raw.Count; i++)
                lines[i] = FromChartSeries(raw[i]);
            return lines;
        }

        return [FromSource(RequireSeries(spec))];
    }

    private static XyLine FromChartSeries(ChartSeries series)
    {
        if (series.Y.Count == 0)
            throw new ArgumentException("Y must not be empty.");

        var y = series.Y.ToArray();
        var x = series.X?.ToArray() ?? Enumerable.Range(0, y.Length).Select(i => (double)i).ToArray();
        if (x.Length != y.Length)
            throw new ArgumentException("X and Y lengths must match.");

        var name = string.IsNullOrWhiteSpace(series.Name) ? "series" : series.Name;
        return new XyLine(name, x, y);
    }

    private static XyLine FromSource(NumericSeries series)
    {
        if (series.HasTimestamps)
        {
            var timed = series.TimeSeriesPoints();
            if (timed.Count > 0)
            {
                var t0 = timed[0].At.UtcTicks;
                return new XyLine(
                    series.Name ?? "series",
                    timed.Select(t => (t.At.UtcTicks - t0) / (double)TimeSpan.TicksPerSecond).ToArray(),
                    timed.Select(t => (double)t.Value).ToArray());
            }
        }

        return new XyLine(
            series.Name ?? "series",
            Enumerable.Range(0, series.Count).Select(i => (double)i).ToArray(),
            series.Values.Select(v => (double)v).ToArray());
    }

    private static Color SeriesColor(ChartOptions options, int index)
    {
        if (options.SeriesColors is { } colors && index >= 0 && index < colors.Count && !string.IsNullOrWhiteSpace(colors[index]))
            return Color.FromHex(colors[index]);
        return index == 0 ? Primary(options) : Color.FromHex(Palette.Secondary);
    }
}
