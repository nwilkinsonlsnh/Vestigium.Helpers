using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.FileIo.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public MainViewModel()
    {
        Volumes = new DemoVolumes();
        Volumes.Seed();
        Copy = new JobPane(FileIoVerb.Copy, Volumes, SetStatus, RefreshLines, BindStats);
        Move = new JobPane(FileIoVerb.Move, Volumes, SetStatus, RefreshLines, BindStats);
        Delete = new JobPane(FileIoVerb.Delete, Volumes, SetStatus, RefreshLines, BindStats);
        Mirror = new JobPane(FileIoVerb.Mirror, Volumes, SetStatus, RefreshLines, BindStats) { IncludeEmpty = true };
        Audit = new JobPane(FileIoVerb.Copy, Volumes, SetStatus, RefreshLines, BindStats, auditForce: true);
        Unique = new UniqueNamePane(SetStatus);
        Compare = new ComparePane(Volumes, SetStatus);
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.FileIo}";
        RefreshTrees();
    }

    public DemoVolumes Volumes { get; }
    public JobPane Copy { get; }
    public JobPane Move { get; }
    public JobPane Delete { get; }
    public JobPane Mirror { get; }
    public JobPane Audit { get; }
    public UniqueNamePane Unique { get; }
    public ComparePane Compare { get; }
    public ObservableCollection<StatsRow> StatsRows { get; } = [];
    public ObservableCollection<StatsRow> BucketStatsRows { get; } = [];

    [ObservableProperty] private string statsHeadline = "Run Copy, Move, Delete, or Mirror. Analytics describes the finished job.";
    [ObservableProperty] private string statsLine = "";

    public string Identity => FileIoHelper.Identity;
    public string DemoRoot => Volumes.Root;

    public string StartupSnippet =>
        "var job = FileIoHelper.Copy(export, archive);\n" +
        "job.ProgressChanged += (_, p) => ui.Render(p);\n" +
        "await job.RunAsync();  // UniqueName default, 15 s recon lead\n" +
        "var sizes = result.Stats.FileSizes;  // NumericSeries snapshot";

    public ObservableCollection<TreeRow> ExportRows { get; } = [];
    public ObservableCollection<TreeRow> ArchiveRows { get; } = [];
    public ObservableCollection<TreeRow> IndexRows { get; } = [];

    [RelayCommand]
    private void RunProbe()
    {
        var id = FileIoHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    [RelayCommand]
    private void OpenDemoFolder()
    {
        Directory.CreateDirectory(Volumes.Root);
        Process.Start(new ProcessStartInfo { FileName = Volumes.Root, UseShellExecute = true });
        StatusText = Volumes.Root;
    }

    [RelayCommand]
    private void ResetVolumes()
    {
        Volumes.Seed();
        RefreshTrees();
        StatusText = "Demo volumes reset under %TEMP%.";
        RefreshLines();
    }

    public void RefreshTrees()
    {
        Bind(ExportRows, Volumes.ExportRoot);
        Bind(ArchiveRows, Volumes.ArchiveRoot);
        IndexRows.Clear();
        foreach (var row in ExportRows.Where(r => r.Kind == "file"))
            IndexRows.Add(row with { Volume = "Export" });
        foreach (var row in ArchiveRows.Where(r => r.Kind == "file"))
            IndexRows.Add(row with { Volume = "Archive" });
        Copy.RefreshTrees();
        Move.RefreshTrees();
        Delete.RefreshTrees();
        Mirror.RefreshTrees();
        Audit.RefreshTrees();
        Compare.RefreshChoices();
    }

    static void Bind(ObservableCollection<TreeRow> target, string root)
    {
        target.Clear();
        foreach (var row in DemoVolumes.Walk(root, root))
            target.Add(row);
    }

    void SetStatus(string text)
    {
        StatusText = text;
        RefreshTrees();
        RefreshLines();
    }

    void BindStats(FileIoJobStats? stats)
    {
        StatsRows.Clear();
        BucketStatsRows.Clear();
        if (stats is null)
        {
            StatsHeadline = "Run Copy, Move, Delete, or Mirror. Analytics describes the finished job.";
            StatsLine = "";
            return;
        }
        StatsHeadline = $"n={stats.FileSizes.Count}  P50={FmtSize(stats.FileSizes.Median)}  P95={FmtSize(stats.FileSizes.P95)}  mean={FmtSize(stats.FileSizes.Mean)}  rates n={stats.TransferRates.Count}  job={FmtRate(stats.JobRateBytesPerSec)}";
        StatsLine = stats.FormatLine("job");
        AddStat("File size n", stats.FileSizes.Count.ToString(CultureInfo.InvariantCulture));
        AddStat("Min", FmtSize(stats.FileSizes.Min));
        AddStat("Q1", FmtSize(stats.FileSizes.Q1));
        AddStat("Median", FmtSize(stats.FileSizes.Median));
        AddStat("Q3", FmtSize(stats.FileSizes.Q3));
        AddStat("Max", FmtSize(stats.FileSizes.Max));
        AddStat("Mean", FmtSize(stats.FileSizes.Mean));
        AddStat("P90", FmtSize(stats.FileSizes.P90));
        AddStat("P95", FmtSize(stats.FileSizes.P95));
        AddStat("P99", FmtSize(stats.FileSizes.P99));
        AddStat("StdDev", FmtSize(stats.FileSizes.StdDev));
        AddStat("CV", stats.FileSizes.CoefficientOfVariation is { } cv ? cv.ToString("0.000", CultureInfo.InvariantCulture) : "—");
        AddStat("High outliers", stats.FileSizes.HighOutlierCount.ToString(CultureInfo.InvariantCulture));
        AddStat("Mean 95% CI", stats.FileSizes.MeanCiLower is { } lo
            ? $"{FmtSize(lo)} – {FmtSize(stats.FileSizes.MeanCiUpper)}"
            : "undefined (n < 2)");
        AddStat("Rate mean", FmtRate(stats.TransferRates.Mean));
        AddStat("Rate P95", FmtRate(stats.TransferRates.P95 is { } p ? (double)p : null));
        AddStat("Job rate", FmtRate(stats.JobRateBytesPerSec));
        AddStat("Elapsed", stats.JobElapsedMs + " ms");
        foreach (var b in stats.Buckets)
        {
            BucketStatsRows.Add(new StatsRow
            {
                Metric = b.Name,
                Value = $"n={b.FileSizes.Count}  P50={FmtSize(b.FileSizes.Median)}  P95={FmtSize(b.FileSizes.P95)}  rate={FmtRate(b.TransferRates.Mean)}"
            });
        }
    }

    void AddStat(string metric, string value) => StatsRows.Add(new StatsRow { Metric = metric, Value = value });

    static string FmtSize(decimal? n) => n is { } d ? DemoVolumes.FormatSize((long)Math.Round(d)) : "—";
    static string FmtSize(double? n) => n is { } d && double.IsFinite(d) ? DemoVolumes.FormatSize((long)Math.Round(d)) : "—";
    static string FmtRate(double? n)
    {
        if (n is not { } d || !double.IsFinite(d) || d <= 0) return "—";
        if (d < 1024) return Math.Round(d).ToString("0", CultureInfo.InvariantCulture) + " B/s";
        if (d < 1024 * 1024) return (d / 1024).ToString("N1", CultureInfo.InvariantCulture) + " KiB/s";
        return (d / (1024.0 * 1024.0)).ToString("N1", CultureInfo.InvariantCulture) + " MiB/s";
    }

    public override void Dispose()
    {
        base.Dispose();
        Copy.Detach();
        Move.Detach();
        Delete.Detach();
        Mirror.Detach();
        Audit.Detach();
    }
}

