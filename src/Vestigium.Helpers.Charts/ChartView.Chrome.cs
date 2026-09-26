using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScottPlot.WPF;
using SpColor = ScottPlot.Color;
using WpfControl = System.Windows.Controls.Control;

namespace Vestigium.Helpers.Charts;

public static partial class ChartView
{
    public static event Action<FrameworkElement, bool>? LegendToggled;

    public static void SetLegendVisible(FrameworkElement view, bool visible)
    {
        if (view is not WpfPlot plot)
            return;
        plot.Plot.Legend.IsVisible = visible;
        plot.Refresh();
    }

    public static bool GetLegendVisible(FrameworkElement view)
        => view is WpfPlot plot && plot.Plot.Legend.IsVisible;

    internal static void AttachChrome(WpfPlot view, ChartSpec spec)
    {
        var options = spec.Options ?? new ChartOptions();
        ApplyColors(view.Plot, options);

        if (options.Width is { } w)
            view.Width = w;

        if (options.Height is { } h)
        {
            view.Height = h;
        }
        else if (options.Stretch)
        {
            view.ClearValue(FrameworkElement.HeightProperty);
            view.MinHeight = 220;
            view.VerticalAlignment = VerticalAlignment.Stretch;
            view.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            view.Height = 240;
        }

        if (spec.Kind == ChartKind.Control)
        {
            try
            {
                view.Plot.Axes.Margins(0.05, 0.22);
            }
            catch (Exception)
            {
            }
        }

        view.Menu = options.HostMenu ? new ChartPlotMenu(view) : null;
        view.ContextMenu = null;
        view.Resources["Vestigium.ChartSpec"] = spec;
        view.Loaded += (_, _) => view.Refresh();
        view.SizeChanged += (_, _) =>
        {
            if (view.ActualWidth > 1 && view.ActualHeight > 1)
                view.Refresh();
        };
        view.Refresh();
    }

    internal static ContextMenu CreateHostMenu(WpfPlot view)
    {
        var menu = new ContextMenu();
        PaintMenu(menu);
        menu.Items.Add(Item("Save Image", "\uE74E", () => SaveImage(view)));
        menu.Items.Add(Item("Copy to Clipboard", "\uE8C8", () => Clipboard.SetImage(Capture(view))));
        menu.Items.Add(Item("Auto Scale", "\uE9A6", () =>
        {
            view.Plot.Axes.AutoScale();
            view.Refresh();
        }));
        menu.Items.Add(Item("Open in New Window", "\uE8A7", () => OpenWindow(view)));
        menu.Items.Add(new Separator());

        var legend = new MenuItem
        {
            Header = "Show Legend",
            Icon = Glyph("\uE81E"),
            IsCheckable = true,
            IsChecked = view.Plot.Legend.IsVisible
        };
        PaintItem(legend);
        legend.Click += (_, _) =>
        {
            view.Plot.Legend.IsVisible = legend.IsChecked;
            view.Refresh();
            LegendToggled?.Invoke(view, legend.IsChecked);
        };
        menu.Items.Add(legend);
        menu.Opened += (_, _) => legend.IsChecked = view.Plot.Legend.IsVisible;
        return menu;
    }

    private static void PaintMenu(ContextMenu menu)
    {
        menu.Background = Brushes.White;
        menu.Foreground = Brushes.Black;
        menu.BorderBrush = Brushes.Silver;
        var style = new Style(typeof(MenuItem));
        style.Setters.Add(new Setter(WpfControl.ForegroundProperty, Brushes.Black));
        style.Setters.Add(new Setter(WpfControl.BackgroundProperty, Brushes.White));
        menu.Resources[typeof(MenuItem)] = style;
    }

    private static void PaintItem(MenuItem item)
    {
        item.Foreground = Brushes.Black;
        item.Background = Brushes.White;
    }

