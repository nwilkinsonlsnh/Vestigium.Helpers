namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Excel chart kinds this library can embed as native OOXML.
/// ClosedXML 0.105 does not author charts; we write chart parts after save.
/// </summary>
public enum ChartKind
{
    Column,
    Bar,
    Line,
    Pie,
    Scatter
}

public sealed class ChartSeries
{
    public required string Name { get; init; }
    public required string ValuesFormula { get; init; }
    public IReadOnlyList<double> Values { get; init; } = [];
    public string? Color { get; init; }
}

/// <summary>
/// One Excel chart anchored on a worksheet. Categories and values are A1 formulas
/// plus cached numbers so Excel and LibreOffice both render the series.
/// Anchor cells are 0-based (Excel drawing markup).
/// </summary>
public sealed class SheetChart
{
    public required string Sheet { get; init; }
    public required string Title { get; init; }
    public ChartKind Kind { get; init; } = ChartKind.Column;
    public required string CategoriesFormula { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public required IReadOnlyList<ChartSeries> Series { get; init; }
    public int FromColumn { get; init; }
    public int FromRow { get; init; } = 1;
    public int ToColumn { get; init; } = 12;
    public int ToRow { get; init; } = 18;
    public string? Color { get; init; }
    public bool NumericCategories { get; init; }
}
