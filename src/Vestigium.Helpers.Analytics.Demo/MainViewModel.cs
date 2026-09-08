using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Helpers.Gallery;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public const int SampleSize = 1000;
    public const int PopulationSize = 100_000;

    public MainViewModel()
    {
        Summary = [];
        Bands = [];
        Intervals = [];
        Histogram = [];
        Sample = [];
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Analytics}";
        DrawSample();
    }

    public string Identity => AnalyticsHelper.Identity;
    public string StartupSnippet =>
        "var series = NumericSeries.FromObservations(observations, \"crypto-1k\");\n" +
        "var ci = series.Confidence(0.95);\n" +
        "var limits = series.ControlLimits(ControlLimitMethod.MovingRange);\n" +
        "panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.FiveNumber));\n" +
        "panel.Children.Add(ChartView.Control(series, limits));";

    public ObservableCollection<KvRow> Summary { get; }
    public ObservableCollection<BandRow> Bands { get; }
    public ObservableCollection<IntervalRow> Intervals { get; }
    public ObservableCollection<HistBar> Histogram { get; }
    public ObservableCollection<SamplePoint> Sample { get; }

    [ObservableProperty] private string name = "crypto-1k";
    [ObservableProperty] private int count;
    [ObservableProperty] private string fiveNumber = "";
    [ObservableProperty] private string meanText = "";
    [ObservableProperty] private string p95Text = "";
    [ObservableProperty] private string meanCiText = "";
    [ObservableProperty] private string skewText = "";
    [ObservableProperty] private string inferenceText = "";
    [ObservableProperty] private string limitsCaption = "";
    [ObservableProperty] private NumericSeries? series;
    [ObservableProperty] private FrameworkElement? histogramPlot;
    [ObservableProperty] private FrameworkElement? histogramBellPlot;
    [ObservableProperty] private FrameworkElement? paretoPlot;
    [ObservableProperty] private FrameworkElement? ecdfPlot;
    [ObservableProperty] private FrameworkElement? linePlot;
    [ObservableProperty] private FrameworkElement? boxPlot;
    [ObservableProperty] private FrameworkElement? controlSigmaPlot;
    [ObservableProperty] private FrameworkElement? controlMovingPlot;

    [RelayCommand]
    private void DrawSample()
    {
        var origin = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);
        var observations = DrawUnique(SampleSize, PopulationSize, origin);
        var snap = NumericSeries.FromObservations(observations, "crypto-1k");
        Series = snap;
        Bind(snap);
        HelperLog.Information(
            HelperLog.AppIds.Analytics,
            VestigiumStatus.Success,
            HelperLog.AppIds.Analytics,
            $"n={snap.Count} unique={snap.Full.Frequency.DistinctCount} mean={snap.Full.Mean:F2} P50={snap.Full.Median} P95={snap.Full.Percentile(0.95)} Identity={Identity}");
        StatusText = $"Drew {snap.Count:N0} unique integers from 1..{PopulationSize:N0}";
        RefreshLines();
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = AnalyticsHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    private void Bind(NumericSeries series)
    {
        var full = series.Full;
        var ci95 = series.Confidence(0.95);
        var hypothesized = (PopulationSize + 1) / 2d;
        var pValue = series.MeanPValue(hypothesized);

        Name = series.Name ?? "series";
        Count = series.Count;
        FiveNumber = $"{FmtDec(full.Min)}  /  {FmtDec(full.Q1)}  /  {FmtDec(full.Median)}  /  {FmtDec(full.Q3)}  /  {FmtDec(full.Max)}";
        MeanText = FmtNum(full.Mean);
        P95Text = FmtDec(full.Percentile(0.95));
        MeanCiText = ci95.Mean.IsDefined
            ? $"[{FmtNum(ci95.Mean.Lower)}, {FmtNum(ci95.Mean.Upper)}]"
            : "undefined";
        SkewText = FmtNum(full.Skewness);
        InferenceText = $"H0 mean = {hypothesized:N0}  ·  p = {FmtNum(pValue)}  ·  just-covering γ = {FmtNum(series.MeanConfidenceLevelContaining(hypothesized))}";

        Summary.Clear();
        void Add(string k, object? v) => Summary.Add(new KvRow { Key = k, Value = Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" });
        Add("Name", series.Name);
        Add("n", series.Count);
        Add("Min", FmtDec(full.Min));
        Add("Q1", FmtDec(full.Q1));
        Add("Median", FmtDec(full.Median));
        Add("Q3", FmtDec(full.Q3));
        Add("Max", FmtDec(full.Max));
        Add("Range", FmtDec(full.Range));
        Add("IQR", FmtDec(full.Iqr));
        Add("Mean", FmtNum(full.Mean));
        Add("StdDev", FmtNum(full.StdDev));
        Add("SEM", FmtNum(full.StandardErrorOfMean));
        Add("CV", FmtNum(full.CoefficientOfVariation));
        Add("Skewness", FmtNum(full.Skewness));
        Add("ExcessKurtosis", FmtNum(full.ExcessKurtosis));
        Add("P90", FmtDec(full.Percentile(0.90)));
        Add("P95", FmtDec(full.Percentile(0.95)));
        Add("P99", FmtDec(full.Percentile(0.99)));
        Add("LowOutliers", full.LowOutliers.Count);
        Add("HighOutliers", full.HighOutliers.Count);
        Add("GeometricMean", FmtNum(full.GeometricMean));
        Add("HarmonicMean", FmtNum(full.HarmonicMean));

        Bands.Clear();
        foreach (var b in series.Bands)
        {
            Bands.Add(new BandRow
            {
                Band = b.Kind.ToString(),
                N = b.Count,
                Min = FmtDec(b.Min),
                P50 = FmtDec(b.Median),
                Max = FmtDec(b.Max),
                Mean = FmtNum(b.Mean),
                StdDev = FmtNum(b.StdDev),
                Skew = FmtNum(b.Skewness),
                ExKurt = FmtNum(b.ExcessKurtosis)
            });
        }

        Intervals.Clear();
        void AddReport(string label, ConfidenceReport report)
        {
            AddIv(label, report.Mean);
            AddIv(label, report.Median);
            AddIv(label, report.Variance);
            AddIv(label, report.StdDev);
        }
        AddReport("90%", series.Confidence(0.90));
        AddReport("95%", ci95);
        AddReport("99%", series.Confidence(0.99));
        AddReport("95% FPC", series.Confidence(0.95, PopulationSize));
        void AddIv(string level, ConfidenceInterval iv)
        {
            Intervals.Add(new IntervalRow
            {
                Level = level,
                Parameter = iv.Parameter,
                Estimate = FmtNum(iv.Estimate),
                Lower = FmtNum(iv.Lower),
                Upper = FmtNum(iv.Upper),
                Width = FmtNum(iv.Width),
                Method = iv.Method,
                Defined = iv.IsDefined ? "yes" : "no"
            });
        }

        Histogram.Clear();
        foreach (var bin in full.Frequency.Histogram)
        {
            Histogram.Add(new HistBar
            {
                Label = FmtDec(bin.LowerInclusive),
                Count = bin.Count,
                Height = 8,
                Caption = $"{bin.LowerInclusive:G4}–{bin.UpperInclusive:G4}: {bin.Count}"
            });
        }

        Sample.Clear();
        var take = Math.Min(80, series.Count);
        for (var s = 0; s < take; s++)
        {
            object? at = series.HasTimestamps && s < series.Times.Count ? series.Times[s] : null;
            Sample.Add(new SamplePoint
            {
                Index = s,
                Value = series.Values[s].ToString(CultureInfo.InvariantCulture),
                Timestamp = at is DateTimeOffset dto ? dto.ToString("HH:mm:ss") : ""
            });
        }

        BindCharts(series);
    }

    private void BindCharts(NumericSeries series)
    {
        var sigma = series.ControlLimits();
        var moving = series.ControlLimits(ControlLimitMethod.MovingRange);
        LimitsCaption = $"mean±3s outside={sigma.OutOfControlCount}  ·  MR outside={moving.OutOfControlCount}  ·  five-number {FiveNumber}";

        HistogramPlot = ChartView.Histogram(series, new ChartOptions { Title = "Histogram (Freedman–Diaconis)" });
        HistogramBellPlot = ChartView.Histogram(series, showBellCurve: true, new ChartOptions { Title = "Histogram + N(μ, s)" });
        ParetoPlot = ChartView.Pareto(series, new ChartOptions { Title = "Pareto" });
        EcdfPlot = ChartView.Ecdf(series, new ChartOptions { Title = "ECDF" });
        LinePlot = ChartView.Line(series, TrendKind.Linear, new ChartOptions { Title = "Sample order + trend" });
        BoxPlot = ChartView.Box(series, BoxWhiskerKind.FiveNumber, new ChartOptions { Title = "Five-number box (min / Q1 / median / Q3 / max)" });
        ControlSigmaPlot = ChartView.Control(series, sigma, new ChartOptions { Title = "Control · mean ± 3s" });
        ControlMovingPlot = ChartView.Control(series, moving, new ChartOptions { Title = "Control · moving range" });
    }

    private static List<Observation> DrawUnique(int count, int populationSize, DateTimeOffset origin)
    {
        var drawn = new HashSet<int>(count);
        while (drawn.Count < count)
            drawn.Add(RandomNumberGenerator.GetInt32(1, populationSize + 1));
        var values = drawn.ToArray();
        var list = new List<Observation>(count);
        for (var i = 0; i < values.Length; i++)
            list.Add(new Observation(values[i], origin.AddSeconds(i)));
        return list;
    }

    private static string FmtDec(decimal? value) =>
        value is { } v ? v.ToString("N4", CultureInfo.InvariantCulture) : "—";

    private static string FmtNum(double? value) =>
        value is { } v ? v.ToString("N4", CultureInfo.InvariantCulture) : "—";
}
