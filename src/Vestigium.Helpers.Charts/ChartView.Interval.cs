using System.Windows;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

public static partial class ChartView
{
    public static FrameworkElement PercentileInterval(
        NumericSeries series,
        double p = 0.95,
        double level = ConfidenceLevel.DefaultValue,
        ChartOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        _ = series.PercentileInterval(p, level);
        var o = options ?? new ChartOptions();
        o = o with { IntervalLevel = level, PercentileP = p };
        return From(Spec(ChartKind.PercentileInterval, series, o));
    }
}