public sealed class DemoVolumes
{
    public string Root { get; private set; } = "";
    public string ExportRoot => Path.Combine(Root, "Export");
    public string ArchiveRoot => Path.Combine(Root, "Archive");

    public void Seed()
    {
        if (!string.IsNullOrWhiteSpace(Root) && Directory.Exists(Root))
        {
            try { Directory.Delete(Root, true); } catch (IOException) { }
        }
        Root = Path.Combine(Path.GetTempPath(), "Vestigium.Helpers.FileIo.Demo", DateTime.UtcNow.ToString("HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Path.Combine(ExportRoot, "empty"));
        Directory.CreateDirectory(Path.Combine(ExportRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(ExportRoot, "captures"));
        Directory.CreateDirectory(Path.Combine(ExportRoot, "media"));
        Directory.CreateDirectory(Path.Combine(ExportRoot, "dup"));
        Directory.CreateDirectory(ArchiveRoot);
        File.WriteAllText(Path.Combine(ExportRoot, "nathan.txt"), "nathan capture\n");
        File.WriteAllText(Path.Combine(ExportRoot, "logs", "session.jsonl"), "{\"STATUS\":\"Pending\"}\n{\"STATUS\":\"Success\"}\n");
        File.WriteAllText(Path.Combine(ExportRoot, "dup", "report.txt"), "report-body-v1");
        File.WriteAllText(Path.Combine(ArchiveRoot, "nathan.txt"), "older nathan\n");
        File.WriteAllText(Path.Combine(ArchiveRoot, "report.txt"), "dest report");
        WriteSized(Path.Combine(ExportRoot, "captures", "frame.bin"), 300_000, 0x11);
        WriteSized(Path.Combine(ExportRoot, "captures", "trace.bin"), 5_000_000, 0xA5);
        WriteSized(Path.Combine(ExportRoot, "media", "clip.dat"), 40_000_000, 0x3C);
    }

    static void WriteSized(string path, long length, byte fill)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        if (length <= 0) return;
        stream.WriteByte(fill);
        stream.SetLength(length);
    }

    public static IEnumerable<TreeRow> Walk(string volumeRoot, string start)
    {
        if (!Directory.Exists(start))
            yield break;
        var relDir = Path.GetRelativePath(volumeRoot, start);
        if (relDir != ".")
            yield return new TreeRow { Path = relDir.Replace('\\', '/') + "/", Kind = "dir", Volume = Path.GetFileName(volumeRoot) };
        foreach (var dir in Directory.GetDirectories(start).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var row in Walk(volumeRoot, dir))
                yield return row;
        }
        foreach (var file in Directory.GetFiles(start).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var info = new FileInfo(file);
            yield return new TreeRow
            {
                Path = Path.GetRelativePath(volumeRoot, file).Replace('\\', '/'),
                Kind = "file",
                Size = FormatSize(info.Length),
                Bucket = BucketName(info.Length),
                FullPath = file,
                Volume = Path.GetFileName(volumeRoot),
            };
        }
    }

    public static string BucketName(long size) =>
        size <= 256L * 1024 ? "Tiny" :
        size <= 4L * 1024 * 1024 ? "Small" :
        size <= 32L * 1024 * 1024 ? "Medium" :
        size <= 256L * 1024 * 1024 ? "Large" : "Huge";

    public static string FormatSize(long n)
    {
        if (n < 1024) return n + " B";
        if (n < 1024 * 1024) return (n / 1024.0).ToString("N1", CultureInfo.InvariantCulture) + " KiB";
        return (n / (1024.0 * 1024.0)).ToString("N1", CultureInfo.InvariantCulture) + " MiB";
    }
}

public sealed record TreeRow
{
    public string Path { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Size { get; init; } = "";
    public string Bucket { get; init; } = "";
    public string FullPath { get; init; } = "";
    public string Volume { get; init; } = "";
}

public sealed record StatsRow
{
    public string Metric { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed partial class BucketRow : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string caption = "0/0";
    [ObservableProperty] private int percent;
}

public sealed partial class JobPane : ObservableObject
{
    readonly DemoVolumes _volumes;
    readonly Action<string> _status;
    readonly Action _refreshLines;
    readonly Action<FileIoJobStats?> _onStats;
    readonly bool _auditForce;
    FileIoJob? _job;

