using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ChartViewTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Charts", ChartHelper.Identity);

    [Fact]
    public void Probe_writes_pending_then_success()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Charts, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Equal("Vestigium.Helpers.Charts", ChartHelper.Probe());
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"APPID\":\"Charts\"") && l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"STATUS\":\"Success\"") && l.Contains("Identity=Vestigium.Helpers.Charts"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void SavePng_writes_each_kind()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "odd");
        var limits = series.ControlLimits();
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        void Save(ChartKind kind, ControlLimits? l = null)
        {
            var path = Path.Combine(dir, kind + ".png");
            ChartView.SavePng(new ChartSpec { Kind = kind, Source = series, Limits = l, Title = kind.ToString() }, path, 400, 200);
            Assert.True(new FileInfo(path).Length > 8);
        }

        Save(ChartKind.Histogram);
        Save(ChartKind.Ecdf);
        Save(ChartKind.Line);
        Save(ChartKind.Scatter);
        Save(ChartKind.Column);
        Save(ChartKind.Bar);
        Save(ChartKind.Pie);
        Save(ChartKind.Pareto);
        Save(ChartKind.Box);
        Save(ChartKind.Bands);
        Save(ChartKind.MeanInterval);
        Save(ChartKind.Control, limits);
    }

    [Fact]
    public void Histogram_with_bell_and_linear_trend_on_one_to_five()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5 }, "seq");
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Histogram,
                Source = series,
                Options = new ChartOptions { ShowBellCurve = true }
            },
            Path.Combine(dir, "bell.png"),
            400,
            200);
        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Line,
                Source = series,
                Options = new ChartOptions { Trend = TrendKind.Linear }
            },
            Path.Combine(dir, "trend.png"),
            400,
            200);
        var y = new[] { 1d, 2d, 3d, 4d, 5d };
        var x = new[] { 0d, 1d, 2d, 3d, 4d };
        var fit = TrendFit.Linear(x, y);
        Assert.NotNull(fit);
        Assert.Equal(1d, fit!.Slope, 6);
        Assert.Equal(1d, fit.RSquared, 6);
    }

    [Fact]
    public void Box_five_number_uses_min_and_max_as_whiskers()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 40 }, "spike");
        var five = PlotBuilder.LayoutBox(series.Full, BoxWhiskerKind.FiveNumber);
        var tukey = PlotBuilder.LayoutBox(series.Full, BoxWhiskerKind.Tukey);
        Assert.Equal(1d, five.WhiskerMin);
        Assert.Equal(40d, five.WhiskerMax);
        Assert.Empty(five.Outliers);
        Assert.True(tukey.WhiskerMax < 40);
        Assert.Contains(40m, tukey.Outliers);
    }

    [Fact]
    public void Shape_samples_are_symmetric_right_and_left()
    {
        var mid = ChartSamples.Symmetric();
        var right = ChartSamples.RightTail();
        var left = ChartSamples.LeftTail();
        Assert.True(Math.Abs(mid.Full.Skewness ?? 9) < 0.35);
        Assert.True((right.Full.Skewness ?? 0) > 0.8);
        Assert.True((left.Full.Skewness ?? 0) < -0.8);
    }

    [Fact]
    public void Control_requires_limits_and_rejects_malformed_band()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5 });
        Assert.Throws<ArgumentNullException>(() => ChartView.Control(series, null!));
        Assert.Throws<ArgumentException>(() =>
            ChartView.Control(series, new ControlLimits { Center = 5, Upper = 4, Lower = 1, Method = ControlLimitMethod.CallerSupplied }));
    }

    [Fact]
    public void Pie_collapses_more_than_twelve_slices()
    {
        var slices = Enumerable.Range(1, 13)
            .Select(i => new ChartSlice { Label = "S" + i, Value = 1 })
            .ToArray();
        var collapsed = PlotBuilder.PieSlices(new ChartSpec { Kind = ChartKind.Pie, Slices = slices });
        Assert.Equal(12, collapsed.Count);
        Assert.Equal("Other", collapsed[^1].Label);
        Assert.Equal(2, collapsed[^1].Value);
    }

    [Fact]
    public void Public_surface_does_not_name_scottplot()
    {
        var names = typeof(ChartView).Assembly.GetExportedTypes().Select(t => t.FullName ?? t.Name);
        Assert.DoesNotContain(names, n => n.Contains("ScottPlot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SavePng_rejects_unknown_kind_blank_path_and_missing_source()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5 });
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "x.png");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartView.SavePng(new ChartSpec { Kind = (ChartKind)99, Source = series }, path));
        Assert.Throws<ArgumentException>(() =>
            ChartView.SavePng(new ChartSpec { Kind = ChartKind.Histogram, Source = series }, "  "));
        Assert.Throws<ArgumentException>(() =>
            ChartView.SavePng(new ChartSpec { Kind = ChartKind.Histogram }, path));
        Assert.Throws<ArgumentNullException>(() =>
            ChartView.SavePng(new ChartSpec { Kind = ChartKind.Control, Source = series }, path));
        Assert.Throws<ArgumentException>(() =>
            ChartView.SavePng(new ChartSpec
            {
                Kind = ChartKind.Pie,
                Slices = [new ChartSlice { Label = "z", Value = 0 }]
            }, path));
        Assert.Throws<ArgumentException>(() =>
            ChartView.SavePng(new ChartSpec
            {
                Kind = ChartKind.Pareto,
                Slices = [new ChartSlice { Label = "z", Value = 0 }]
            }, path));
        Assert.Throws<ArgumentException>(() => ChartView.Scatter(new[] { 1d }, new[] { 1d, 2d }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartSamples.Symmetric(n: 1));
        Assert.Null(TrendFit.Linear([1d], [2d]));
        Assert.Null(TrendFit.Linear([1d, 2d], [3d]));
        Assert.Null(TrendFit.Linear([1d, 1d], [3d, 4d]));
        var flat = TrendFit.Linear([1d, 2d], [5d, 5d]);
        Assert.NotNull(flat);
        Assert.Equal(1d, flat!.RSquared, 6);
    }

    [Fact]
    public void SavePng_options_bell_skip_control_outliers_and_timestamps()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 40 }, "spike");
        var flat = NumericSeries.From(new[] { 5, 5, 5, 5, 5 }, "flat");
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Histogram,
                Source = series,
                Title = "hist",
                Options = new ChartOptions
                {
                    ShowGrid = false,
                    ShowLegend = false,
                    ShowBellCurve = true,
                    Color = "#226688",
                    XLabel = "x",
                    YLabel = "y"
                }
            },
            Path.Combine(dir, "opts.png"),
            320,
            180);

        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Histogram, Source = flat, Options = new ChartOptions { ShowBellCurve = true } },
            Path.Combine(dir, "flat-bell.png"),
            320,
            180);

        var limits = series.ControlLimits();
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Control, Source = series, Limits = limits },
            Path.Combine(dir, "control.png"),
            320,
            180);

        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Box, Source = series, Options = new ChartOptions { BoxWhisker = BoxWhiskerKind.Tukey } },
            Path.Combine(dir, "tukey.png"),
            320,
            180);

        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Pareto,
                Slices =
                [
                    new ChartSlice { Label = "A", Value = 5 },
                    new ChartSlice { Label = "B", Value = 3 },
                    new ChartSlice { Label = "C", Value = 1 }
                ],
                Options = new ChartOptions { ShowParetoLine = false }
            },
            Path.Combine(dir, "pareto.png"),
            320,
            180);

        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Scatter,
                Series = [new ChartSeries { X = [0, 1, 2], Y = [1, 2, 3] }],
                Options = new ChartOptions { Trend = TrendKind.Linear }
            },
            Path.Combine(dir, "xy.png"),
            320,
            180);

        var t0 = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var timed = NumericSeries.FromObservations(
        [
            new Observation(1m, t0),
            new Observation(2m, t0.AddSeconds(1)),
            new Observation(3m, t0.AddSeconds(2))
        ], "timed");
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Line, Source = timed },
            Path.Combine(dir, "timed.png"),
            320,
            180);

        Assert.True(new FileInfo(Path.Combine(dir, "opts.png")).Length > 8);
    }

    [Fact]
    public void SavePng_pareto_from_series_control_limits_on_line_and_box_five()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 40 }, "spike");
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Pareto, Source = series },
            Path.Combine(dir, "pareto-series.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Line,
                Source = series,
                Limits = series.ControlLimits(),
                Options = new ChartOptions { ShowLegend = true }
            },
            Path.Combine(dir, "line-limits.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Box,
                Source = series,
                Options = new ChartOptions { BoxWhisker = BoxWhiskerKind.FiveNumber }
            },
            Path.Combine(dir, "five.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec
            {
                Kind = ChartKind.Control,
                Source = NumericSeries.From(new[] { 4, 5, 6 }, "in"),
                Limits = series.ControlLimits()
            },
            Path.Combine(dir, "in-control.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Ecdf, Source = series, Options = new ChartOptions { Color = " " } },
            Path.Combine(dir, "ecdf.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Bands, Source = series },
            Path.Combine(dir, "bands.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.MeanInterval, Source = series },
            Path.Combine(dir, "mean.png"),
            320,
            180);
        ChartView.SavePng(
            new ChartSpec { Kind = ChartKind.Pie, Source = series },
            Path.Combine(dir, "pie-series.png"),
            320,
            180);
        Assert.True(new FileInfo(Path.Combine(dir, "pareto-series.png")).Length > 8);
    }

}
