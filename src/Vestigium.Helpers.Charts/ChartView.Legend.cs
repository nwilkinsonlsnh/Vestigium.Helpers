using System.Windows;
using ScottPlot.WPF;

namespace Vestigium.Helpers.Charts;

public static partial class ChartView
{
    /// <summary>
    /// Applies the host's remembered legend value. Charts does not store it.
    /// </summary>
    public static void ApplyLegend(FrameworkElement view, bool visible)
        => SetLegendVisible(view, visible);
}
