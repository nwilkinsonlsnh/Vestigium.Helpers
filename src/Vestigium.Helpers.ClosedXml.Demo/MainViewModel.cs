using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Gallery;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public const int SampleSize = 1000;
    public const int PopulationSize = 100_000;

    public MainViewModel()
    {
        Histogram = [];
        Bands = [];
        Intervals = [];
        Sample = [];
        Injection = [];
        TableStyles = WorkbookHelper.TableStyles;
        TableStyle = ExcelTableStyles.DefaultId;
        IncludeCharts = true;
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.ClosedXml}";
        DrawSample();
        SeedInjection();
    }

    public string Identity => WorkbookHelper.Identity;
    public string StartupSnippet =>
        "using var book = WorkbookHelper.Create(\"Summary\", HelperLog.AppIds.ClosedXml);\n" +
        "book.TableStyle = \"Medium2\";\n" +
        "WorkbookHelper.WriteSeries(book, series, populationSize: 100_000);\n" +
        "var path = book.Save();  // Desktop\\Vestigium\\Exports\\ClosedXml\\";
    public IReadOnlyList<string> TableStyles { get; }

    public ObservableCollection<HistBar> Histogram { get; }
    public ObservableCollection<BandRow> Bands { get; }
    public ObservableCollection<IntervalRow> Intervals { get; }
    public ObservableCollection<SamplePoint> Sample { get; }
    public ObservableCollection<KvRow> Injection { get; }

    [ObservableProperty] private string tableStyle = ExcelTableStyles.DefaultId;
    [ObservableProperty] private bool includeCharts = true;
    [ObservableProperty] private string exportPath = "";
    [ObservableProperty] private string sheetList = "Summary, Charts, Bands, Confidence, Histogram, Sample";
    [ObservableProperty] private int chartCount;
    [ObservableProperty] private int count;
    [ObservableProperty] private string meanText = "";
    [ObservableProperty] private string p95Text = "";
    [ObservableProperty] private NumericSeries? series;

    public string ChartCaption => IncludeCharts ? $"{ChartCount} Excel chart(s)" : "charts off";
    public string ExcelStyleName => ExcelTableStyles.ToExcelName(TableStyle);
    public string ExportFolder => WorkbookHelper.DefaultExportDirectory(HelperLog.AppIds.ClosedXml);

    partial void OnTableStyleChanged(string value) => OnPropertyChanged(nameof(ExcelStyleName));
    partial void OnIncludeChartsChanged(bool value) => OnPropertyChanged(nameof(ChartCaption));
    partial void OnChartCountChanged(int value) => OnPropertyChanged(nameof(ChartCaption));

    [RelayCommand]
    private void DrawSample()
    {
        var origin = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);
        var observations = DrawUnique(SampleSize, PopulationSize, origin);
        Series = NumericSeries.FromObservations(observations, "crypto-1k");
        Bind(Series);
        StatusText = $"Drew {Series.Count:N0} unique integers from 1..{PopulationSize:N0}";
    }

    [RelayCommand]
    private void WriteWorkbook()
    {
        if (Series is null)
            DrawSample();
        if (Series is null)
            return;
        using var book = WorkbookHelper.Create("Summary", HelperLog.AppIds.ClosedXml);
        book.TableStyle = TableStyle;
        book.IncludeCharts = IncludeCharts;
        WorkbookHelper.WriteSeries(book, Series, populationSize: PopulationSize);
        ExportPath = book.Save();
        SheetList = string.Join(", ", book.SheetNames);
        ChartCount = book.Charts.Count;
        StatusText = $"Wrote {ExportPath} · sheets={book.SheetNames.Count} · charts={ChartCount}";
        RefreshLines();
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        var dir = string.IsNullOrWhiteSpace(ExportPath)
            ? ExportFolder
            : Path.GetDirectoryName(ExportPath) ?? ExportFolder;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        StatusText = $"Opened {dir}";
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = WorkbookHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    private void Bind(NumericSeries series)
    {
        Count = series.Count;
        MeanText = FmtNum(series.Full.Mean);
        P95Text = FmtDec(series.Full.Percentile(0.95));

        Histogram.Clear();
        var hist = series.Full.Frequency.Histogram;
        var max = hist.Count == 0 ? 1d : Math.Max(1, hist.Max(h => h.Count));
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

        Bands.Clear();
        foreach (var b in series.Bands)
        {
            Bands.Add(new BandRow
            {
                Band = b.Kind.ToString(),
                N = b.Count,
                Mean = FmtNum(b.Mean),
                P50 = FmtDec(b.Median),
                Min = FmtDec(b.Min),
                Max = FmtDec(b.Max)
            });
        }

        Intervals.Clear();
        foreach (var (label, report) in new (string, ConfidenceReport)[]
                 {
                     ("90%", series.Confidence(0.90)),
                     ("95%", series.Confidence(0.95)),
                     ("99%", series.Confidence(0.99)),
                     ("95% FPC", series.Confidence(0.95, PopulationSize))
                 })
        {
            var iv = report.Mean;
            Intervals.Add(new IntervalRow
            {
                Level = label,
                Parameter = iv.Parameter,
                Estimate = FmtNum(iv.Estimate),
                Lower = FmtNum(iv.Lower),
                Upper = FmtNum(iv.Upper),
                Method = iv.Method,
                Defined = iv.IsDefined ? "yes" : "no"
            });
        }

        Sample.Clear();
        var take = Math.Min(80, series.Count);
        for (var s = 0; s < take; s++)
        {
            Sample.Add(new SamplePoint
            {
                Index = s,
                Value = series.Values[s].ToString(CultureInfo.InvariantCulture)
            });
        }
    }

    private void SeedInjection()
    {
        Injection.Clear();
        string[] payloads = ["=1+1", "+2", "-SUM(A1)", "@cmd", "plain"];
        foreach (var p in payloads)
        {
            var stored = p.Length > 0 && p[0] is '=' or '+' or '-' or '@' ? "'" + p : p;
            Injection.Add(new KvRow { Key = p, Value = stored });
        }
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
