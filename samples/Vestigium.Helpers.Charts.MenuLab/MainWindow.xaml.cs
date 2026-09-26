using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScottPlot.WPF;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Themes;
using WpfControl = System.Windows.Controls.Control;

namespace Vestigium.Helpers.Charts.MenuLab;

public partial class MainWindow : Window
{
    private bool _ready;

    public MainWindow()
    {
        InitializeComponent();
        ThemeBox.ItemsSource = App.Themes.AvailableThemes;
        ThemeBox.SelectedValue = App.Themes.Current?.Id ?? "StandardWPF";
        Loaded += (_, _) =>
        {
            _ready = true;
            BuildPlots();
        };
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;
        if (App.Themes.Current?.Id == id)
        {
            if (_ready)
                BuildPlots();
            return;
        }

        App.Themes.SwitchTheme(id);
        if (_ready)
            BuildPlots();
    }

    private void BuildPlots()
    {
        var ys = Enumerable.Range(0, 40).Select(i => 8 + Math.Sin(i / 4d) + i % 5 * 0.2).ToArray();
        var xs = Enumerable.Range(0, ys.Length).Select(i => (double)i).ToArray();

        var broken = new WpfPlot { MinHeight = 280 };
        broken.Plot.Title("Default ScottPlot menu");
        broken.Plot.Add.Scatter(xs, ys);
        PaintPlot(broken.Plot);
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
            Height = 280,
            Color = Hex("Vestigium.Brushes.Accent.Primary"),
            FigureColor = Hex("Vestigium.Brushes.Surface.Window"),
            DataColor = Hex("Vestigium.Brushes.Surface.Card"),
            AxisColor = Hex("Vestigium.Brushes.Text.Primary"),
            GridColor = Hex("Vestigium.Brushes.Stroke.Subtle")
        });
    }

    private static void PaintPlot(ScottPlot.Plot plot)
    {
        if (Hex("Vestigium.Brushes.Surface.Window") is { } figure)
            plot.FigureBackground.Color = ScottPlot.Color.FromHex(figure);
        if (Hex("Vestigium.Brushes.Surface.Card") is { } data)
            plot.DataBackground.Color = ScottPlot.Color.FromHex(data);
        if (Hex("Vestigium.Brushes.Text.Primary") is { } axis)
            plot.Axes.Color(ScottPlot.Color.FromHex(axis));
        if (Hex("Vestigium.Brushes.Stroke.Subtle") is { } grid)
            plot.Grid.MajorLineColor = ScottPlot.Color.FromHex(grid);
    }

    private static string? Hex(string resourceKey)
    {
        if (Application.Current?.TryFindResource(resourceKey) is not SolidColorBrush brush)
            return null;
        var c = brush.Color;
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    private void ThemeTrap_Changed(object sender, RoutedEventArgs e)
    {
        var resources = Application.Current.Resources;
        resources.Remove(typeof(MenuItem));
        if (ThemeTrap.IsChecked != true)
            return;

        var style = new Style(typeof(MenuItem));
        style.Setters.Add(new Setter(WpfControl.ForegroundProperty, Brushes.White));
        style.Setters.Add(new Setter(WpfControl.BackgroundProperty, Brushes.White));
        resources[typeof(MenuItem)] = style;
    }
}
