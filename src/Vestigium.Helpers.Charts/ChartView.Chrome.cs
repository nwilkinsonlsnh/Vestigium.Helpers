using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScottPlot.WPF;

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
        TrySet(view, "MenuOnRightClick", false);

        if (options.Width is { } w)
            view.Width = w;

        if (options.Height is { } h)
        {
            view.Height = h;
        }
        else if (options.Stretch)
        {
            view.Height = double.NaN;
            view.MinHeight = 140;
            view.VerticalAlignment = VerticalAlignment.Stretch;
            view.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            view.Height = 240;
        }

        if (spec.Kind == ChartKind.Control)
            view.Plot.Axes.Margins(0.05, 0.22);

        if (options.HostMenu)
            view.ContextMenu = BuildMenu(view);

        view.Refresh();
    }

    private static ContextMenu BuildMenu(WpfPlot view)
    {
        var menu = new ContextMenu
        {
            Background = Brushes.White,
            Foreground = Brushes.Black,
            BorderBrush = Brushes.Silver
        };

        menu.Items.Add(Item("Save Image", () => SaveImage(view)));
        menu.Items.Add(Item("Copy to Clipboard", () => Clipboard.SetImage(Capture(view))));
        menu.Items.Add(Item("Auto Scale", () =>
        {
            view.Plot.Axes.AutoScale();
            view.Refresh();
        }));
        menu.Items.Add(Item("Open in New Window", () => OpenWindow(view)));
        menu.Items.Add(new Separator());

        var legend = new MenuItem
        {
            Header = "Show Legend",
            IsCheckable = true,
            IsChecked = view.Plot.Legend.IsVisible,
            Background = Brushes.White,
            Foreground = Brushes.Black
        };
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

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem
        {
            Header = header,
            Background = Brushes.White,
            Foreground = Brushes.Black
        };
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

    private static void TrySet(object target, string name, object value)
    {
        try
        {
            target.GetType().GetProperty(name)?.SetValue(target, value);
        }
        catch (Exception)
        {
        }
    }
}
