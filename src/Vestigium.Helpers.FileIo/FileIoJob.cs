using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// One validated file job. Recon fills five size buckets; consumers drain them after the lead.
/// This type never spawns robocopy.exe. Payload copies stream 64 KiB and never ReadAllBytes.
/// </summary>
public sealed partial class FileIoJob
{
    public const int StreamBufferSize = 64 * 1024;
    public const int HugeWatermark = 32;
    public static readonly TimeSpan MaxLead = TimeSpan.FromSeconds(180);

    private static readonly (FileIoBucket Id, string Name, long Max, int Workers)[] BucketTable =
    [
        (FileIoBucket.Tiny, "Tiny", 256L * 1024, 8),
        (FileIoBucket.Small, "Small", 4L * 1024 * 1024, 4),
        (FileIoBucket.Medium, "Medium", 32L * 1024 * 1024, 2),
        (FileIoBucket.Large, "Large", 256L * 1024 * 1024, 1),
        (FileIoBucket.Huge, "Huge", long.MaxValue, 1),
    ];

    private readonly FileIoJobOptions _options;
    private readonly bool _sourceIsFile;
    private readonly ConcurrentQueue<WorkItem>[] _queues;
    private readonly ConcurrentDictionary<string, byte> _created = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<FileIoTransferObservation> _observations = [];
    private readonly object _gate = new();
    private readonly CancellationTokenSource _cts = new();
    private TaskCompletionSource? _pauseWait;
    private TaskCompletionSource? _reconDone;
    private DateTimeOffset _started = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastRateAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastProgressLog = DateTimeOffset.MinValue;
    private volatile bool _reconComplete;
    private volatile bool _paused;
    private volatile bool _cancelled;
    private int _walked;

    private FileIoJob(FileIoVerb verb, string source, string destination, FileIoJobOptions options, bool sourceIsFile)
    {
        Verb = verb;
        Source = source;
        Destination = destination;
        _options = options;
        _sourceIsFile = sourceIsFile;
        JobId = "fio-" + FileIoLog.NewId();
        Options = options;
        Progress = new FileIoProgress { JobId = JobId };
        _queues = [new ConcurrentQueue<WorkItem>(), new ConcurrentQueue<WorkItem>(), new ConcurrentQueue<WorkItem>(), new ConcurrentQueue<WorkItem>(), new ConcurrentQueue<WorkItem>()];
    }

    public string JobId { get; }
    public FileIoVerb Verb { get; }
    public string Source { get; }
    public string Destination { get; }
    public FileIoJobOptions Options { get; }
    public FileIoProgress Progress { get; }
    public bool IsPaused => _paused;
    public event EventHandler<FileIoProgress>? ProgressChanged;

    public static FileIoJob Copy(string source, string destination, FileIoJobOptions? options = null)
        => Create(FileIoVerb.Copy, source, destination, options);

    public static FileIoJob Move(string source, string destination, FileIoJobOptions? options = null)
        => Create(FileIoVerb.Move, source, destination, options);

    public static FileIoJob Delete(string path, FileIoJobOptions? options = null)
        => Create(FileIoVerb.Delete, path, path, options);

    public static FileIoJob Mirror(string source, string destination, FileIoJobOptions? options = null)
    {
        var o = options ?? new FileIoJobOptions();
        o.IncludeEmptyDirectories = true;
        return Create(FileIoVerb.Mirror, source, destination, o);
    }