    public JobPane(FileIoVerb verb, DemoVolumes volumes, Action<string> status, Action refreshLines, Action<FileIoJobStats?> onStats, bool auditForce = false)
    {
        Verb = verb;
        _volumes = volumes;
        _status = status;
        _refreshLines = refreshLines;
        _onStats = onStats;
        _auditForce = auditForce;
        Audit = auditForce;
        IncludeEmpty = verb == FileIoVerb.Mirror;
        Title = verb switch
        {
            FileIoVerb.Move => "Move",
            FileIoVerb.Delete => "Delete",
            FileIoVerb.Mirror => "Mirror",
            _ => auditForce ? "Audit Mode" : "Copy"
        };
        Blurb = auditForce
            ? "Same recon, same UniqueName decisions, zero mutations. WouldCopy / WouldUniqueName / WouldDelete land on JSONL."
            : "Recon fills five buckets. Consumers start after the lead. Pause resumes committed bytes. Cancel aborts the current write.";
        CollisionChoices = ["UniqueName", "Skip", "Overwrite"];
        CollisionChoice = "UniqueName";
        Pattern = UniqueName.DefaultPattern;
        LeadSeconds = 2;
        foreach (var name in new[] { "Tiny", "Small", "Medium", "Large", "Huge" })
            Buckets.Add(new BucketRow { Name = name });
        RefreshTrees();
    }

    public FileIoVerb Verb { get; }
    public string Title { get; }
    public string Blurb { get; }
    public IReadOnlyList<string> CollisionChoices { get; }
    public bool ShowPurge => Verb == FileIoVerb.Mirror;
    public bool ShowShred => Verb == FileIoVerb.Delete;
    public bool AuditEnabled => !_auditForce;
    public ObservableCollection<TreeRow> ExportRows { get; } = [];
    public ObservableCollection<TreeRow> ArchiveRows { get; } = [];
    public ObservableCollection<BucketRow> Buckets { get; } = [];

