using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
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
    private readonly Dictionary<string, SheetTable> _readCache = new(StringComparer.OrdinalIgnoreCase);

    public MainViewModel()
    {
        Histogram = [];
        Bands = [];
        Intervals = [];
        Sample = [];
        Injection = [];
        Swatches = [];
        ReadSheets = [];
        ReadRows = [];
        ChromeFormats = [];
        SheetOrder = [];
        TableStyles = WorkbookHelper.TableStyles;
        StyleGroups = ["Light", "Medium", "Dark"];
        StyleGroup = "Medium";
        TableStyle = ExcelTableStyles.DefaultId;
        IncludeCharts = true;
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.ClosedXml}";
        RebuildSwatches();
        BindPreviewBrushes(ExcelTableStylePreview.Of(TableStyle));
        DrawSample();
        SeedInjection();
        SeedChrome();
        BindSheetOrder(["Summary", "Charts", "Bands", "Confidence", "Histogram", "Sample"]);
    }

    public string Identity => WorkbookHelper.Identity;
    public string StartupSnippet =>
        "using var book = WorkbookHelper.Create(\"Summary\", HelperLog.AppIds.ClosedXml);\n" +
        "book.TableStyle = \"Medium2\";\n" +
        "WorkbookHelper.WriteSeries(book, series, populationSize: 100_000);\n" +
        "var path = book.Save();  // Desktop\\Vestigium\\Exports\\ClosedXml\\";
    public IReadOnlyList<string> TableStyles { get; }
    public IReadOnlyList<string> StyleGroups { get; }

    public ObservableCollection<HistBar> Histogram { get; }
    public ObservableCollection<BandRow> Bands { get; }
    public ObservableCollection<IntervalRow> Intervals { get; }
    public ObservableCollection<SamplePoint> Sample { get; }
    public ObservableCollection<KvRow> Injection { get; }
    public ObservableCollection<TableStyleSwatch> Swatches { get; }
    public ObservableCollection<string> ReadSheets { get; }
    public ObservableCollection<ReadCellRow> ReadRows { get; }
    public ObservableCollection<KvRow> ChromeFormats { get; }
    public ObservableCollection<KvRow> SheetOrder { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExcelStyleName))]
    [NotifyPropertyChangedFor(nameof(SelectedStyleCaption))]
    private string tableStyle = ExcelTableStyles.DefaultId;

    [ObservableProperty] private string styleGroup = "Medium";
    [ObservableProperty] private bool includeCharts = true;
    [ObservableProperty] private string exportPath = "";
    [ObservableProperty] private string sheetList = "Summary, Charts, Bands, Confidence, Histogram, Sample";
    [ObservableProperty] private int chartCount;
    [ObservableProperty] private int count;
    [ObservableProperty] private string meanText = "";
    [ObservableProperty] private string p95Text = "";
    [ObservableProperty] private NumericSeries? series;
    [ObservableProperty] private string readSheet = "Summary";
    [ObservableProperty] private string readCaption = "Write a workbook, then Read used range.";
    [ObservableProperty] private string chromeFooter = "ClosedXml  ·  &D &T";
    [ObservableProperty] private string chromePrint = "Landscape · fit-to-width";
    [ObservableProperty] private Brush previewHeader = Brushes.Transparent;
    [ObservableProperty] private Brush previewHeaderInk = Brushes.White;
    [ObservableProperty] private Brush previewBand = Brushes.Transparent;
    [ObservableProperty] private Brush previewBandAlt = Brushes.Transparent;
    [ObservableProperty] private Brush previewBodyInk = Brushes.Black;

    public string ChartCaption => IncludeCharts ? $"{ChartCount} Excel chart(s)" : "charts off";
    public string ExcelStyleName => ExcelTableStyles.ToExcelName(TableStyle);
    public string SelectedStyleCaption
    {
        get
        {
            var preview = ExcelTableStylePreview.Of(TableStyle);
            var extra = TableStyle == ExcelTableStyles.DefaultId ? " · Excel default" : "";
            return $"{preview.Caption} · {preview.ExcelName}{extra}";
        }
    }
    public string ExportFolder => WorkbookHelper.DefaultExportDirectory(HelperLog.AppIds.ClosedXml);

    partial void OnTableStyleChanged(string value)
    {
        var preview = ExcelTableStylePreview.Of(value);
        BindPreviewBrushes(preview);
        if (!string.Equals(StyleGroup, preview.Group, StringComparison.OrdinalIgnoreCase))
            StyleGroup = preview.Group;
        else
            RebuildSwatches();
    }

    partial void OnStyleGroupChanged(string value) => RebuildSwatches();
    partial void OnIncludeChartsChanged(bool value) => OnPropertyChanged(nameof(ChartCaption));
    partial void OnChartCountChanged(int value) => OnPropertyChanged(nameof(ChartCaption));

    [RelayCommand]
    private void SelectGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return;
        StyleGroup = group;
    }

    [RelayCommand]
    private void SelectStyle(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;
        TableStyle = id;
        StatusText = $"Table style {SelectedStyleCaption}";
    }

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
        BindSheetOrder(book.SheetNames);
        StatusText = $"Wrote {ExportPath} · {SelectedStyleCaption} · sheets={book.SheetNames.Count} · charts={ChartCount}";
        RefreshLines();
    }

    [RelayCommand]
    private void ReadWorkbook()
    {
        var path = ExportPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            WriteWorkbook();
            path = ExportPath;
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            StatusText = "Write a workbook first.";
            return;
        }

        using var book = WorkbookHelper.Open(path, HelperLog.AppIds.ClosedXml);
        _readCache.Clear();
        ReadSheets.Clear();
        foreach (var name in book.SheetNames)
        {
            ReadSheets.Add(name);
            _readCache[name] = book.Sheet(name).ReadUsedRange();
        }

        BindSheetOrder(book.SheetNames);
        if (ReadSheets.Count == 0)
        {
            ReadCaption = "No sheets.";
            return;
        }

        if (!ReadSheets.Contains(ReadSheet))
            ReadSheet = ReadSheets[0];
        else
            BindReadSheet();
        StatusText = $"Read {path} · {ReadSheets.Count} sheet(s)";
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

    private void RebuildSwatches()
    {
        Swatches.Clear();
        foreach (var preview in ExcelTableStylePreview.In(StyleGroup))
            Swatches.Add(TableStyleSwatch.From(preview, preview.Id == TableStyle));
    }

    private void BindPreviewBrushes(ExcelTableStylePreview preview)
    {
        PreviewHeader = TableStyleSwatch.BrushOf(preview.Header);
        PreviewHeaderInk = TableStyleSwatch.BrushOf(preview.HeaderInk);
        PreviewBand = TableStyleSwatch.BrushOf(preview.Band);
        PreviewBandAlt = TableStyleSwatch.BrushOf(preview.BandAlt);
        PreviewBodyInk = TableStyleSwatch.BrushOf(preview.BodyInk);
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

    partial void OnReadSheetChanged(string value) => BindReadSheet();

    private void BindReadSheet()
    {
        ReadRows.Clear();
        if (!_readCache.TryGetValue(ReadSheet, out var table))
        {
            ReadCaption = "Pick a sheet after Read used range.";
            return;
        }

        ReadCaption = $"{table.Name ?? ReadSheet} · {table.Rows.Count:N0} rows · {table.Headers.Count} cols";
        var take = Math.Min(80, table.Rows.Count);
        for (var r = 0; r < take; r++)
        {
            var row = table.Rows[r];
            for (var c = 0; c < table.Headers.Count; c++)
            {
                var value = c < row.Count ? row[c] : null;
                ReadRows.Add(new ReadCellRow
                {
                    Row = r + 1,
                    Header = table.Headers[c],
                    Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
                    Type = SheetTable.CellKind(value)
                });
            }
        }
    }

    private void BindSheetOrder(IReadOnlyList<string> names)
    {
        SheetOrder.Clear();
        for (var i = 0; i < names.Count; i++)
            SheetOrder.Add(new KvRow { Key = (i + 1).ToString(CultureInfo.InvariantCulture), Value = names[i] });
    }

    private void SeedChrome()
    {
        ChromeFormats.Clear();
        ChromeFormats.Add(new KvRow { Key = "ms", Value = "0.0" });
        ChromeFormats.Add(new KvRow { Key = "pct / relative", Value = "0.00%" });
        ChromeFormats.Add(new KvRow { Key = "utc / timestamp", Value = "yyyy-mm-dd hh:mm:ss" });
        ChromePrint = "Landscape · fit-to-width";
        ChromeFooter = $"{HelperLog.AppIds.ClosedXml}  ·  &D &T";
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
