using System.Windows;
using System.Windows.Media;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Fills <see cref="ChartOptions"/> from Vestigium brush token strings.
/// Does not reference Vestigium.Themes. A missing token keeps the documented hex fallback.
/// </summary>
public static class ChartTheme
{
    public const string AccentPrimary = "Vestigium.Brushes.Accent.Primary";
    public const string SurfaceWindow = "Vestigium.Brushes.Surface.Window";
    public const string SurfaceCard = "Vestigium.Brushes.Surface.Card";
    public const string TextPrimary = "Vestigium.Brushes.Text.Primary";
    public const string StrokeSubtle = "Vestigium.Brushes.Stroke.Subtle";

    public static readonly string[] SeriesFallback =
    [
        "#4C6B8A",
        "#C47B4A",
        "#2F4F4F",
        "#3B6EA3",
        "#5B8C5A",
        "#8C5A7A"
    ];

    public static string SeriesKey(int index)
    {
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "Series tokens are 1-based.");
        return $"Vestigium.Brushes.Series.{index}";
    }

    public static ChartOptions Apply(ChartOptions? options = null, ResourceDictionary? resources = null)
    {
        var source = options ?? new ChartOptions();
        var lookup = resources ?? Application.Current?.Resources;
        var series = Series(lookup);
        return source with
        {
            Color = source.Color ?? series[0],
            FigureColor = source.FigureColor ?? Token(lookup, SurfaceWindow, "#FFFFFF"),
            DataColor = source.DataColor ?? Token(lookup, SurfaceCard, "#FFFFFF"),
            AxisColor = source.AxisColor ?? Token(lookup, TextPrimary, "#1F2A33"),
            GridColor = source.GridColor ?? Token(lookup, StrokeSubtle, Palette.Grid),
            SeriesColors = source.SeriesColors ?? series
        };
    }

    private static string[] Series(ResourceDictionary? lookup)
    {
        var colors = new string[SeriesFallback.Length];
        colors[0] = Token(lookup, AccentPrimary, SeriesFallback[0]);
        for (var i = 1; i < colors.Length; i++)
            colors[i] = Token(lookup, SeriesKey(i), SeriesFallback[i]);
        return colors;
    }

    private static string Token(ResourceDictionary? lookup, string key, string fallback)
    {
        if (lookup?.Contains(key) == true && lookup[key] is SolidColorBrush brush)
            return $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
        return fallback;
    }
}
