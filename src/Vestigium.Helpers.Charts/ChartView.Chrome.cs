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
        var open = view.GetType().GetMethod("OpenInNewWindow", Type.EmptyTypes);
        if (open is not null)
        {
            open.Invoke(view, null);
            return;
        }

        new Window
        {
            Title = "Chart",
            Width = Math.Max(900, view.ActualWidth + 80),
            Height = Math.Max(560, view.ActualHeight + 80),
            Background = Brushes.White,
            Content = new Image { Source = Capture(view), Stretch = Stretch.Uniform, Margin = new Thickness(8) }
        }.Show();
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
