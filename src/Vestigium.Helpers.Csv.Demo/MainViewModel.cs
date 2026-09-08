using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Csv;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Csv.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public const int SampleSize = 1000;
    public const int PopulationSize = 100_000;

    public MainViewModel()
    {
        Sample = [];
        Injection = [];
        ReadRows = [];
        Dialects = ["comma", "tab", "semicolon", "pipe"];
        Dialect = "comma";
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Csv}";
        SeedInjection();
        DrawSample();
    }

    public string Identity => CsvHelper.Identity;

    public string StartupSnippet =>
        "using var file = CsvHelper.Create(HelperLog.AppIds.Csv);\n" +
        "CsvHelper.WriteSeries(file, series);\n" +
        "var path = file.Save();  // Desktop\\Vestigium\\Exports\\Csv\\";

    public IReadOnlyList<string> Dialects { get; }

    public ObservableCollection<SamplePoint> Sample { get; }
    public ObservableCollection<KvRow> Injection { get; }
    public ObservableCollection<ReadCellRow> ReadRows { get; }

    [ObservableProperty] private string dialect = "comma";
    [ObservableProperty] private bool utf8Bom = true;
    [ObservableProperty] private string exportPath = "";
    [ObservableProperty] private int count;
    [ObservableProperty] private string meanText = "";
    [ObservableProperty] private NumericSeries? series;
    [ObservableProperty] private string previewText = "";
    [ObservableProperty] private string readCaption = "Write a CSV, then Read it back.";
    [ObservableProperty] private string headerList = "Index, Value, Timestamp";

    public string ExportFolder => CsvHelper.DefaultExportDirectory(HelperLog.AppIds.Csv);

    public string DialectCaption => Dialect switch
    {
        "tab" => "Tab (TSV)",
        "semicolon" => "Semicolon",
        "pipe" => "Pipe",
        _ => "RFC 4180 comma"
    };

    partial void OnDialectChanged(string value) => OnPropertyChanged(nameof(DialectCaption));

    [RelayCommand]
    private void DrawSample()
    {
        var origin = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);
        var observations = DrawUnique(SampleSize, PopulationSize, origin);
        Series = NumericSeries.FromObservations(observations, "crypto-1k");
        Bind(Series);
        StatusText = $"Drew {Series.Count:N0} unique integers from 1..{PopulationSize:N0}";
        RefreshLines();
    }

    [RelayCommand]
    private void WriteCsv()
    {
        if (Series is null)
            DrawSample();
        if (Series is null)
            return;
        using var file = CsvHelper.Create(HelperLog.AppIds.Csv, CurrentOptions());
        CsvHelper.WriteSeries(file, Series);
        ExportPath = file.Save();
        PreviewText = Preview(ExportPath);
        HeaderList = "Index, Value, Timestamp";
        StatusText = $"Wrote {ExportPath} · {DialectCaption} · n={Series.Count}";
        RefreshLines();
    }

    [RelayCommand]
    private void ReadCsv()
    {
        if (string.IsNullOrWhiteSpace(ExportPath) || !File.Exists(ExportPath))
            WriteCsv();
        if (string.IsNullOrWhiteSpace(ExportPath))
            return;
        using var file = CsvHelper.Open(ExportPath, HelperLog.AppIds.Csv, CurrentOptions());
        var table = file.Read();
        HeaderList = string.Join(", ", table.Headers);
        ReadRows.Clear();
        var take = Math.Min(12, table.Rows.Count);
        for (var r = 0; r < take; r++)
        {
            ReadRows.Add(new ReadCellRow
            {
                Row = r + 1,
                A = Convert.ToString(table.Rows[r][0], CultureInfo.InvariantCulture) ?? "",
                B = table.Rows[r].Count > 1 ? Convert.ToString(table.Rows[r][1], CultureInfo.InvariantCulture) ?? "" : "",
                C = table.Rows[r].Count > 2 ? Convert.ToString(table.Rows[r][2], CultureInfo.InvariantCulture) ?? "" : ""
            });
        }

        ReadCaption = $"{table.Rows.Count} data rows · {table.Headers.Count} columns · {DialectCaption}";
        PreviewText = Preview(ExportPath);
        StatusText = $"Read {ExportPath}";
        RefreshLines();
    }

    [RelayCommand]
    private void WriteInject()
    {
        var table = CsvTable.Create(
            ["Payload"],
            [["=1+1"], ["+2"], ["-SUM(A1)"], ["@cmd"], ["plain"]]);
        using var file = CsvHelper.Create(HelperLog.AppIds.Csv, CurrentOptions());
        file.WriteTable(table);
        ExportPath = file.SaveAs(Path.Combine(
            CsvHelper.DefaultExportDirectory(HelperLog.AppIds.Csv),
            "inject.csv"));
        SeedInjection();
        PreviewText = Preview(ExportPath);
        StatusText = $"Wrote injection sample {ExportPath}";
        RefreshLines();
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        var dir = ExportFolder;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        StatusText = dir;
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = CsvHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    private CsvOptions CurrentOptions() => Dialect switch
    {
        "tab" => CsvOptions.Tab with { Utf8Bom = Utf8Bom },
        "semicolon" => CsvOptions.Semicolon with { Utf8Bom = Utf8Bom },
        "pipe" => CsvOptions.Pipe with { Utf8Bom = Utf8Bom },
        _ => CsvOptions.Rfc4180 with { Utf8Bom = Utf8Bom }
    };

    private void Bind(NumericSeries snap)
    {
        Count = snap.Count;
        MeanText = snap.Full.Mean?.ToString("N4", CultureInfo.InvariantCulture) ?? "—";
        Sample.Clear();
        for (var i = 0; i < Math.Min(12, snap.Count); i++)
        {
            Sample.Add(new SamplePoint
            {
                Index = i,
                Value = snap.Values[i].ToString(CultureInfo.InvariantCulture),
                At = snap.Times.Count == snap.Count && snap.Times[i] is { } t
                    ? t.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                    : ""
            });
        }
    }

    private void SeedInjection()
    {
        Injection.Clear();
        Injection.Add(new KvRow { Key = "=1+1", Value = "'=1+1" });
        Injection.Add(new KvRow { Key = "+2", Value = "'+2" });
        Injection.Add(new KvRow { Key = "-SUM(A1)", Value = "'-SUM(A1)" });
        Injection.Add(new KvRow { Key = "@cmd", Value = "'@cmd" });
        Injection.Add(new KvRow { Key = "plain", Value = "plain" });
    }

    private static string Preview(string path)
    {
        if (!File.Exists(path))
            return "";
        var bytes = File.ReadAllBytes(path);
        var take = Math.Min(bytes.Length, 800);
        var text = Encoding.UTF8.GetString(bytes, 0, take);
        if (text.Length > 0 && text[0] == '\uFEFF')
            text = text[1..];
        return text.Replace("\r\n", "\\r\\n\n");
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
}

public sealed class SamplePoint
{
    public int Index { get; init; }
    public string Value { get; init; } = "";
    public string At { get; init; } = "";
}

public sealed class ReadCellRow
{
    public int Row { get; init; }
    public string A { get; init; } = "";
    public string B { get; init; } = "";
    public string C { get; init; } = "";
}
