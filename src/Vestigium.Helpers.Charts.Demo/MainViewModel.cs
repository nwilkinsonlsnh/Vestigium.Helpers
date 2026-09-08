using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Helpers.Gallery;
using Vestigium.Logging;

namespace Vestigium.Helpers.Charts.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public MainViewModel()
    {
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Charts}";
        DrawSample();
    }

    public string Identity => ChartHelper.Identity;
    public string StartupSnippet =>
        "var limits = series.ControlLimits(ControlLimitMethod.MovingRange);\n" +
        "panel.Children.Add(ChartView.Control(series, limits));";

    [ObservableProperty] private int count;
    [ObservableProperty] private string limitsCaption = "";
    [ObservableProperty] private string movingCaption = "";
    [ObservableProperty] private string spikeCaption = "";
    [ObservableProperty] private FrameworkElement? histogramPlot;
    [ObservableProperty] private FrameworkElement? bellPlot;
    [ObservableProperty] private FrameworkElement? controlSigmaPlot;
    [ObservableProperty] private FrameworkElement? controlMovingPlot;
    [ObservableProperty] private FrameworkElement? controlSpikeSigmaPlot;
    [ObservableProperty] private FrameworkElement? controlSpikePlot;
    [ObservableProperty] private FrameworkElement? paretoPlot;
    [ObservableProperty] private FrameworkElement? ecdfPlot;
    [ObservableProperty] private FrameworkElement? linePlot;
    [ObservableProperty] private FrameworkElement? piePlot;
    [ObservableProperty] private FrameworkElement? boxPlot;

    [RelayCommand]
    private void DrawSample()
    {
        var series = NumericSeries.From(new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 14.2, 12.1 }, "rtt-ms");
        var spiked = NumericSeries.From(new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 14.2, 40.2 }, "rtt-spike");
        Count = series.Count;
        var sigma = series.ControlLimits();
        var moving = series.ControlLimits(ControlLimitMethod.MovingRange);
        var spikeSigma = spiked.ControlLimits();
        var spikeMr = spiked.ControlLimits(ControlLimitMethod.MovingRange);
        LimitsCaption = $"mean±3s  CL={sigma.Center:F2}  UCL={sigma.Upper:F2}  LCL={sigma.Lower:F2}  outside={sigma.OutOfControlCount}";
        MovingCaption = $"MR  CL={moving.Center:F2}  UCL={moving.Upper:F2}  LCL={moving.Lower:F2}  MR̄={moving.MovingRangeBar:F2}  outside={moving.OutOfControlCount}";
        SpikeCaption = $"40.2 ms spike · mean±3s outside={spikeSigma.OutOfControlCount} (s inflated) · MR outside={spikeMr.OutOfControlCount}";

        HistogramPlot = ChartView.Histogram(series, new ChartOptions { Title = "Histogram" });
        BellPlot = ChartView.Histogram(series, showBellCurve: true, new ChartOptions { Title = "Histogram + N(μ, s)" });
        ControlSigmaPlot = ChartView.Control(series, sigma);
        ControlMovingPlot = ChartView.Control(series, moving, new ChartOptions { Title = "Moving range UCL/LCL" });
        ControlSpikeSigmaPlot = ChartView.Control(spiked, spikeSigma, new ChartOptions { Title = "Spike · mean ± 3s (often swallows it)" });
        ControlSpikePlot = ChartView.Control(spiked, spikeMr, new ChartOptions { Title = "Spike · moving-range fences" });
        ParetoPlot = ChartView.Pareto(series, new ChartOptions { Title = "Pareto (histogram bins)" });
        EcdfPlot = ChartView.Ecdf(series, new ChartOptions { Title = "ECDF" });
        LinePlot = ChartView.Line(series, TrendKind.Linear, new ChartOptions { Title = "Sample + linear trend" });
        PiePlot = ChartView.Pie(series, new ChartOptions { Title = "Frequencies" });
        BoxPlot = ChartView.Box(series, new ChartOptions { Title = "Tukey box" });

        HelperLog.Information(
            HelperLog.AppIds.Charts,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Chart,
            $"gallery drew n={series.Count} series={series.SeriesId}");
        StatusText = $"Drew {series.Count} RTT samples · {LimitsCaption}";
        RefreshLines();
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = ChartHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }
}
