using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScottPlot.WPF;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

namespace Vestigium.Helpers.Charts.MenuLab;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => BuildPlots();
    }

    private void BuildPlots()
    {
        var ys = Enumerable.Range(0, 40).Select(i => 8 + Math.Sin(i / 4d) + i % 5 * 0.2).ToArray();

        var broken = new WpfPlot { MinHeight = 280 };
        broken.Plot.Title("Default ScottPlot menu");
        broken.Plot.Add.Scatter(ys);
        broken.Refresh();
        BrokenHost.Child = broken;

        var series = NumericSeries.From(ys.Select(v => (decimal)v).ToArray(), "lab-rtt");
        FixedHost.Child = ChartView.Line(series, new ChartOptions
        {
            Title = "ChartView HostMenu",
            XLabel = "Request",
            YLabel = "RTT (ms)",
            ShowLegend = true,
            HostMenu = true,
            Stretch = true,
            Height = 280
        });
    }

    private void ThemeTrap_Changed(object sender, RoutedEventArgs e)
    {
        var resources = Application.Current.Resources;
        resources.Remove(typeof(MenuItem));
        if (ThemeTrap.IsChecked != true)
            return;

        var style = new Style(typeof(MenuItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        resources[typeof(MenuItem)] = style;
    }
}
