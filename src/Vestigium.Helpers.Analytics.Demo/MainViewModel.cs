using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Gallery;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public const int SampleSize = 1000;
    public const int PopulationSize = 100_000;
    private const double PlotWidth = 720;
    private const double PlotHeight = 240;
    private const double PlotPad = 12;

    public MainViewModel()
    {
        Summary = [];
        Bands = [];
        Intervals = [];
        Histogram = [];
        ParetoBars = [];
        Sample = [];
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Analytics}";
        DrawSample();
    }

    public string Identity => AnalyticsHelper.Identity;
    public string StartupSnippet =>
        "var series = NumericSeries.FromObservations(observations, \"crypto-1k\");\n" +
        "var ci = series.Confidence(0.95);\n" +
        "decimal p95 = series.Full.Percentile(0.95);  // tail cut — not a confidence level";

    public ObservableCollection<KvRow> Summary { get; }
    public ObservableCollection<BandRow> Bands { get; }
    public ObservableCollection<IntervalRow> Intervals { get; }
    public ObservableCollection<HistBar> Histogram { get; }
    public ObservableCollection<ParetoBar> ParetoBars { get; }
    public ObservableCollection<SamplePoint> Sample { get; }

    [ObservableProperty] private string name = "crypto-1k";
    [ObservableProperty] private int count;
    [ObservableProperty] private string fiveNumber = "";
    [ObservableProperty] private string meanText = "";
    [ObservableProperty] private string p95Text = "";
    [ObservableProperty] private string meanCiText = "";
    [ObservableProperty] private string skewText = "";
    [ObservableProperty] private string inferenceText = "";
    [ObservableProperty] private NumericSeries? series;
    [ObservableProperty] private PointCollection ecdfCurve = [];
    [ObservableProperty] private PointCollection sampleCurve = [];
    [ObservableProperty] private double p95LineX;
    [ObservableProperty] private string ecdfMinLabel = "";
    [ObservableProperty] private string ecdfMaxLabel = "";
    [ObservableProperty] private string ecdfP95Label = "";
    [ObservableProperty] private PointCollection histogramTrendCurve = [];
    [ObservableProperty] private PointCollection paretoCurve = [];
    [ObservableProperty] private double paretoEightyY = 36;
    [ObservableProperty] private string histogramTrendCaption = "";
    [ObservableProperty] private string paretoCaption = "";

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
        var hist = full.Frequency.Histogram;
        var trend = series.HistogramTrendPoints();
        var max = hist.Count == 0 ? 1d : Math.Max(1, hist.Max(h => h.Count));
        if (trend.Count > 0)
            max = Math.Max(max, trend.Max(p => p.Y));
        var i = 0;
        foreach (var bin in hist)
        {
            Histogram.Add(new HistBar
            {
                Label = FmtDec(bin.LowerInclusive),
                Count = bin.Count,
                Height = 8 + bin.Count / max * 140,
                Caption = $"{i}: {bin.Count}"
            });
            i++;
        }

        HistogramTrendCurve = MapBarCanvas(trend, max);
        if (trend.Count >= 2)
        {
            var dx = trend[^1].X - trend[0].X;
            var slope = dx == 0 ? 0 : (trend[^1].Y - trend[0].Y) / dx;
            HistogramTrendCaption = $"OLS trend  slope = {slope:N4} counts per unit";
        }
        else
            HistogramTrendCaption = "";

        ParetoBars.Clear();
        var pareto = series.ParetoPoints();
        var pMax = pareto.Count == 0 ? 1d : Math.Max(1, pareto.Max(b => b.Count));
        foreach (var bin in pareto)
        {
            ParetoBars.Add(new ParetoBar
            {
                Label = $"#{bin.Rank}",
                Count = bin.Count,
                Height = 8 + bin.Count / pMax * 140,
                Share = $"{bin.CumulativeShare:P0}",
                Caption = $"rank {bin.Rank}  mid {bin.Midpoint:N0}  n={bin.Count}  cum {bin.CumulativeShare:P1}"
            });
        }

        ParetoCurve = MapShareCanvas(pareto.Select(b => new ChartPoint(b.Rank, b.CumulativeShare)).ToArray());
        ParetoEightyY = 180 - 0.8 * 160;
        var k80 = pareto.FirstOrDefault(b => b.CumulativeShare >= 0.8);
        ParetoCaption = k80.Rank == 0
            ? ""
            : $"80% of the sample sits in the first {k80.Rank} of {pareto.Count} bins ({100.0 * k80.Rank / Math.Max(1, pareto.Count):0}%). Classic 80/20 would be ~20% of the bins.";

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

        BindCurves(series);
    }

    private void BindCurves(NumericSeries series)
    {
        var ecdf = series.EcdfPoints();
        EcdfCurve = MapCurve(ecdf);
        SampleCurve = MapCurve(series.SampleOrderPoints());
        if (ecdf.Count == 0)
        {
            P95LineX = PlotPad;
            EcdfMinLabel = "";
            EcdfMaxLabel = "";
            EcdfP95Label = "";
            return;
        }

        var minX = ecdf[0].X;
        var maxX = ecdf[^1].X;
        var span = maxX - minX;
        if (span == 0)
            span = 1;
        var p95 = (double)series.Full.Percentile(0.95);
        P95LineX = PlotPad + (p95 - minX) / span * (PlotWidth - 2 * PlotPad);
        EcdfMinLabel = FmtNum(minX);
        EcdfMaxLabel = FmtNum(maxX);
        EcdfP95Label = $"P95 = {FmtDec(series.Full.Percentile(0.95))}";
    }

    private static PointCollection MapCurve(IReadOnlyList<ChartPoint> source)
    {
        var pts = new PointCollection();
        if (source.Count == 0)
            return pts;

        var minX = source[0].X;
        var maxX = source[0].X;
        var minY = source[0].Y;
        var maxY = source[0].Y;
        foreach (var p in source)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        var dx = maxX - minX;
        if (dx == 0) dx = 1;
        var dy = maxY - minY;
        if (dy == 0) dy = 1;
        var innerW = PlotWidth - 2 * PlotPad;
        var innerH = PlotHeight - 2 * PlotPad;
        foreach (var p in Downsample(source, 240))
        {
            var x = PlotPad + (p.X - minX) / dx * innerW;
            var y = PlotPad + (1 - (p.Y - minY) / dy) * innerH;
            pts.Add(new Point(x, y));
        }

        return pts;
    }

    private static List<ChartPoint> Downsample(IReadOnlyList<ChartPoint> source, int max)
    {
        if (source.Count <= max)
            return [.. source];
        var list = new List<ChartPoint>(max);
        var step = (source.Count - 1) / (double)(max - 1);
        for (var i = 0; i < max; i++)
        {
            var idx = (int)Math.Round(i * step);
            if (idx >= source.Count)
                idx = source.Count - 1;
            list.Add(source[idx]);
        }

        return list;
    }


    private static PointCollection MapBarCanvas(IReadOnlyList<ChartPoint> source, double maxY, double width = 720, double height = 180)
    {
        var pts = new PointCollection();
        if (source.Count == 0)
            return pts;
        if (maxY <= 0)
            maxY = 1;
        var n = source.Count;
        for (var i = 0; i < n; i++)
        {
            var x = (i + 0.5) / n * width;
            var y = height - (8 + source[i].Y / maxY * 140);
            pts.Add(new Point(x, Math.Clamp(y, 2, height - 2)));
        }
        return pts;
    }

    private static PointCollection MapShareCanvas(IReadOnlyList<ChartPoint> source, double width = 720, double height = 180)
    {
        var pts = new PointCollection();
        if (source.Count == 0)
            return pts;
        var n = source.Count;
        for (var i = 0; i < n; i++)
        {
            var x = (i + 0.5) / n * width;
            var y = height - source[i].Y * 160;
            pts.Add(new Point(x, Math.Clamp(y, 2, height - 2)));
        }
        return pts;
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
