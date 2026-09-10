using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.FileIo.Demo;

public sealed partial class MainViewModel
{
    public ObservableCollection<StatsRow> AnalyzeRows { get; } = [];
    public ObservableCollection<StatsRow> AnalyzeBuckets { get; } = [];

    string _analyzePath = @"\\truenas\sandbox";
    string _analyzeSizeValue = "1";
    string _analyzeSizeUnit = "TB";
    string _analyzeSummary = "Paste a reachable path. UNC example: \\\\truenas\\sandbox. Analyze = sizes only. Probe writes 64 MiB to that path (then deletes) and scales the caller size.";
    string _analyzeNormalized = "";
    string _analyzeEstimate = "";
    bool _analyzeBusy;

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

    public string AnalyzeEstimate
    {
        get => _analyzeEstimate;
        set => SetProperty(ref _analyzeEstimate, value);
    }

    public bool AnalyzeBusy
    {
        get => _analyzeBusy;
        set
        {
            if (SetProperty(ref _analyzeBusy, value))
            {
                AnalyzeDemoFolderCommand.NotifyCanExecuteChanged();
                WriteProbeDemoCommand.NotifyCanExecuteChanged();
            }
        }
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
    private void UseDemoFolder()
    {
        AnalyzePath = Volumes.ExportRoot;
        StatusText = "Analyze path set to demo Export folder.";
    }

    bool CanAnalyzeShare() => !AnalyzeBusy;

    [RelayCommand(CanExecute = nameof(CanAnalyzeShare))]
    private async Task AnalyzeDemoFolderAsync()
    {
        AnalyzeBusy = true;
        try
        {
            var path = ResolveAnalyzePath();
            AnalyzeSummary = "Analyzing " + path + " … UI stays live while sizes are walked.";
            StatusText = AnalyzeSummary;
            var analysis = await Task.Run(() => FileIoHelper.AnalyzeDirectory(path)).ConfigureAwait(true);
            AnalyzeRows.Clear();
            AnalyzeBuckets.Clear();
            AnalyzeRows.Add(new StatsRow { Metric = "Path", Value = analysis.Path });
            AnalyzeRows.Add(new StatsRow { Metric = "Files", Value = analysis.FileCount.ToString(CultureInfo.InvariantCulture) });
            AnalyzeRows.Add(new StatsRow { Metric = "Directories", Value = analysis.DirectoryCount.ToString(CultureInfo.InvariantCulture) });
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
        finally
        {
            AnalyzeBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAnalyzeShare))]
    private async Task WriteProbeDemoAsync()
    {
        AnalyzeBusy = true;
        try
        {
            var path = ResolveAnalyzePath();
            var probeDir = Path.Combine(path, ".vestigium-probe");
            var planned = ParseCallerSize();
            AnalyzeSummary = "Writing 64 MiB probe to " + probeDir + " …";
            StatusText = AnalyzeSummary;
            var probe = await Task.Run(() => FileIoHelper.WriteProbe(probeDir, FileIoSize.From(64, FileIoSizeUnit.MiB))).ConfigureAwait(true);
            var seconds = probe.BytesPerSecond > 0 ? planned.Bytes / probe.BytesPerSecond : 0;
            var span = TimeSpan.FromSeconds(seconds);
            AnalyzeEstimate =
                $"Measured {probe.BytesPerSecond / (1024.0 * 1024.0):0.00} MiB/s on {path}. " +
                $"{planned.Display} write ≈ {FormatDuration(span)}. Linear scale from 64 MiB sequential; many small files will be slower.";
            AnalyzeSummary =
                $"probe {probe.Size.Display} in {probe.Duration.TotalMilliseconds:0} ms · deleted={probe.Deleted}";
            StatusText = AnalyzeEstimate;
        }
        catch (Exception ex)
        {
            AnalyzeEstimate = ex.Message;
            AnalyzeSummary = ex.Message;
            StatusText = ex.Message;
        }
        finally
        {
            AnalyzeBusy = false;
        }
    }

    string ResolveAnalyzePath()
    {
        var path = string.IsNullOrWhiteSpace(AnalyzePath) ? Volumes.ExportRoot : AnalyzePath.Trim();
        AnalyzePath = path;
        return path;
    }

    FileIoSize ParseCallerSize()
    {
        if (!decimal.TryParse(AnalyzeSizeValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            value = 1;
        var unit = Enum.TryParse<FileIoSizeUnit>(AnalyzeSizeUnit, true, out var parsed)
            ? parsed
            : FileIoSizeUnit.TB;
        return FileIoSize.From(value, unit);
    }

    static string FormatDuration(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return span.TotalHours.ToString("0.00", CultureInfo.InvariantCulture) + " h";
        if (span.TotalMinutes >= 1)
            return span.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + " min";
        return span.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
    }
}