    private static FrameworkElement Glyph(string symbol)
    {
        return new TextBlock
        {
            Text = symbol,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = Brushes.Black,
            Width = 16,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static void ApplyColors(ScottPlot.Plot plot, ChartOptions options)
    {
        if (TryHex(options.FigureColor, out var figure))
            plot.FigureBackground.Color = figure;
        if (TryHex(options.DataColor, out var data))
            plot.DataBackground.Color = data;
        if (TryHex(options.AxisColor, out var axis))
            plot.Axes.Color(axis);
        if (TryHex(options.GridColor, out var grid))
            plot.Grid.MajorLineColor = grid;
    }

    private static bool TryHex(string? value, out SpColor color)
    {
        color = SpColor.FromHex("#FFFFFF");
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            color = SpColor.FromHex(value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static MenuItem Item(string header, string glyph, Action action)
    {
        var item = new MenuItem
        {
            Header = header,
            Icon = Glyph(glyph)
        };
        PaintItem(item);
        item.Click += (_, _) => action();
        return item;
    }

    private static void SaveImage(WpfPlot view)
    {
        var dialog = new SaveFileDialog { Filter = "PNG image|*.png", FileName = "chart.png" };
        if (dialog.ShowDialog() != true)
            return;
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Capture(view)));
        using var stream = File.Create(dialog.FileName);
        encoder.Save(stream);
    }

    private static void OpenWindow(WpfPlot view)
    {
        var spec = view.Resources["Vestigium.ChartSpec"] as ChartSpec;
        var title = spec?.Options?.Title ?? spec?.Title ?? PlotTitle(view) ?? "Chart";
        var options = spec?.Options ?? new ChartOptions();
        var content = spec is null
            ? SnapshotView(view)
            : Host(ForWindow(spec));

        if (content is WpfPlot plot)
        {
            plot.ClearValue(FrameworkElement.WidthProperty);
            plot.ClearValue(FrameworkElement.HeightProperty);
            plot.MinHeight = 0;
            plot.HorizontalAlignment = HorizontalAlignment.Stretch;
            plot.VerticalAlignment = VerticalAlignment.Stretch;
        }

        var window = new Window
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Maximized,
            Content = content
        };
        PaintWindow(window, options);
        window.Show();
    }

    private static ChartSpec ForWindow(ChartSpec spec)
    {
        var options = spec.Options ?? new ChartOptions();
        return new ChartSpec
        {
            Kind = spec.Kind,
            Title = spec.Title,
            Series = spec.Series,
            Options = options with { Width = null, Height = null, Stretch = true },
            Limits = spec.Limits,
            Source = spec.Source,
            Slices = spec.Slices,
            RunRules = spec.RunRules,
            Spec = spec.Spec
        };
    }

    private static void PaintWindow(Window window, ChartOptions options)
    {
        if (Application.Current?.TryFindResource("Vestigium.Brushes.Surface.Window") is Brush theme)
        {
            window.SetResourceReference(WpfControl.BackgroundProperty, "Vestigium.Brushes.Surface.Window");
            if (Application.Current.TryFindResource("Vestigium.Brushes.Text.Primary") is Brush)
                window.SetResourceReference(WpfControl.ForegroundProperty, "Vestigium.Brushes.Text.Primary");
            return;
        }

        if (TryWpfBrush(options.FigureColor, out var figure))
            window.Background = figure;
    }

    private static bool TryWpfBrush(string? value, out SolidColorBrush brush)
    {
        brush = Brushes.Transparent;
        if (!TryHex(value, out var color))
            return false;
        brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        brush.Freeze();
        return true;
    }

    private static string? PlotTitle(WpfPlot view)
    {
        try
        {
            var text = view.Plot.Axes.Title.Label.Text;
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static FrameworkElement SnapshotView(WpfPlot view)
    {
        return new Image
        {
            Source = Capture(view),
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private static BitmapSource Capture(FrameworkElement view)
    {
        var width = Math.Max(1, (int)Math.Ceiling(view.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(view.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        return bitmap;
    }
}
