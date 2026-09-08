using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

public enum ChartKind
{
    Column,
    Bar,
    Line,
    Scatter,
    Pie,
    Histogram,
    Ecdf,
    Pareto,
    Box,
    Bands,
    MeanInterval,
    Control
}

public enum TrendKind
{
    None,
    Linear
}

/// <summary>
/// Whiskers on <see cref="ChartKind.Box"/>. Five-number is min / Q1 / median / Q3 / max.
/// Tukey stops at the last in-fence point and plots outliers.
/// </summary>
public enum BoxWhiskerKind
{
    FiveNumber = 0,
    Tukey = 1
}

public sealed record ChartOptions
{
    public string? Title { get; init; }
    public string? XLabel { get; init; }
    public string? YLabel { get; init; }
    public bool ShowLegend { get; init; } = true;
    public bool ShowGrid { get; init; } = true;
    public bool ShowBellCurve { get; init; }
    public TrendKind Trend { get; init; } = TrendKind.None;
    public bool ShowParetoLine { get; init; } = true;
    public BoxWhiskerKind BoxWhisker { get; init; } = BoxWhiskerKind.FiveNumber;
    public ControlLimits? Limits { get; init; }
    public double? Width { get; init; }
    public double? Height { get; init; }
    public string? Color { get; init; }
}

public sealed class ChartSpec
{
    public ChartKind Kind { get; init; }
    public string? Title { get; init; }
    public IReadOnlyList<ChartSeries> Series { get; init; } = [];
    public ChartOptions? Options { get; init; }
    public ControlLimits? Limits { get; init; }
    public NumericSeries? Source { get; init; }
    public IReadOnlyList<ChartSlice>? Slices { get; init; }
}

public sealed class ChartSeries
{
    public string? Name { get; init; }
    public IReadOnlyList<double> Y { get; init; } = [];
    public IReadOnlyList<double>? X { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
}

public sealed class ChartSlice
{
    public string Label { get; init; } = "";
    public double Value { get; init; }
}

public sealed class TrendFit
{
    public TrendKind Kind { get; init; }
    public double Intercept { get; init; }
    public double Slope { get; init; }
    public double RSquared { get; init; }
    public int N { get; init; }

    public static TrendFit? Linear(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return null;
        var n = x.Count;
        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0, sumYy = 0;
        for (var i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXy += x[i] * y[i];
            sumXx += x[i] * x[i];
            sumYy += y[i] * y[i];
        }

        var denom = n * sumXx - sumX * sumX;
        if (denom == 0)
            return null;
        var slope = (n * sumXy - sumX * sumY) / denom;
        var intercept = (sumY - slope * sumX) / n;
        var ssTot = sumYy - sumY * sumY / n;
        var r2 = ssTot == 0 ? 1 : 1 - Residual(x, y, intercept, slope) / ssTot;
        return new TrendFit
        {
            Kind = TrendKind.Linear,
            Intercept = intercept,
            Slope = slope,
            RSquared = r2,
            N = n
        };
    }

    private static double Residual(IReadOnlyList<double> x, IReadOnlyList<double> y, double a, double b)
    {
        double s = 0;
        for (var i = 0; i < x.Count; i++)
        {
            var e = y[i] - (a + b * x[i]);
            s += e * e;
        }

        return s;
    }
}

internal static class Palette
{
    public const string Primary = "#4C6B8A";
    public const string Secondary = "#C47B4A";
    public const string Trend = "#2F4F4F";
    public const string Bell = "#8B3A3A";
    public const string Outlier = "#A33B3B";
    public const string Cl = "#2F4F4F";
    public const string Ucl = "#A33B3B";
    public const string Lcl = "#3B6EA3";
    public const string Grid = "#D9DEE4";
}