    internal static FileIoJob Create(FileIoVerb verb, string source, string destination, FileIoJobOptions? options)
    {
        var src = FileIoLog.RequireNotBlank(source, nameof(source));
        var dest = verb == FileIoVerb.Delete ? src : FileIoLog.RequireNotBlank(destination, nameof(destination));
        var o = options ?? new FileIoJobOptions();
        UniqueName.Parse(o.UniqueNamePattern);
        if (o.ReconLeadTime < TimeSpan.Zero || o.ReconLeadTime > MaxLead)
            throw new ArgumentOutOfRangeException(nameof(o.ReconLeadTime), "ReconLeadTime outside 0–180 seconds.");
        if (FileIoLog.LooksLikeSecret(o.RequestedBy))
            throw new ArgumentException("requestedBy looks like a secret.", nameof(options));
        if (FileIoLog.LooksLikeSecret(o.Reason))
            throw new ArgumentException("reason looks like a secret.", nameof(options));
        if (o.RequestedBy is { Length: > 50 })
            throw new ArgumentException("requestedBy exceeds 50 characters.", nameof(options));
        if (o.Reason is { Length: > 80 })
            throw new ArgumentException("reason exceeds 80 characters.", nameof(options));
        var srcIsFile = File.Exists(src);
        if (!srcIsFile && !Directory.Exists(src))
            throw new FileNotFoundException("source was not found.", src);
        return new FileIoJob(verb, src, dest, o, srcIsFile);
    }

    public void Pause()
    {
        _paused = true;
        Progress.IsPaused = true;
        FileIoLog.JobPaused(JobId, FileIoLog.Props(("jobId", JobId)));
        Emit();
    }

    public void Resume()
    {
        _paused = false;
        Progress.IsPaused = false;
        FileIoLog.JobResumed(JobId, FileIoLog.Props(("jobId", JobId)));
        _pauseWait?.TrySetResult();
        _pauseWait = null;
        Emit();
    }

    public void Cancel()
    {
        _cancelled = true;
        _paused = false;
        _pauseWait?.TrySetResult();
        _pauseWait = null;
        _cts.Cancel();
        FileIoLog.JobCancelled(JobId, FileIoLog.Props(("jobId", JobId)));
    }

    public async Task<FileIoJobResult> RunAsync(CancellationToken cancellation = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var token = linked.Token;
        _started = DateTimeOffset.UtcNow;
        var verbName = VerbLabel();
        FileIoLog.JobStart(JobId, FileIoLog.Props(
            ("jobId", JobId),
            ("verb", verbName),
            ("src", Source),
            ("dest", Destination),
            ("collision", _options.Collision.ToString()),
            ("pattern", _options.UniqueNamePattern),
            ("leadMs", ((int)_options.ReconLeadTime.TotalMilliseconds).ToString()),
            ("audit", _options.AuditMode ? "true" : "false"),
            ("uniqueContent", _options.CopyOnlyUniqueContent ? "true" : "false"),
            ("purge", _options.Purge ? "true" : "false"),
            ("retry", $"{_options.RetryCount}/{_options.RetryWait.TotalSeconds:0.#}s"),
            ("by", string.IsNullOrWhiteSpace(_options.RequestedBy) ? null : _options.RequestedBy),
            ("reason", string.IsNullOrWhiteSpace(_options.Reason) ? null : _options.Reason)));
        FileIoLog.ReconStart(JobId, FileIoLog.Props(("jobId", JobId)));

        var destIndex = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_options.CopyOnlyUniqueContent)
        {
            FileIoDestIndex.LoadInto(FileIoHelper.IndexPath(Destination), destIndex);
            if (Directory.Exists(Destination))
            {
                foreach (var f in SafeEnumerateFiles(Destination))
                {
                    try
                    {
                        destIndex[HashingHelper.HashFile(f, _options.HashAlgorithm)] = f;
                    }
                    catch (IOException)
                    {
                        /* dest locked; skip index row */
                    }
                }
            }
            FileIoLog.IndexBuilt(JobId, FileIoLog.Props(("jobId", JobId), ("files", destIndex.Count.ToString())));
        }

