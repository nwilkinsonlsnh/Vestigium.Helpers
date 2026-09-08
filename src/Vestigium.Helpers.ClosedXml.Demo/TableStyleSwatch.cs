using System.Windows.Media;
using Vestigium.Helpers.ClosedXml;

namespace Vestigium.Helpers.ClosedXml.Demo;

public sealed class TableStyleSwatch
{
    public required string Id { get; init; }
    public required string Caption { get; init; }
    public required string ExcelName { get; init; }
    public required int Index { get; init; }
    public required Brush Header { get; init; }
    public required Brush HeaderInk { get; init; }
    public required Brush Band { get; init; }
    public required Brush BandAlt { get; init; }
    public bool IsSelected { get; init; }

    public static TableStyleSwatch From(ExcelTableStylePreview preview, bool selected) =>
        new()
        {
            Id = preview.Id,
            Caption = preview.Caption,
            ExcelName = preview.ExcelName,
            Index = preview.Index,
            Header = BrushOf(preview.Header),
            HeaderInk = BrushOf(preview.HeaderInk),
            Band = BrushOf(preview.Band),
            BandAlt = BrushOf(preview.BandAlt),
            IsSelected = selected
        };

    private static readonly Dictionary<string, SolidColorBrush> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static SolidColorBrush BrushOf(string hex)
    {
        if (Cache.TryGetValue(hex, out var hit))
            return hit;
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (brush.CanFreeze)
            brush.Freeze();
        Cache[hex] = brush;
        return brush;
    }
}
