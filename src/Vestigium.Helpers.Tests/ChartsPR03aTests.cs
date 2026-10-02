using System.Windows;
using System.Windows.Media;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ChartsPR03aTests
{
    [Fact]
    public void Themed_options_read_stub_tokens_and_keep_a_caller_color()
    {
        var themed = OnSta(() =>
        {
            var resources = new ResourceDictionary();
            resources[ChartTheme.AccentPrimary] = Brush(0x11, 0x22, 0x33);
            resources[ChartTheme.SurfaceWindow] = Brush(0xAA, 0xBB, 0xCC);
            resources[ChartTheme.SurfaceCard] = Brush(0x01, 0x02, 0x03);
            resources[ChartTheme.TextPrimary] = Brush(0x10, 0x20, 0x30);
            resources[ChartTheme.StrokeSubtle] = Brush(0x40, 0x50, 0x60);
            resources[ChartTheme.SeriesKey(1)] = Brush(0x70, 0x80, 0x90);
            return ChartTheme.Apply(new ChartOptions { Color = "#ABCDEF", Title = "keep" }, resources);
        });

        Assert.Equal("#ABCDEF", themed.Color);
        Assert.Equal("keep", themed.Title);
        Assert.Equal("#AABBCC", themed.FigureColor);
        Assert.Equal("#010203", themed.DataColor);
        Assert.Equal("#102030", themed.AxisColor);
        Assert.Equal("#405060", themed.GridColor);
        Assert.Equal("#708090", themed.SeriesColors![1]);
    }

    [Fact]
    public void Missing_token_keeps_the_documented_fallback()
    {
        var themed = OnSta(() => ChartTheme.Apply(new ChartOptions(), new ResourceDictionary()));
        Assert.Equal(ChartTheme.SeriesFallback[0], themed.Color);
        Assert.Equal("#FFFFFF", themed.FigureColor);
        Assert.Equal("#FFFFFF", themed.DataColor);
        Assert.Equal("#1F2A33", themed.AxisColor);
        Assert.Equal("#D9DEE4", themed.GridColor);
        Assert.Equal(ChartTheme.SeriesFallback[1], themed.SeriesColors![1]);
    }

    [Fact]
    public void Line_and_column_draw_limit_lines_only_when_limits_are_set()
    {
        var limits = new ControlLimits
        {
            Center = 5,
            Upper = 8,
            Lower = 2,
            Method = ControlLimitMethod.CallerSupplied
        };
        var with = new ChartSpec
        {
            Kind = ChartKind.Line,
            Series = [new ChartSeries { Name = "Receive", X = [0, 1, 2], Y = [4, 5, 6] }],
            Options = new ChartOptions { Limits = limits }
        };
        var names = LegendNames(PlotBuilder.Create(with));
        Assert.Contains("CL", names);
        Assert.Contains("UCL", names);
        Assert.Contains("LCL", names);

        var column = new ChartSpec
        {
            Kind = ChartKind.Column,
            Series = [new ChartSeries { Name = "Integrity", X = [1, 2], Y = [0, 1], Labels = ["a", "b"] }],
            Options = new ChartOptions { Limits = limits, CountAxis = true }
        };
        var columnNames = LegendNames(PlotBuilder.Create(column));
        Assert.Contains("CL", columnNames);
        Assert.Contains("UCL", columnNames);
        Assert.Contains("LCL", columnNames);

        var plain = PlotBuilder.Create(new ChartSpec
        {
            Kind = ChartKind.Line,
            Series = [new ChartSeries { Name = "Receive", Y = [1, 2, 3] }]
        });
        Assert.DoesNotContain("UCL", LegendNames(plain));
    }

    private static T OnSta<T>(Func<T> action)
    {
        T? value = default;
        Exception? err = null;
        var thread = new Thread(() =>
        {
            try { value = action(); }
            catch (Exception ex) { err = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (err is not null)
            throw err;
        return value!;
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static string[] LegendNames(ScottPlot.Plot plot)
    {
        var names = new List<string>();
        foreach (var plottable in plot.GetPlottables())
        {
            var text = plottable.GetType().GetProperty("LegendText")?.GetValue(plottable) as string;
            if (!string.IsNullOrWhiteSpace(text))
                names.Add(text);
        }

        return names.ToArray();
    }
}
