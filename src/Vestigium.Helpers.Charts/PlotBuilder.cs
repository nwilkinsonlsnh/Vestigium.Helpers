using ScottPlot;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static class PlotBuilder
{
    public static Plot Create(ChartSpec spec)
    {
        var plot = new Plot();
        Fill(plot, spec);
        return plot;
    }

    public static void Fill(Plot plot, ChartSpec spec)
    {
        plot.FigureBackground.Color = Colors.White;
        plot.DataBackground.Color = Colors.White;
        var options = spec.Options ?? new ChartOptions();
        if (!options.ShowGrid)
            plot.Grid.IsVisible = false;
        plot.Legend.IsVisible = options.ShowLegend;
        plot.Axes.Color(Color.FromHex("#1F2A33"));
        plot.Grid.MajorLineColor = Color.FromHex(Palette.Grid);

        switch (spec.Kind)
        {
            case ChartKind.Histogram:
                FillHistogram(plot, spec, options);
                break;
            case ChartKind.Ecdf:
                FillEcdf(plot, spec, options);
                break;
            case ChartKind.Line:
            case ChartKind.Scatter:
                FillXy(plot, spec, options, spec.Kind == ChartKind.Line);
                break;
            case ChartKind.Column:
            case ChartKind.Bar:
                FillBars(plot, spec, options, horizontal: spec.Kind == ChartKind.Bar);
                break;
            case ChartKind.Pie:
                FillPie(plot, spec);
                break;
            case ChartKind.Pareto:
                FillPareto(plot, spec, options);
                break;
            case ChartKind.Box:
                FillBox(plot, spec, options);
                break;
            case ChartKind.Bands:
                FillBands(plot, spec, options);
                break;
            case ChartKind.MeanInterval:
                FillMeanInterval(plot, spec, options);
                break;
            case ChartKind.Control:
                FillControl(plot, spec, options);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "Unknown chart kind.");
        }

        var title = options.Title ?? spec.Title;
        if (!string.IsNullOrWhiteSpace(title))
            plot.Title(title);
        if (!string.IsNullOrWhiteSpace(options.XLabel))
            plot.XLabel(options.XLabel);
        if (!string.IsNullOrWhiteSpace(options.YLabel))
            plot.YLabel(options.YLabel);
    }

    private static Color Primary(ChartOptions options)
        => Color.FromHex(string.IsNullOrWhiteSpace(options.Color) ? Palette.Primary : options.Color);

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

        if (options.ShowBellCurve && series.Full.Mean is { } mu && series.Full.StdDev is { } s and not 0)
        {
            var min = (double)series.Full.Min!.Value;
            var max = (double)series.Full.Max!.Value;
            var lo = Math.Min(min, mu - 3.5 * s);
            var hi = Math.Max(max, mu + 3.5 * s);
            var binWidth = bins.Count == 0
                ? 1
                : (double)(bins[0].UpperInclusive - bins[0].LowerInclusive);
            if (binWidth <= 0)
                binWidth = Math.Max(1e-9, (max - min) / Math.Max(1, bins.Count));
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
    }

    private static void FillEcdf(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var points = RequireSeries(spec).EcdfPoints();
        var xs = points.Select(p => p.X).ToArray();
        var ys = points.Select(p => p.Y).ToArray();
        var sc = plot.Add.ScatterLine(xs, ys);
        sc.Color = Primary(options);
        sc.LegendText = "ECDF";
        plot.Axes.SetLimitsY(0, 1);
    }

    private static void FillXy(Plot plot, ChartSpec spec, ChartOptions options, bool line)
    {
        var (xs, ys) = Xy(spec);
        var sc = line ? plot.Add.ScatterLine(xs, ys) : plot.Add.Scatter(xs, ys);
        sc.Color = Primary(options);
        sc.LegendText = spec.Source?.Name ?? "series";
        ApplyLimits(plot, options.Limits ?? spec.Limits, xs, ys);

        if (options.Trend == TrendKind.Linear)
        {
            var fit = TrendFit.Linear(xs, ys);
            if (fit is not null)
            {
                var x0 = xs.Min();
                var x1 = xs.Max();
                var tr = plot.Add.ScatterLine(new[] { x0, x1 }, new[] { fit.Intercept + fit.Slope * x0, fit.Intercept + fit.Slope * x1 });
                tr.Color = Color.FromHex(Palette.Trend);
                tr.LegendText = $"trend R²={fit.RSquared:F3}";
            }
        }
    }

    private static void FillBars(Plot plot, ChartSpec spec, ChartOptions options, bool horizontal)
    {
        var (xs, ys, labels) = BarData(spec);
        var bars = new List<Bar>(ys.Length);
        for (var i = 0; i < ys.Length; i++)
        {
            bars.Add(new Bar
            {
                Position = xs[i],
                Value = ys[i],
                FillColor = Primary(options)
            });
        }

        var added = plot.Add.Bars(bars);
        added.Horizontal = horizontal;
        if (labels.Length == ys.Length)
            plot.Axes.Bottom.SetTicks(xs, labels);
    }

    private static void FillPie(Plot plot, ChartSpec spec)
    {
        var slices = PieSlices(spec);
        var values = slices.Select(s => s.Value).ToArray();
        var pie = plot.Add.Pie(values);
        for (var i = 0; i < pie.Slices.Count && i < slices.Count; i++)
            pie.Slices[i].LegendText = $"{slices[i].Label} ({slices[i].Value:G4})";
    }

    private static void FillPareto(Plot plot, ChartSpec spec, ChartOptions options)
    {
        IReadOnlyList<(string Label, double Value)> items;
        if (spec.Slices is { Count: > 0 } slices)
        {
            items = slices.Where(s => s.Value > 0).Select(s => (s.Label, s.Value)).ToArray();
        }
        else
        {
            var points = RequireSeries(spec).ParetoPoints();
            items = points.Select(p => ($"{p.Midpoint:G4}", p.Count)).ToArray();
        }

        var ordered = items.OrderByDescending(i => i.Value).ToArray();
        if (ordered.Length == 0)
            throw new ArgumentException("Pareto needs at least one positive value.");

        var total = ordered.Sum(i => i.Value);
        var xs = Enumerable.Range(1, ordered.Length).Select(i => (double)i).ToArray();
        var ys = ordered.Select(i => i.Value).ToArray();
        var labels = ordered.Select(i => i.Label).ToArray();
        var bars = new List<Bar>();
        for (var i = 0; i < ys.Length; i++)
            bars.Add(new Bar { Position = xs[i], Value = ys[i], FillColor = Primary(options) });
        plot.Add.Bars(bars);
        plot.Axes.Bottom.SetTicks(xs, labels);

        if (options.ShowParetoLine)
        {
            double run = 0;
            var share = new double[ys.Length];
            for (var i = 0; i < ys.Length; i++)
            {
                run += ys[i];
                share[i] = run / total;
            }

            var line = plot.Add.ScatterLine(xs, share);
            line.Color = Color.FromHex(Palette.Trend);
            line.LegendText = "cumulative";
            line.Axes.YAxis = plot.Axes.Right;
            plot.Axes.Right.Min = 0;
            plot.Axes.Right.Max = 1;
            plot.Axes.Right.Label.Text = "share";
        }
    }

    private static void FillBox(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var s = RequireSeries(spec).Full;
        var layout = LayoutBox(s, options.BoxWhisker);
        var box = new Box
        {
            Position = 1,
            BoxMin = layout.Q1,
            BoxMax = layout.Q3,
            BoxMiddle = layout.Median,
            WhiskerMin = layout.WhiskerMin,
            WhiskerMax = layout.WhiskerMax,
            FillColor = Primary(options)
        };
        plot.Add.Box(box);

        if (layout.Outliers.Count > 0)
        {
            var ox = Enumerable.Repeat(1d, layout.Outliers.Count).ToArray();
            var oy = layout.Outliers.Select(v => (double)v).ToArray();
            var sc = plot.Add.Scatter(ox, oy);
            sc.Color = Color.FromHex(Palette.Outlier);
            sc.LegendText = "outliers";
        }

        LabelFive(plot, layout);
        plot.Axes.SetLimitsX(0.15, 2.35);
    }

    internal readonly record struct BoxLayout(
        double WhiskerMin,
        double WhiskerMax,
        double Q1,
        double Median,
        double Q3,
        IReadOnlyList<decimal> Outliers,
        BoxWhiskerKind Kind);

    internal static BoxLayout LayoutBox(SeriesSlice s, BoxWhiskerKind kind)
    {
        var q1 = (double)s.Q1!.Value;
        var median = (double)s.Median!.Value;
        var q3 = (double)s.Q3!.Value;
        if (kind == BoxWhiskerKind.Tukey)
        {
            var inFence = s.Values.Where(v =>
                (s.TukeyLowerFence is not { } lo || v >= lo) &&
                (s.TukeyUpperFence is not { } hi || v <= hi)).ToArray();
            return new BoxLayout(
                (double)(inFence.Length == 0 ? s.Min!.Value : inFence.Min()),
                (double)(inFence.Length == 0 ? s.Max!.Value : inFence.Max()),
                q1,
                median,
                q3,
                s.Outliers,
                BoxWhiskerKind.Tukey);
        }

        return new BoxLayout(
            (double)s.Min!.Value,
            (double)s.Max!.Value,
            q1,
            median,
            q3,
            [],
            BoxWhiskerKind.FiveNumber);
    }

    private static void LabelFive(Plot plot, BoxLayout layout)
    {
        void Mark(double y, string name)
        {
            var text = plot.Add.Text($"{name} {y:G4}", 1.32, y);
            text.LabelFontSize = 11;
            text.LabelFontColor = Color.FromHex("#1F2A33");
        }

        Mark(layout.WhiskerMax, layout.Kind == BoxWhiskerKind.FiveNumber ? "max" : "upper");
        Mark(layout.Q3, "Q3");
        Mark(layout.Median, "median");
        Mark(layout.Q1, "Q1");
        Mark(layout.WhiskerMin, layout.Kind == BoxWhiskerKind.FiveNumber ? "min" : "lower");
    }

    private static void FillBands(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var series = RequireSeries(spec);
        var labels = new[] { "Full", "Q1", "Q2", "Q3", "Q4", "IQR" };
        var xs = new double[] { 1, 2, 3, 4, 5, 6 };
        var bars = new List<Bar>();
        for (var i = 0; i < series.Bands.Count; i++)
        {
            var mean = series.Bands[i].Mean;
            if (mean is null)
                continue;
            bars.Add(new Bar { Position = xs[i], Value = mean.Value, FillColor = Primary(options) });
        }

        plot.Add.Bars(bars);
        plot.Axes.Bottom.SetTicks(xs, labels);
    }

    private static void FillMeanInterval(Plot plot, ChartSpec spec, ChartOptions options)
    {
        var series = RequireSeries(spec);
        var report = series.Confidence(0.95);
        if (!report.Mean.IsDefined || report.Mean.Estimate is null)
            throw new InvalidOperationException("Mean interval is undefined for this series.");
        var y = report.Mean.Estimate.Value;
        var lo = report.Mean.Lower!.Value;
        var hi = report.Mean.Upper!.Value;
        var sc = plot.Add.Scatter(new[] { 1d }, new[] { y });
        sc.Color = Primary(options);
        sc.LegendText = "mean";
        var err = plot.Add.Scatter(new[] { 1d, 1d }, new[] { lo, hi });
        err.Color = Color.FromHex(Palette.Trend);
        err.LegendText = "95% CI";
        plot.Axes.SetLimitsX(0, 2);
    }

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

        AddHLine(plot, limits.Center, Palette.Cl, "CL");
        AddHLine(plot, limits.Upper, Palette.Ucl, "UCL");
        AddHLine(plot, limits.Lower, Palette.Lcl, "LCL");
    }

    private static void ApplyLimits(Plot plot, ControlLimits? limits, double[] xs, double[] ys)
    {
        if (limits is null)
            return;
        AddHLine(plot, limits.Center, Palette.Cl, "CL");
        AddHLine(plot, limits.Upper, Palette.Ucl, "UCL");
        AddHLine(plot, limits.Lower, Palette.Lcl, "LCL");
        var ox = new List<double>();
        var oy = new List<double>();
        for (var i = 0; i < ys.Length; i++)
        {
            if (limits.IsOutOfControl(ys[i]))
            {
                ox.Add(xs[i]);
                oy.Add(ys[i]);
            }
        }

        if (ox.Count > 0)
        {
            var sc = plot.Add.Scatter(ox, oy);
            sc.Color = Color.FromHex(Palette.Outlier);
            sc.LegendText = "outside";
        }
    }

    private static void AddHLine(Plot plot, double y, string hex, string name)
    {
        var line = plot.Add.HorizontalLine(y);
        line.Color = Color.FromHex(hex);
        line.LegendText = name;
        line.LinePattern = LinePattern.Dashed;
    }

    private static NumericSeries RequireSeries(ChartSpec spec)
        => spec.Source ?? throw new ArgumentException("This chart kind needs a NumericSeries.");

    private static (double[] X, double[] Y) Xy(ChartSpec spec)
    {
        if (spec.Series is { Count: > 0 } s)
        {
            var y = s[0].Y.ToArray();
            var x = s[0].X?.ToArray() ?? Enumerable.Range(0, y.Length).Select(i => (double)i).ToArray();
            if (x.Length != y.Length)
                throw new ArgumentException("X and Y lengths must match.");
            return (x, y);
        }

        var series = RequireSeries(spec);
        if (series.HasTimestamps)
        {
            var timed = series.TimeSeriesPoints();
            if (timed.Count > 0)
            {
                var t0 = timed[0].At.UtcTicks;
                return (
                    timed.Select(t => (t.At.UtcTicks - t0) / (double)TimeSpan.TicksPerSecond).ToArray(),
                    timed.Select(t => (double)t.Value).ToArray());
            }
        }

        return (
            Enumerable.Range(0, series.Count).Select(i => (double)i).ToArray(),
            series.Values.Select(v => (double)v).ToArray());
    }

    private static (double[] X, double[] Y, string[] Labels) BarData(ChartSpec spec)
    {
        if (spec.Series is { Count: > 0 } s)
        {
            var y = s[0].Y.ToArray();
            var x = s[0].X?.ToArray() ?? Enumerable.Range(1, y.Length).Select(i => (double)i).ToArray();
            var labels = s[0].Labels?.ToArray() ?? x.Select(v => v.ToString("G4")).ToArray();
            return (x, y, labels);
        }

        var series = RequireSeries(spec);
        return (
            Enumerable.Range(1, series.Count).Select(i => (double)i).ToArray(),
            series.Values.Select(v => (double)v).ToArray(),
            Enumerable.Range(1, series.Count).Select(i => i.ToString()).ToArray());
    }

    internal static IReadOnlyList<ChartSlice> PieSlices(ChartSpec spec)
    {
        IReadOnlyList<ChartSlice> raw;
        if (spec.Slices is { Count: > 0 } slices)
            raw = slices;
        else
        {
            var series = RequireSeries(spec);
            raw = series.Full.Frequency.Frequencies
                .Select(f => new ChartSlice { Label = f.Value.ToString("G4"), Value = f.Count })
                .ToArray();
        }

        var positive = raw.Where(s => s.Value > 0).ToList();
        if (positive.Count == 0)
            throw new ArgumentException("Pie needs at least one positive slice.");
        if (positive.Count <= 12)
            return positive;
        var keep = positive.OrderByDescending(s => s.Value).Take(11).ToList();
        var other = positive.OrderByDescending(s => s.Value).Skip(11).Sum(s => s.Value);
        keep.Add(new ChartSlice { Label = "Other", Value = other });
        return keep;
    }
}
