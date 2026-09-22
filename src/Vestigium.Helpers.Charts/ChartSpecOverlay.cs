using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

internal static class ChartSpecOverlay
{
    public static IReadOnlyList<(double Y, string Name, string Hex)> Lines(SpecLimits? spec)
    {
        if (spec is null)
            return [];
        var list = new List<(double, string, string)>(2);
        if (spec.Lower is { } lsl)
            list.Add((lsl, "LSL", Palette.Lsl));
        if (spec.Upper is { } usl)
            list.Add((usl, "USL", Palette.Usl));
        return list;
    }
}
