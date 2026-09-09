using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Vestigium.Helpers;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// One validated file job. Recon fills five size buckets; consumers drain them after the lead.
/// This type never spawns robocopy.exe. Payload copies stream 64 KiB and never ReadAllBytes.
/// </summary>
public sealed class FileIoJob
{
    public const int StreamBufferSize = 64 * 1024;
    public const int HugeWatermark = 32;
    public static readonly TimeSpan MaxLead = TimeSpan.FromSeconds(180);

    static readonly (FileIoBucket Id, string Name, long Max, int Workers)[] BucketTable =
    [
        (FileIoBucket.Tiny, "Tiny", 256L * 1024, 8),
        (FileIoBucket.Small, "Small", 4L * 1024 * 1024, 4),
        (FileIoBucket.Medium, "Medium", 32L * 1024 * 1024, 2),
        (FileIoBucket.Large, "Large", 256L * 1024 * 1024, 1),
        (FileIoBucket.Huge, "Huge", long.MaxValue, 1),
    ];

    readonly FileIoJobOptions _options;
    readonly bool _sourceIsFile;
    readonly ConcurrentQueue<WorkItem>[] _queues;
    readonly ConcurrentDictionary<string, byte> _created = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();
    readonly CancellationTokenSource _cts = new();
    TaskCompletionSource? _pauseWait;
    TaskCompletionSource? _reconDone;
    DateTimeOffset _lastRateAt = DateTimeOffset.UtcNow;
    DateTimeOffset _lastProgressLog = DateTimeOffset.MinValue;
    volatile bool _reconComplete;
    volatile bool _paused;
    volatile bool _cancelled;
    int _walked;

    FileIoJob(FileIoVerb verb, string source, string destination, FileIoJobOptions options, bool sourceIsFile)
    {
        Verb = verb;
        Source = source;
        Destination = destination;
        _options = options;
        _sourceIsFile = sourceIsFile;
        JobId = "fio-" + HelperLog.NewId();
        Options = options;
        Progress = new FileIoProgress { JobId = JobId };
        _queues = [new(), new(), new(), new(), new()];
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
        var src = HelperGuard.NotBlank(source, nameof(source));
        var dest = verb == FileIoVerb.Delete ? src : HelperGuard.NotBlank(destination, nameof(destination));
        var o = options ?? new FileIoJobOptions();
        UniqueName.Parse(o.UniqueNamePattern);
        if (o.ReconLeadTime < TimeSpan.Zero || o.ReconLeadTime > MaxLead)
            throw new ArgumentOutOfRangeException(nameof(o.ReconLeadTime), "ReconLeadTime outside 0–180 seconds.");
        if (FileIoLog.LooksLikeSecret(o.RequestedBy))
            throw new ArgumentException("requestedBy looks like a secret.", nameof(options));
        if (FileIoLog.LooksLikeSecret(o.Reason))
            throw new ArgumentException("reason looks like a secret.", nameof(options));
        var srcIsFile = File.Exists(src);
        if (!srcIsFile && !Directory.Exists(src))
            throw new FileNotFoundException("source was not found.", src);
        return new FileIoJob(verb, src, dest, o, srcIsFile);
    }

    public void Pause()
    {
        _paused = true;
        Progress.IsPaused = true;
        FileIoLog.Warning(HelperLog.Subcategories.Job, $"Pause job={JobId}");
        Emit();
    }

    public void Resume()
    {
        _paused = false;
        Progress.IsPaused = false;
        FileIoLog.Success(HelperLog.Subcategories.Job, $"Resume job={JobId}");
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
    }

    public async Task<FileIoJobResult> RunAsync(CancellationToken cancellation = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var token = linked.Token;
        var verbName = VerbLabel();
        FileIoLog.Pending(
            HelperLog.Subcategories.Job,
            $"{verbName} start job={JobId} src={Source} dest={Destination} collision={_options.Collision} pattern={_options.UniqueNamePattern} leadMs={(int)_options.ReconLeadTime.TotalMilliseconds} audit={_options.AuditMode} uniqueContent={_options.CopyOnlyUniqueContent} purge={_options.Purge}");
        FileIoLog.Pending(HelperLog.Subcategories.Recon, $"Recon start job={JobId}");

        var destIndex = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_options.CopyOnlyUniqueContent && Directory.Exists(Destination))
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
            FileIoLog.Success(HelperLog.Subcategories.Index, $"Index built dest files={destIndex.Count}");
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
        FileIoLog.Success(verbName == "Delete" ? HelperLog.Subcategories.Delete : HelperLog.Subcategories.Copy, $"Consumers released job={JobId}");
        Emit();

