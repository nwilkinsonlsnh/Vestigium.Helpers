using System.IO;
using System.Windows;
using System.Windows.Controls;
using ScottPlot.WPF;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Easy ScottPlot wrapper. Hosts drop the returned <see cref="FrameworkElement"/>
/// on a WPF form. Limit values come from Analytics — this type does not compute UCL/LCL.
/// </summary>
public static class ChartView
{
    public static FrameworkElement From(ChartSpec spec)
    {
        HelperGuard.NotNull(spec, nameof(spec));
        using var scope = HelperLog.Begin(
            HelperLog.AppIds.Charts,
            HelperLog.Subcategories.Chart,
            "From",
            $"kind={spec.Kind}");
        try
        {
            var view = Host(spec);
            HelperLog.Information(
                HelperLog.AppIds.Charts,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Chart,
                $"built kind={spec.Kind} series={spec.Source?.SeriesId ?? "-"}");
            return view;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static void SavePng(ChartSpec spec, string path, int width = 800, int height = 400)
    {
        HelperGuard.NotNull(spec, nameof(spec));
        var target = HelperGuard.NotBlank(path, nameof(path));
        using var _ = HelperLog.Begin(
            HelperLog.AppIds.Charts,
            HelperLog.Subcategories.Chart,
            "SavePng",
            $"kind={spec.Kind} path={target}");
        var plot = PlotBuilder.Create(spec);
        var dir = Path.GetDirectoryName(target);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        plot.SavePng(target, width, height);
        HelperLog.Information(
            HelperLog.AppIds.Charts,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Chart,
            $"saved kind={spec.Kind} path={target}");
    }

    public static FrameworkElement Histogram(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Histogram, series, options));

    public static FrameworkElement Histogram(NumericSeries series, bool showBellCurve, ChartOptions? options = null)
        => Histogram(series, With(options, showBell: showBellCurve));

    public static FrameworkElement Ecdf(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Ecdf, series, options));

    public static FrameworkElement Line(NumericSeries series, TrendKind trend = TrendKind.None, ChartOptions? options = null)
        => From(Spec(ChartKind.Line, series, With(options, trend: trend)));

    public static FrameworkElement Line(NumericSeries series, ChartOptions options)
        => Line(series, TrendKind.None, options);

    public static FrameworkElement Line(NumericSeries series, TrendKind trend, out TrendFit? fit, ChartOptions? options = null)
    {
        fit = Fit(series, trend);
        return Line(series, trend, options);
    }

    public static FrameworkElement Scatter(NumericSeries series, TrendKind trend = TrendKind.None, ChartOptions? options = null)
        => From(Spec(ChartKind.Scatter, series, With(options, trend: trend)));

    public static FrameworkElement Scatter(NumericSeries series, ChartOptions options)
        => Scatter(series, TrendKind.None, options);

    public static FrameworkElement Scatter(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        TrendKind trend = TrendKind.None,
        ChartOptions? options = null)
    {
        HelperGuard.NotNull(x, nameof(x));
        HelperGuard.NotNull(y, nameof(y));
        if (x.Count != y.Count)
        {
            HelperLog.Reject("X and Y lengths must match");
            throw new ArgumentException("X and Y lengths must match.");
        }

        return From(new ChartSpec
        {
            Kind = ChartKind.Scatter,
            Series = [new ChartSeries { X = x.ToArray(), Y = y.ToArray() }],
            Options = With(options, trend: trend)
        });
    }

    public static FrameworkElement Column(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Column, series, options));

    public static FrameworkElement Bar(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Bar, series, options));

    public static FrameworkElement Pie(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Pie, series, options));

    public static FrameworkElement Pie(IReadOnlyList<ChartSlice> slices, ChartOptions? options = null)
    {
        HelperGuard.NotEmpty(slices, nameof(slices));
        return From(new ChartSpec { Kind = ChartKind.Pie, Slices = slices, Options = options });
    }

    public static FrameworkElement Pareto(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Pareto, series, options));

    public static FrameworkElement Pareto(IReadOnlyList<ChartSlice> slices, ChartOptions? options = null)
    {
        HelperGuard.NotEmpty(slices, nameof(slices));
        return From(new ChartSpec { Kind = ChartKind.Pareto, Slices = slices, Options = options });
    }

    public static FrameworkElement Box(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Box, series, options));

    public static FrameworkElement Box(NumericSeries series, BoxWhiskerKind whisker, ChartOptions? options = null)
        => Box(series, With(options, whisker: whisker));

    public static FrameworkElement Bands(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Bands, series, options));

    public static FrameworkElement MeanInterval(NumericSeries series, double level = 0.95, ChartOptions? options = null)
    {
        HelperGuard.NotNull(series, nameof(series));
        _ = series.Confidence(level);
        return From(Spec(ChartKind.MeanInterval, series, options));
    }

    public static FrameworkElement Control(NumericSeries series, ControlLimits limits, ChartOptions? options = null)
    {
        HelperGuard.NotNull(series, nameof(series));
        HelperGuard.NotNull(limits, nameof(limits));
        if (limits.Upper <= limits.Center || limits.Center <= limits.Lower)
        {
            HelperLog.Reject($"malformed limits UCL={limits.Upper} CL={limits.Center} LCL={limits.Lower}");
            throw new ArgumentException("UCL must be greater than CL and CL must be greater than LCL.");
        }

        return From(new ChartSpec
        {
            Kind = ChartKind.Control,
            Source = series,
            Limits = limits,
            Options = options,
            Title = options?.Title ?? $"CL={limits.Center:G4}  UCL={limits.Upper:G4}  LCL={limits.Lower:G4}"
        });
    }

    public static FrameworkElement Control(
        IReadOnlyList<double> values,
        ControlLimits limits,
        ChartOptions? options = null)
    {
        HelperGuard.NotEmpty(values, nameof(values));
        HelperGuard.NotNull(limits, nameof(limits));
        return From(new ChartSpec
        {
            Kind = ChartKind.Control,
            Limits = limits,
            Series = [new ChartSeries { Y = values.ToArray() }],
            Options = options
        });
    }

    private static ChartSpec Spec(ChartKind kind, NumericSeries series, ChartOptions? options)
    {
        HelperGuard.NotNull(series, nameof(series));
        return new ChartSpec { Kind = kind, Source = series, Options = options, Title = options?.Title ?? series.Name };
    }

    private static ChartOptions With(
        ChartOptions? options,
        bool? showBell = null,
        TrendKind? trend = null,
        BoxWhiskerKind? whisker = null)
    {
        var o = options ?? new ChartOptions();
        if (showBell is { } bell)
            o = o with { ShowBellCurve = bell };
        if (trend is { } t)
            o = o with { Trend = t };
        if (whisker is { } w)
            o = o with { BoxWhisker = w };
        return o;
    }

    private static TrendFit? Fit(NumericSeries series, TrendKind trend)
    {
        if (trend != TrendKind.Linear)
            return null;
        var y = series.Values.Select(v => (double)v).ToArray();
        var x = Enumerable.Range(0, y.Length).Select(i => (double)i).ToArray();
        return TrendFit.Linear(x, y);
    }

    private static FrameworkElement Host(ChartSpec spec)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new TextBlock
            {
                Text = "ChartView hosts a WPF ScottPlot control on Windows. Use ChartView.SavePng on this OS.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12)
            };
        }

        var view = new WpfPlot();
        PlotBuilder.Fill(view.Plot, spec);
        if (spec.Options?.Width is { } w)
            view.Width = w;
        view.Height = spec.Options?.Height ?? 240;
        view.Refresh();
        return view;
    }
}
