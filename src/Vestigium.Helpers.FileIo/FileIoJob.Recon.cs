using System.Collections.Concurrent;
using System.Diagnostics;

namespace Vestigium.Helpers.FileIo;

public sealed partial class FileIoJob
{
    private async Task ReconAsync(CancellationToken token)
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

    private void WalkDirectory(string dir, int depth, ConcurrentQueue<(string Path, int Depth)> dirs, int[] walking)
    {
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileIoLog.ItemUnauthorized(FileIoLog.Subcategories.Recon, JobId, FileIoLog.Props(("path", dir)));
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

    private void OfferFile(string path, string rel)
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

    private async Task ConsumeAsync(ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var tasks = new List<Task>();
        foreach (var row in BucketTable)
        {
            for (var i = 0; i < row.Workers; i++)
                tasks.Add(TakeAsync(row.Id, destIndex, token));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task TakeAsync(FileIoBucket bucket, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
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

    private async Task HandleAsync(WorkItem item, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
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
                    Fail(item, ex is UnauthorizedAccessException ? "Unauthorized" : "InUse");
                    Observe(item, "Fail");
                    if (_options.StopOnError)
                        Cancel();
                    return;
                }
                await Task.Delay(_options.RetryWait, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private Task DoDeleteAsync(WorkItem item)
    {
        const string sub = FileIoLog.Subcategories.Delete;
        if (_options.AuditMode)
        {
            FileIoLog.Decision(sub, "WouldDelete", JobId, FileIoLog.Props(("path", item.SourcePath), ("size", item.Size.ToString())));
            Done(item);
            Observe(item, "Done");
            return Task.CompletedTask;
        }
        var sw = Stopwatch.StartNew();
        if (_options.Shred is { } recipe)
        {
            FileIoLog.Pending(FileIoLog.Subcategories.SecureDelete, $"Shred path={item.SourcePath} passes={recipe}", JobId);
            FileIoHelper.SecureDelete(item.SourcePath, recipe);
        }
        else if (File.Exists(item.SourcePath))
            File.Delete(item.SourcePath);
        sw.Stop();
        Done(item);
        Observe(item, "Done", sw.Elapsed);
        return Task.CompletedTask;
    }
}
