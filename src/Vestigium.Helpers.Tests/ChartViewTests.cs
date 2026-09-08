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
}
