using System.IO;
using System.Windows;
using System.Windows.Controls;
using ScottPlot.WPF;
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
        ArgumentNullException.ThrowIfNull(spec);
        ChartsLog.Debug(
            ChartsEvents.ChartEnter,
            VestigiumStatus.Pending,
            ChartsCatalog.Subcategories.Chart,
            "enter chart",
            properties: ChartsLog.Props(("kind", spec.Kind.ToString()), ("via", "From")));
        try
        {
            var view = Host(spec);
            ChartsLog.Information(
                ChartsEvents.ChartBuilt,
                VestigiumStatus.Success,
                ChartsCatalog.Subcategories.Chart,
                "chart built",
                spec.Source?.SeriesId,
                ChartsLog.Props(("kind", spec.Kind.ToString())));
            return view;
        }
        catch (Exception ex)
        {
            ChartsLog.Unexpected(ex, spec.Source?.SeriesId);
            throw;
        }
    }

    public static void SavePng(ChartSpec spec, string path, int width = 800, int height = 400)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(path))
        {
            ChartsLog.Error(
                ChartsEvents.ChartRejectedBlankPath,
                VestigiumStatus.Failed,
                ChartsCatalog.Subcategories.Chart,
                "rejected blank path");
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var target = path.Trim();
        ChartsLog.Debug(
            ChartsEvents.ChartEnter,
            VestigiumStatus.Pending,
            ChartsCatalog.Subcategories.Chart,
            "enter chart",
            properties: ChartsLog.Props(("kind", spec.Kind.ToString()), ("via", "SavePng"), ("path", target)));
        try
        {
            var plot = PlotBuilder.Create(spec);
            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            plot.SavePng(target, width, height);
            ChartsLog.Information(
                ChartsEvents.ChartSaved,
                VestigiumStatus.Success,
                ChartsCatalog.Subcategories.Chart,
                "chart saved",
                spec.Source?.SeriesId,
                ChartsLog.Props(("kind", spec.Kind.ToString()), ("path", target)));
        }
        catch (Exception ex)
        {
            ChartsLog.Unexpected(ex, spec.Source?.SeriesId);
            throw;
        }
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
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Count != y.Count)
        {
            ChartsLog.Error(
                ChartsEvents.ChartRejectedXy,
                VestigiumStatus.Failed,
                ChartsCatalog.Subcategories.Chart,
                "rejected x/y length mismatch",
                properties: ChartsLog.Props(("nx", x.Count.ToString()), ("ny", y.Count.ToString())));
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
        RequireNotEmpty(slices, nameof(slices));
        return From(new ChartSpec { Kind = ChartKind.Pie, Slices = slices, Options = options });
    }

    public static FrameworkElement Pareto(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Pareto, series, options));

    public static FrameworkElement Pareto(IReadOnlyList<ChartSlice> slices, ChartOptions? options = null)
    {
        RequireNotEmpty(slices, nameof(slices));
        return From(new ChartSpec { Kind = ChartKind.Pareto, Slices = slices, Options = options });
    }

    public static FrameworkElement Box(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Box, series, options));

    public static FrameworkElement Box(NumericSeries series, BoxWhiskerKind whisker, ChartOptions? options = null)
        => Box(series, With(options, whisker: whisker));

    public static FrameworkElement Bands(NumericSeries series, ChartOptions? options = null)
        => From(Spec(ChartKind.Bands, series, options));

    public static FrameworkElement MeanInterval(
        NumericSeries series,
        double level = ConfidenceLevel.DefaultValue,
        ChartOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        _ = series.Confidence(level);
        return From(Spec(ChartKind.MeanInterval, series, With(options, level: level)));
    }

    public static FrameworkElement Control(NumericSeries series, ControlLimits limits, ChartOptions? options = null)
        => Control(series, limits, runRules: options?.RunRules, options);

    public static FrameworkElement Control(
        NumericSeries series,
        ControlLimits limits,
        RunRuleReport? runRules,
        ChartOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(limits);
        RequireLimits(limits);
        return From(new ChartSpec
        {
            Kind = ChartKind.Control,
            Source = series,
            Limits = limits,
            RunRules = runRules ?? options?.RunRules,
            Options = options,
            Title = options?.Title ?? $"CL={limits.Center:G4}  UCL={limits.Upper:G4}  LCL={limits.Lower:G4}"
        });
    }

    public static FrameworkElement Control(
        IReadOnlyList<double> values,
        ControlLimits limits,
        ChartOptions? options = null)
    {
        RequireNotEmpty(values, nameof(values));
        ArgumentNullException.ThrowIfNull(limits);
        return From(new ChartSpec
        {
            Kind = ChartKind.Control,
            Limits = limits,
            RunRules = options?.RunRules,
            Series = [new ChartSeries { Y = values.ToArray() }],
            Options = options
        });
    }

    private static ChartSpec Spec(ChartKind kind, NumericSeries series, ChartOptions? options)
    {
        ArgumentNullException.ThrowIfNull(series);
        return new ChartSpec { Kind = kind, Source = series, Options = options, Title = options?.Title ?? series.Name };
    }

    private static ChartOptions With(
        ChartOptions? options,
        bool? showBell = null,
        TrendKind? trend = null,
        BoxWhiskerKind? whisker = null,
        double? level = null)
    {
        var o = options ?? new ChartOptions();
        if (showBell is { } bell)
            o = o with { ShowBellCurve = bell };
        if (trend is { } t)
            o = o with { Trend = t };
        if (whisker is { } w)
            o = o with { BoxWhisker = w };
        if (level is { } g)
            o = o with { IntervalLevel = g };
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

    private static void RequireNotEmpty<T>(IReadOnlyList<T> items, string paramName)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count > 0)
            return;
        ChartsLog.Error(
            ChartsEvents.ChartRejectedEmpty,
            VestigiumStatus.Failed,
            ChartsCatalog.Subcategories.Chart,
            "rejected empty chart input",
            properties: ChartsLog.Props(("param", paramName)));
        throw new ArgumentException("At least one value is required.", paramName);
    }

    private static void RequireLimits(ControlLimits limits)
    {
        if (limits.Upper > limits.Center && limits.Center > limits.Lower)
            return;
        ChartsLog.Error(
            ChartsEvents.ChartRejectedLimits,
            VestigiumStatus.Failed,
            ChartsCatalog.Subcategories.Chart,
            "rejected malformed limits",
            properties: ChartsLog.Props(
                ("ucl", limits.Upper.ToString("G6")),
                ("cl", limits.Center.ToString("G6")),
                ("lcl", limits.Lower.ToString("G6"))));
        throw new ArgumentException("UCL must be greater than CL and CL must be greater than LCL.");
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