    [ObservableProperty] private string collisionChoice = "UniqueName";
    [ObservableProperty] private string pattern = ".##";
    [ObservableProperty] private int leadSeconds = 2;
    [ObservableProperty] private bool uniqueContent;
    [ObservableProperty] private bool audit;
    [ObservableProperty] private bool includeEmpty;
    [ObservableProperty] private bool purge;
    [ObservableProperty] private bool shred;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool busy;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeCommand))]
    private bool paused;
    [ObservableProperty] private string percentText = "—";
    [ObservableProperty] private string phaseText = "Idle";
    [ObservableProperty] private int percent;
    [ObservableProperty] private int certainty;
    [ObservableProperty] private string certaintyCaption = "recon idle";
    [ObservableProperty] private string summary = "";
    [ObservableProperty] private string counts = "";

    public void RefreshTrees()
    {
        ExportRows.Clear();
        ArchiveRows.Clear();
        foreach (var row in DemoVolumes.Walk(_volumes.ExportRoot, _volumes.ExportRoot))
            ExportRows.Add(row);
        foreach (var row in DemoVolumes.Walk(_volumes.ArchiveRoot, _volumes.ArchiveRoot))
            ArchiveRows.Add(row);
    }

    bool CanRun() => !Busy;
    bool CanPause() => Busy && !Paused;
    bool CanResume() => Busy && Paused;
    bool CanCancel() => Busy;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        Busy = true;
        Paused = false;
        Summary = "";
        try
        {
            var options = new FileIoJobOptions
            {
                Collision = CollisionChoice switch
                {
                    "Skip" => FileIoCollision.Skip,
                    "Overwrite" => FileIoCollision.Overwrite,
                    _ => FileIoCollision.UniqueName
                },
                UniqueNamePattern = Pattern,
                ReconLeadTime = TimeSpan.FromSeconds(LeadSeconds),
                CopyOnlyUniqueContent = UniqueContent,
                AuditMode = _auditForce || Audit,
                IncludeEmptyDirectories = IncludeEmpty,
                Purge = Purge,
                Shred = Shred ? FileIoShredRecipe.ZeroRandomZero : null,
            };
            var dest = Verb == FileIoVerb.Delete ? _volumes.ExportRoot : _volumes.ArchiveRoot;
            _job = Verb switch
            {
                FileIoVerb.Move => FileIoHelper.Move(_volumes.ExportRoot, dest, options),
                FileIoVerb.Delete => FileIoHelper.Delete(_volumes.ExportRoot, options),
                FileIoVerb.Mirror => FileIoHelper.Mirror(_volumes.ExportRoot, dest, options),
                _ => FileIoHelper.Copy(_volumes.ExportRoot, dest, options)
            };
            _job.ProgressChanged += OnProgress;
            var result = await _job.RunAsync().ConfigureAwait(true);
            Summary = $"{result.Status} · done {Math.Max(result.Copied, result.Deleted)} · skipped {result.Skipped} · failed {result.Failed} · {DemoVolumes.FormatSize(result.Bytes)}"
                + (result.AuditMode ? " · audit" : "");
            if (result.Stats is { } stats)
            {
                Summary += $" · sizes n={stats.FileSizes.Count} P50={DemoVolumes.FormatSize((long)(stats.FileSizes.Median ?? 0))} rates n={stats.TransferRates.Count}";
                _onStats(stats);
            }
            _status($"{Title} {result.Status}");
        }
        catch (Exception ex)
        {
            Summary = ex.Message;
            _status(ex.Message);
        }
        finally
        {
            Detach();
            Busy = false;
            Paused = false;
            RefreshTrees();
            _refreshLines();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        _job?.Pause();
        Paused = true;
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume()
    {
        _job?.Resume();
        Paused = false;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _job?.Cancel();

    public void Detach()
    {
        if (_job is null) return;
        _job.ProgressChanged -= OnProgress;
        _job = null;
    }

    void OnProgress(object? sender, FileIoProgress p)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            Apply(p);
        else
            dispatcher.BeginInvoke(() => Apply(p));
    }

    void Apply(FileIoProgress p)
    {
        Paused = p.IsPaused;
        PhaseText = p.IsPaused ? p.Phase + " · paused" : p.Phase;
        Certainty = p.CertaintyPercent;
        CertaintyCaption = p.ReconComplete ? "population known" : "recon running";
        Percent = p.BytesFound > 0
            ? (int)Math.Min(100, Math.Round(100.0 * p.BytesDone / p.BytesFound))
            : p.FilesFound > 0 ? (int)Math.Min(100, Math.Round(100.0 * p.FilesDone / p.FilesFound)) : 0;
        PercentText = Percent + "%";
        Counts = $"files {p.FilesDone}/{p.FilesFound} · skipped {p.FilesSkipped} · failed {p.FilesFailed} · {DemoVolumes.FormatSize(p.BytesDone)} / {DemoVolumes.FormatSize(p.BytesFound)}";
        for (var i = 0; i < Buckets.Count && i < p.Buckets.Length; i++)
        {
            var b = p.Buckets[i];
            Buckets[i].Percent = b.BytesFound > 0 ? (int)Math.Round(100.0 * b.BytesDone / b.BytesFound) : 0;
            Buckets[i].Caption = $"{b.Done}/{b.Found} q{b.Queued}";
        }
    }
}

public sealed partial class UniqueNamePane : ObservableObject
{
    readonly Action<string> _status;

