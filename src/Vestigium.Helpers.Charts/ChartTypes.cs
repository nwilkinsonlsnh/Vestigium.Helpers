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
    Control,
    PercentileInterval
}

public enum TrendKind
{
    None,
    Linear
}

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
    public bool ShowKde { get; init; }
    public TrendKind Trend { get; init; } = TrendKind.None;
    public bool ShowParetoLine { get; init; } = true;
    public BoxWhiskerKind BoxWhisker { get; init; } = BoxWhiskerKind.FiveNumber;
    public ControlLimits? Limits { get; init; }
    public double? Width { get; init; }
    public double? Height { get; init; }
    public bool Stretch { get; init; } = true;
    public bool HostMenu { get; init; } = true;
    public string? Color { get; init; }
    public string? FigureColor { get; init; }
    public string? DataColor { get; init; }
    public string? AxisColor { get; init; }
    public string? GridColor { get; init; }
    public IReadOnlyList<string>? SeriesColors { get; init; }
    public double? XMin { get; init; }
    public double? XMax { get; init; }
    public double? YMin { get; init; }
    public double? YMax { get; init; }
    public bool CountAxis { get; init; }
    public double? IntervalLevel { get; init; }
    public double? PercentileP { get; init; }
    public RunRuleReport? RunRules { get; init; }
    public SpecLimits? Spec { get; init; }
}
