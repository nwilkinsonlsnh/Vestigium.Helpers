using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.FileIo.Demo;

public sealed partial class MainViewModel
{
    public ObservableCollection<StatsRow> AnalyzeRows { get; } = [];
    public ObservableCollection<StatsRow> AnalyzeBuckets { get; } = [];

    string _analyzePath = "";
    string _analyzeSizeValue = "2048";
    string _analyzeSizeUnit = "MiB";
    string _analyzeSummary = "Analyze walks sizes only. 2048 MiB displays as 2 GiB. Probe writes random bytes and deletes them.";
    string _analyzeNormalized = "";

    public string AnalyzePath
    {
        get => _analyzePath;
        set => SetProperty(ref _analyzePath, value);
    }

    public string AnalyzeSizeValue
    {
        get => _analyzeSizeValue;
        set => SetProperty(ref _analyzeSizeValue, value);
    }

    public string AnalyzeSizeUnit
    {
        get => _analyzeSizeUnit;
        set => SetProperty(ref _analyzeSizeUnit, value);
    }

    public string AnalyzeSummary
    {
        get => _analyzeSummary;
        set => SetProperty(ref _analyzeSummary, value);
    }

    public string AnalyzeNormalized
    {
        get => _analyzeNormalized;
        set => SetProperty(ref _analyzeNormalized, value);
    }

    public IReadOnlyList<string> SizeUnits { get; } =
        ["Byte", "KB", "KiB", "MB", "MiB", "GB", "GiB", "TB", "TiB"];

    [RelayCommand]
    private void NormalizeSizeDemo()
    {
        try
        {
            var size = ParseCallerSize();
            AnalyzeNormalized = $"{size.InputDisplay} → {size.Display} ({size.Bytes:N0} bytes)";
            StatusText = AnalyzeNormalized;
        }
        catch (Exception ex)
        {
            AnalyzeNormalized = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void AnalyzeDemoFolder()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AnalyzePath))
                AnalyzePath = Volumes.ExportRoot;
            var analysis = FileIoHelper.AnalyzeDirectory(AnalyzePath);
            AnalyzeRows.Clear();
            AnalyzeBuckets.Clear();
            AnalyzeRows.Add(new StatsRow { Metric = "Path", Value = analysis.Path });
            AnalyzeRows.Add(new StatsRow { Metric = "Files", Value = analysis.FileCount.ToString() });
            AnalyzeRows.Add(new StatsRow { Metric = "Directories", Value = analysis.DirectoryCount.ToString() });
            AnalyzeRows.Add(new StatsRow { Metric = "Total", Value = analysis.TotalSize.Display });
            AnalyzeRows.Add(new StatsRow { Metric = "P50 size", Value = analysis.FileSizes.Median?.ToString("N0") ?? "—" });
            AnalyzeRows.Add(new StatsRow { Metric = "P95 size", Value = analysis.FileSizes.P95?.ToString("N0") ?? "—" });
            foreach (var b in analysis.Buckets)
            {
                AnalyzeBuckets.Add(new StatsRow
                {
                    Metric = b.Name,
                    Value = $"n={b.FileCount}  bytes={FileIoSize.FromBytes(b.TotalBytes).Display}  P50={b.Sizes.Median?.ToString("N0") ?? "—"}"
                });
            }
            AnalyzeSummary = $"{analysis.FileCount} files · {analysis.TotalSize.Display} · no copy";
            StatusText = AnalyzeSummary;
        }
        catch (Exception ex)
        {
            AnalyzeSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void WriteProbeDemo()
    {
        try
        {
            var dest = Path.Combine(Volumes.Root, "probe");
            var size = FileIoSize.From(1, FileIoSizeUnit.MiB);
            var result = FileIoHelper.WriteProbe(dest, size);
            AnalyzeSummary = $"probe {result.Size.Display} in {result.Duration.TotalMilliseconds:0} ms · {result.BytesPerSecond:N0} B/s · deleted={result.Deleted}";
            StatusText = AnalyzeSummary;
        }
        catch (Exception ex)
        {
            AnalyzeSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    FileIoSize ParseCallerSize()
    {
        if (!decimal.TryParse(AnalyzeSizeValue, out var value))
            value = 1;
        var unit = Enum.TryParse<FileIoSizeUnit>(AnalyzeSizeUnit, true, out var parsed)
            ? parsed
            : FileIoSizeUnit.MiB;
        return FileIoSize.From(value, unit);
    }
}