    public UniqueNamePane(Action<string> status)
    {
        _status = status;
        Original = "report.txt";
        Pattern = UniqueName.DefaultPattern;
        Existing.Add("report.txt");
        Caption = "Mint the next UniqueName. Cap never overwrites.";
    }

    public ObservableCollection<string> Existing { get; } = [];

    [ObservableProperty] private string original = "report.txt";
    [ObservableProperty] private string pattern = ".##";
    [ObservableProperty] private string caption = "";
    [ObservableProperty] private string errorText = "";

    [RelayCommand]
    private void Mint()
    {
        try
        {
            UniqueName.Parse(Pattern);
            var next = UniqueName.Next(Existing.ToArray(), Original, Pattern);
            if (next is null)
            {
                ErrorText = "";
                Caption = "NameCap — pattern exhausted. The file fails. Dest is not overwritten.";
                _status("NameCap");
                return;
            }
            Existing.Add(next);
            ErrorText = "";
            Caption = $"{Original} → {next}";
            _status(Caption);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void ResetList()
    {
        Existing.Clear();
        Existing.Add(string.IsNullOrWhiteSpace(Original) ? "report.txt" : Original);
        Caption = "Reset to the original name only.";
        ErrorText = "";
    }

    [RelayCommand]
    private void UseAlpha()
    {
        Pattern = "A##";
        Existing.Clear();
        Existing.Add("report.txt");
        Caption = "Alpha width two. A01 … A99 then B01.";
        ErrorText = "";
    }
}

public sealed partial class ComparePane : ObservableObject
{
    readonly DemoVolumes _volumes;
    readonly Action<string> _status;

    public ComparePane(DemoVolumes volumes, Action<string> status)
    {
        _volumes = volumes;
        _status = status;
        RefreshChoices();
    }

    public ObservableCollection<string> LeftChoices { get; } = [];
    public ObservableCollection<string> RightChoices { get; } = [];

    [ObservableProperty] private string leftPath = "";
    [ObservableProperty] private string rightPath = "";
    [ObservableProperty] private string result = "Pick two files and compare SHA-256.";
    [ObservableProperty] private string leftDigest = "";
    [ObservableProperty] private string rightDigest = "";

    public void RefreshChoices()
    {
        LeftChoices.Clear();
        RightChoices.Clear();
        foreach (var row in DemoVolumes.Walk(_volumes.ExportRoot, _volumes.ExportRoot).Where(r => r.Kind == "file"))
            LeftChoices.Add(row.FullPath);
        foreach (var row in DemoVolumes.Walk(_volumes.ArchiveRoot, _volumes.ArchiveRoot).Where(r => r.Kind == "file"))
            RightChoices.Add(row.FullPath);
        if (string.IsNullOrWhiteSpace(LeftPath) && LeftChoices.Count > 0)
            LeftPath = LeftChoices[0];
        if (string.IsNullOrWhiteSpace(RightPath) && RightChoices.Count > 0)
            RightPath = RightChoices[0];
    }

    [RelayCommand]
    private void Compare()
    {
        if (string.IsNullOrWhiteSpace(LeftPath) || string.IsNullOrWhiteSpace(RightPath))
        {
            Result = "Pick two files that exist on the demo volumes.";
            return;
        }
        var cmp = FileIoHelper.CompareFiles(LeftPath, RightPath);
        LeftDigest = cmp.LeftDigest;
        RightDigest = cmp.RightDigest;
        Result = cmp.Equal ? "Equal — same SHA-256 and size." : "Different — unique-content skip would not match these.";
        _status(Result);
    }
}