        var consume = ConsumeAsync(destIndex, token);
        await recon.ConfigureAwait(false);
        _reconComplete = true;
        Progress.ReconComplete = true;
        Progress.CertaintyPercent = 100;
        FileIoLog.Success(HelperLog.Subcategories.Recon, $"Recon complete files={Progress.FilesFound} bytes={Progress.BytesFound}");
        Emit();
        await consume.ConfigureAwait(false);

        Progress.Phase = "Finalize";
        var deleted = 0;
        if (Verb == FileIoVerb.Mirror && _options.Purge && !_cancelled)
            deleted += PurgeDest();
        if (_options.PruneEmptyDirectories && !_options.AuditMode && !_cancelled && Directory.Exists(Destination))
        {
            var n = FileIoHelper.PruneEmptyDirectories(Destination);
            if (n > 0)
                FileIoLog.Success(HelperLog.Subcategories.Prune, $"Prune empty dirs={n}");
        }

        var status = _cancelled || token.IsCancellationRequested
            ? "Cancelled"
            : Progress.FilesFailed > 0 ? "Failed" : "Success";
        return Finish(status, deleted);
    }

    FileIoJobResult Finish(string status, int deleted)
    {
        Progress.Phase = "Done";
        Progress.ReconComplete = true;
        if (Progress.ReconComplete)
            Progress.CertaintyPercent = 100;
        Emit();
        var sub = HelperLog.Subcategories.Job;
        if (status == "Success")
            FileIoLog.Success(sub, $"{status} job={JobId} found={Progress.FilesFound} done={Progress.FilesDone} skipped={Progress.FilesSkipped} failed={Progress.FilesFailed} bytes={Progress.BytesDone}");
        else
            FileIoLog.Failed(sub, $"{status} job={JobId} found={Progress.FilesFound} done={Progress.FilesDone} skipped={Progress.FilesSkipped} failed={Progress.FilesFailed} bytes={Progress.BytesDone}");
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
        };
    }

    async Task ReconAsync(CancellationToken token)
    {
        try
        {
            if (_sourceIsFile)
            {
                OfferFile(Source, Path.GetFileName(Source));
                return;
            }

            if (_options.IncludeEmptyDirectories && !_options.AuditMode && Verb != FileIoVerb.Delete)
                Directory.CreateDirectory(Destination);

            var dirs = new ConcurrentQueue<(string Path, int Depth)>();
            dirs.Enqueue((Source, 0));
            var walking = new[] { 1 };
            var workers = Math.Clamp(Math.Max(2, Environment.ProcessorCount / 2), 2, 8);
            var tasks = new Task[workers];
            for (var i = 0; i < workers; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    while (!_cancelled && !token.IsCancellationRequested)
                    {
                        if (!dirs.TryDequeue(out var node))
                        {
                            if (Volatile.Read(ref walking[0]) == 0)
                                return;
                            Thread.Sleep(5);
                            continue;
                        }
                        try
                        {
                            WalkDirectory(node.Path, node.Depth, dirs, walking);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref walking[0]);
                        }
                    }
                }, CancellationToken.None);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* cancel is a result, not a throw */
        }
        finally
        {
            _reconComplete = true;
            Progress.ReconComplete = true;
            Progress.CertaintyPercent = 100;
            _reconDone?.TrySetResult();
            Emit();
        }
    }

    void WalkDirectory(string dir, int depth, ConcurrentQueue<(string Path, int Depth)> dirs, int[] walking)
    {
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileIoLog.Failed(HelperLog.Subcategories.Recon, $"Unauthorized path={dir}");
            return;
        }

        foreach (var entry in entries)
        {
            if (_cancelled)
                return;
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry))
            {
                if (Masked(name, _options.ExcludeDirectoryMasks))
                    continue;
                if (_options.MaxDepth is { } max && depth + 1 >= max)
                    continue;
                if (_options.IncludeEmptyDirectories && !_options.AuditMode && Verb != FileIoVerb.Delete)
                {
                    var rel = Rel(Source, entry);
                    if (!string.IsNullOrEmpty(rel))
                        Directory.CreateDirectory(Path.Combine(Destination, rel));
                }
                Interlocked.Increment(ref walking[0]);
                dirs.Enqueue((entry, depth + 1));
            }
            else if (File.Exists(entry))
            {
                if (Masked(name, _options.ExcludeFileMasks))
                    continue;
                var rel = Rel(Source, entry);
                OfferFile(entry, rel);
            }
        }
    }

    void OfferFile(string path, string rel)
    {
        FileInfo info;
        try
        {
            info = new FileInfo(path);
        }
        catch (IOException)
        {
            return;
        }
        if (_options.MinSizeBytes is { } min && info.Length < min)
            return;
        if (_options.MaxSizeBytes is { } max && info.Length > max)
            return;
        var write = info.LastWriteTimeUtc;
        if (_options.MinAge is { } minAge && write > minAge.CutoffUtc())
            return;
        if (_options.MaxAge is { } maxAge && write < maxAge.CutoffUtc())
            return;
        var bucket = BucketFor(info.Length);
        _queues[(int)bucket].Enqueue(new WorkItem(path, rel, info.Length, bucket, write, info.Attributes));
        lock (_gate)
        {
            var b = Progress.Buckets[(int)bucket];
            b.Found++;
            b.Queued++;
            b.BytesFound += info.Length;
            Progress.FilesFound++;
            Progress.BytesFound += info.Length;
            _walked++;
            Progress.CertaintyPercent = Math.Min(99, Progress.FilesFound == 0 ? 0 : 50);
        }
        if (_walked % 8 == 0)
            Emit();
    }

    async Task ConsumeAsync(ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var tasks = new List<Task>();
        foreach (var row in BucketTable)
        {
            for (var i = 0; i < row.Workers; i++)
                tasks.Add(TakeAsync(row.Id, destIndex, token));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    async Task TakeAsync(FileIoBucket bucket, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        while (!_cancelled && !token.IsCancellationRequested)
        {
            await GateAsync().ConfigureAwait(false);
            if (_cancelled || token.IsCancellationRequested)
                return;
            if (bucket == FileIoBucket.Huge && _queues[0].Count + _queues[1].Count >= HugeWatermark)
            {
                await Task.Delay(15, CancellationToken.None).ConfigureAwait(false);
                continue;
            }
            if (!_queues[(int)bucket].TryDequeue(out var item))
            {
                if (_reconComplete)
                    return;
                await Task.WhenAny(_reconDone?.Task ?? Task.CompletedTask, Task.Delay(15, CancellationToken.None)).ConfigureAwait(false);
                continue;
            }
            var b = Progress.Buckets[(int)bucket];
            lock (_gate)
            {
                b.Queued = _queues[(int)bucket].Count;
                b.Active++;
            }
            Emit();
            try
            {
                await HandleAsync(item, destIndex, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                lock (_gate) b.Active--;
            }
            MaybeProgressLog();
        }
    }

    async Task HandleAsync(WorkItem item, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var retries = Math.Max(0, _options.RetryCount);
        for (var attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                await GateAsync().ConfigureAwait(false);
                if (_cancelled || token.IsCancellationRequested)
                    return;
                if (Verb == FileIoVerb.Delete)
                {
                    await DoDeleteAsync(item).ConfigureAwait(false);
                    return;
                }
                await DoCopyAsync(item, destIndex, token).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == retries)
                {
                    Fail(item, ex is UnauthorizedAccessException ? "Unauthorized" : "Unavailable");
                    if (_options.StopOnError)
                        Cancel();
                    return;
                }
                await Task.Delay(_options.RetryWait, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    Task DoDeleteAsync(WorkItem item)
    {
        var sub = HelperLog.Subcategories.Delete;
        if (_options.AuditMode)
        {
            FileIoLog.Success(sub, $"WouldDelete path={item.SourcePath} size={item.Size}");
            Done(item);
            return Task.CompletedTask;
        }
        if (_options.Shred is { } recipe)
        {
            FileIoLog.Pending(HelperLog.Subcategories.SecureDelete, $"Shred path={item.SourcePath} passes={recipe}");
            FileIoHelper.SecureDelete(item.SourcePath, recipe);
        }
        else if (File.Exists(item.SourcePath))
            File.Delete(item.SourcePath);
        Done(item);
        return Task.CompletedTask;
    }

    async Task DoCopyAsync(WorkItem item, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var destIsDir = !_sourceIsFile || Directory.Exists(Destination);
        var destPath = destIsDir ? Path.Combine(Destination, item.RelativePath) : Destination;
        if (_options.CopyOnlyUniqueContent)
        {
            var digest = HashingHelper.HashFile(item.SourcePath, _options.HashAlgorithm);
            if (destIndex.TryGetValue(digest, out var hit))
            {
                FileIoLog.Success(HelperLog.Subcategories.Index, $"SkipDuplicate digest={digest[..Math.Min(12, digest.Length)]}… name={Path.GetFileName(item.SourcePath)} matched={hit}");
                Skip(item);
                return;
            }
        }

        var finalPath = destPath;
        var exists = File.Exists(destPath);
        if (exists)
        {
            if (_options.Collision == FileIoCollision.Skip)
            {
                FileIoLog.Success(VerbSub(), $"Skip path={destPath}");
                Skip(item);
                return;
            }
            if (_options.Collision == FileIoCollision.UniqueName)
            {
                var parent = Path.GetDirectoryName(destPath) ?? Destination;
                var names = Directory.Exists(parent)
                    ? Directory.GetFiles(parent).Select(Path.GetFileName).OfType<string>().ToArray()
                    : [];
                var next = UniqueName.Next(names, Path.GetFileName(item.SourcePath), _options.UniqueNamePattern);
                if (next is null)
                {
                    FileIoLog.Failed(VerbSub(), $"NameCap path={destPath}");
                    Fail(item, "NameCap");
                    return;
                }
                finalPath = Path.Combine(parent, next);
                FileIoLog.Success(VerbSub(), $"UniqueName from={Path.GetFileName(item.SourcePath)} to={next}");
            }
            else
                FileIoLog.Warning(VerbSub(), $"Overwrite path={destPath}");
        }

        if (_options.AuditMode)
        {
            var would = exists && _options.Collision == FileIoCollision.UniqueName ? "WouldUniqueName" : "WouldCopy";
            FileIoLog.Success(VerbSub(), $"{would} path={finalPath} size={item.Size}");
            Done(item);
            return;
        }

        var parentDir = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrEmpty(parentDir))
            Directory.CreateDirectory(parentDir);

        var created = !File.Exists(finalPath);
        await CopyStreamAsync(item.SourcePath, finalPath, token, created).ConfigureAwait(false);
        if (_cancelled || token.IsCancellationRequested)
            return;
        if (_options.CopyTimestampsAndAttributes)
        {
            File.SetLastWriteTimeUtc(finalPath, item.WriteTimeUtc);
            File.SetAttributes(finalPath, item.Attributes);
        }
        if (created)
            _created[finalPath] = 0;
        if (_options.CopyOnlyUniqueContent)
        {
            try
            {
                destIndex[HashingHelper.HashFile(finalPath, _options.HashAlgorithm)] = finalPath;
            }
            catch (IOException)
            {
                /* index is best-effort */
            }
        }
        if (Verb == FileIoVerb.Move && File.Exists(item.SourcePath))
            File.Delete(item.SourcePath);
        Done(item);
        NoteRate(item.Size);
    }

    async Task CopyStreamAsync(string source, string dest, CancellationToken token, bool created)
    {
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        try
        {
            await using var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            int read;
            while ((read = await src.ReadAsync(buffer.AsMemory(0, StreamBufferSize), token).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                await GateAsync().ConfigureAwait(false);
                if (_cancelled || token.IsCancellationRequested)
                {
                    await dst.DisposeAsync().ConfigureAwait(false);
                    if (created)
                    {
                        try { File.Delete(dest); } catch (IOException) { }
                    }
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (created)
            {
                try { File.Delete(dest); } catch (IOException) { }
            }
            throw;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    int PurgeDest()
    {
        if (!Directory.Exists(Destination) || !Directory.Exists(Source))
            return 0;
        FileIoLog.Warning(HelperLog.Subcategories.Mirror, $"Purge start dest={Destination}");
        var srcFiles = new HashSet<string>(
            SafeEnumerateFiles(Source).Select(p => Rel(Source, p)),
            StringComparer.OrdinalIgnoreCase);
        var n = 0;
        foreach (var f in SafeEnumerateFiles(Destination).ToArray())
        {
            var rel = Rel(Destination, f);
            if (srcFiles.Contains(rel))
                continue;
            if (_options.AuditMode)
                FileIoLog.Success(HelperLog.Subcategories.Delete, $"WouldDelete extra={f}");
            else
            {
                File.Delete(f);
                FileIoLog.Success(HelperLog.Subcategories.Delete, $"Purge extra={f}");
            }
            n++;
        }
        return n;
    }

    async Task GateAsync()
    {
        while (_paused && !_cancelled)
            await Task.Delay(15, CancellationToken.None).ConfigureAwait(false);
    }

    void Emit()
    {
        _options.Progress?.Report(Progress);
        ProgressChanged?.Invoke(this, Progress);
    }

    void MaybeProgressLog()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastProgressLog < TimeSpan.FromSeconds(15))
            return;
        _lastProgressLog = now;
        FileIoLog.Success(
            HelperLog.Subcategories.Progress,
            $"Progress job={JobId} done={Progress.FilesDone}/{Progress.FilesFound} bytes={Progress.BytesDone} certainty={Progress.CertaintyPercent}");
    }

    void Skip(WorkItem item)
    {
        lock (_gate)
        {
            Progress.FilesSkipped++;
            Progress.BytesDone += item.Size;
            var b = Progress.Buckets[(int)item.Bucket];
            b.Skipped++;
            b.BytesDone += item.Size;
        }
        Emit();
    }

    void Fail(WorkItem item, string reason)
    {
        lock (_gate)
        {
            Progress.FilesFailed++;
            Progress.Buckets[(int)item.Bucket].Failed++;
        }
        FileIoLog.Failed(VerbSub(), $"Failed path={item.SourcePath} reason={reason}");
        Emit();
    }

    void Done(WorkItem item)
    {
        lock (_gate)
        {
            Progress.FilesDone++;
            Progress.BytesDone += item.Size;
            var b = Progress.Buckets[(int)item.Bucket];
            b.Done++;
            b.BytesDone += item.Size;
        }
        Emit();
    }

    void NoteRate(long bytes)
    {
        var now = DateTimeOffset.UtcNow;
        var dt = Math.Max(0.001, (now - _lastRateAt).TotalSeconds);
        Progress.RateBytesPerSec = _options.AuditMode ? 0 : (long)(bytes / dt);
        _lastRateAt = now;
        if (Progress.CertaintyPercent == 100 && Progress.BytesFound > Progress.BytesDone && Progress.RateBytesPerSec > 0)
        {
            var remain = Progress.BytesFound - Progress.BytesDone;
            Progress.EtaUtc = now.AddSeconds(remain / (double)Progress.RateBytesPerSec);
        }
    }

    string VerbSub() => Verb switch
    {
        FileIoVerb.Move => HelperLog.Subcategories.Move,
        FileIoVerb.Delete => HelperLog.Subcategories.Delete,
        FileIoVerb.Mirror => HelperLog.Subcategories.Mirror,
        _ => HelperLog.Subcategories.Copy,
    };

    string VerbLabel() => Verb switch
    {
        FileIoVerb.Move => "Move",
        FileIoVerb.Delete => "Delete",
        FileIoVerb.Mirror => "Mirror",
        _ => "Copy",
    };

    internal static FileIoBucket BucketFor(long size)
    {
        foreach (var row in BucketTable)
        {
            if (size <= row.Max)
                return row.Id;
        }
        return FileIoBucket.Huge;
    }

    static bool Masked(string name, IReadOnlyList<string> masks)
    {
        foreach (var mask in masks)
        {
            var body = Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".");
            if (Regex.IsMatch(name, "^" + body + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return true;
        }
        return false;
    }

    static string Rel(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(path);
        if (full.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
            return "";
        if (full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return full[(fullRoot.Length + 1)..];
        return Path.GetFileName(path);
    }

    static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        if (!Directory.Exists(root))
            yield break;
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] files;
            string[] dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            foreach (var f in files)
                yield return f;
            foreach (var d in dirs)
                stack.Push(d);
        }
    }

    readonly record struct WorkItem(string SourcePath, string RelativePath, long Size, FileIoBucket Bucket, DateTime WriteTimeUtc, FileAttributes Attributes);
}