        _reconDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recon = ReconAsync(token);
        Progress.Phase = _options.ReconLeadTime > TimeSpan.Zero ? "WaitingLead" : "Transfer";
        Emit();
        if (_options.ReconLeadTime > TimeSpan.Zero)
            await Task.WhenAny(recon, Task.Delay(_options.ReconLeadTime, CancellationToken.None)).ConfigureAwait(false);
        if (_cancelled || token.IsCancellationRequested)
            return Finish("Cancelled", 0);
        Progress.Phase = "Transfer";
        FileIoLog.ConsumersReleased(JobId, FileIoLog.Props(("jobId", JobId), ("verb", verbName)));
        Emit();

        var consume = ConsumeAsync(destIndex, token);
        await recon.ConfigureAwait(false);
        _reconComplete = true;
        Progress.ReconComplete = true;
        Progress.CertaintyPercent = 100;
        FileIoLog.ReconComplete(JobId, FileIoLog.Props(
            ("jobId", JobId),
            ("files", Progress.FilesFound.ToString()),
            ("bytes", Progress.BytesFound.ToString())));
        Emit();
        await consume.ConfigureAwait(false);

        Progress.Phase = "Finalize";
        var deleted = 0;
        if (Verb == FileIoVerb.Mirror && _options.Purge && !_cancelled)
            deleted += PurgeDest();
        if (_options is { PruneEmptyDirectories: true, AuditMode: false } && !_cancelled && Directory.Exists(Destination))
        {
            var n = FileIoHelper.PruneEmptyDirectories(Destination);
            if (n > 0)
                FileIoLog.Success(FileIoLog.Subcategories.Prune, $"Prune empty dirs={n}", JobId);
        }

        var status = _cancelled || token.IsCancellationRequested
            ? "Cancelled"
            : Progress.FilesFailed > 0 ? "Failed" : "Success";
        return Finish(status, deleted);
    }

    private FileIoJobResult Finish(string status, int deleted)
    {
        Progress.Phase = "Done";
        Progress.ReconComplete = true;
        if (Progress.ReconComplete)
            Progress.CertaintyPercent = 100;
        Emit();
        var stats = FileIoJobStats.From([.. _observations], DateTimeOffset.UtcNow - _started, Progress.BytesDone);
        FileIoLog.StatsFinalize(JobId, FileIoLog.Props(("jobId", JobId), ("line", stats.FormatLine(JobId))));
        var endProps = FileIoLog.Props(
            ("jobId", JobId),
            ("status", status),
            ("found", Progress.FilesFound.ToString()),
            ("done", Progress.FilesDone.ToString()),
            ("skipped", Progress.FilesSkipped.ToString()),
            ("failed", Progress.FilesFailed.ToString()),
            ("bytes", Progress.BytesDone.ToString()));
        if (status == "Cancelled")
            FileIoLog.JobCancelled(JobId, endProps);
        else
            FileIoLog.JobComplete(JobId, endProps);
        return new FileIoJobResult
        {
            JobId = JobId,
            Verb = Verb,
            Status = status,
            Copied = Verb == FileIoVerb.Delete ? 0 : Progress.FilesDone,
            Skipped = Progress.FilesSkipped,
            Failed = Progress.FilesFailed,
            Deleted = Verb == FileIoVerb.Delete ? Progress.FilesDone : deleted,
            Bytes = Progress.BytesDone,
            AuditMode = _options.AuditMode,
            Stats = stats,
        };
    }

    private void RememberDest(string destPath, string relativePath, ConcurrentDictionary<string, string> destIndex)
    {
        if (!_options.CopyOnlyUniqueContent || _options.AuditMode)
            return;
        try
        {
            var digest = HashingHelper.HashFile(destPath, _options.HashAlgorithm);
            destIndex[digest] = destPath;
            var info = new FileInfo(destPath);
            FileIoDestIndex.Append(
                FileIoHelper.IndexPath(Destination),
                new FileIoDestIndex.Row(
                    relativePath,
                    info.Length,
                    info.LastWriteTimeUtc.ToString("O"),
                    digest));
        }
        catch (IOException)
        {
            /* index is best-effort */
        }
    }
}
