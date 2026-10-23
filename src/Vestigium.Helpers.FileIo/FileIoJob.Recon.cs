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
                OfferOne(Source, Path.GetFileName(Source));
                return;
            }

            if (_options is { IncludeEmptyDirectories: true, AuditMode: false } && Verb != FileIoVerb.Delete)
                Directory.CreateDirectory(Destination);

            using var dirs = new BlockingCollection<(string Path, int Depth)>();
            var seed = new List<(string Path, int Depth)>();
            var files = new List<WorkItem>();
            WalkDirectory(Source, 0, seed, files);
            Offer(files);
            if (seed.Count == 0 || _cancelled || token.IsCancellationRequested)
                return;

            var walking = seed.Count;
            foreach (var node in seed)
                dirs.Add(node);

            var workers = Agents();
            var tasks = new Task[workers];
            for (var i = 0; i < workers; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    while (!_cancelled && !token.IsCancellationRequested)
                    {
                        if (!dirs.TryTake(out var node, Timeout.Infinite))
                            return;
                        var found = new List<(string Path, int Depth)>();
                        var batch = new List<WorkItem>();
                        try
                        {
                            WalkDirectory(node.Path, node.Depth, found, batch);
                            foreach (var child in found)
                            {
                                Interlocked.Increment(ref walking);
                                dirs.Add(child);
                            }
                            Offer(batch);
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref walking) == 0)
                                dirs.CompleteAdding();
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
        catch (InvalidOperationException)
        {
            /* queue completed while an agent was finishing */
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

    private int Agents()
    {
        var asked = _options.ReconWorkers;
        var cores = Math.Max(4, Environment.ProcessorCount);
        var n = asked > 0 ? asked : cores;
        return Math.Clamp(n, 2, 32);
    }

    private void WalkDirectory(string dir, int depth, List<(string Path, int Depth)> children, List<WorkItem> files)
    {
        DirectoryInfo folder;
        try
        {
            folder = new DirectoryInfo(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileIoLog.ItemUnauthorized(FileIoLog.Subcategories.Recon, JobId, FileIoLog.Props(("path", dir)));
            return;
        }

        IEnumerable<FileSystemInfo> entries;
        try
        {
            entries = folder.EnumerateFileSystemInfos();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileIoLog.ItemUnauthorized(FileIoLog.Subcategories.Recon, JobId, FileIoLog.Props(("path", dir)));
            return;
        }

        foreach (var info in entries)
        {
            if (_cancelled)
                return;
            var name = info.Name;
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                if (Masked(name, _options.ExcludeDirectoryMasks))
                    continue;
                if (_options.MaxDepth is { } max && depth + 1 >= max)
                    continue;
                if (_options is { IncludeEmptyDirectories: true, AuditMode: false } && Verb != FileIoVerb.Delete)
                {
                    var rel = Rel(Source, info.FullName);
                    if (!string.IsNullOrEmpty(rel))
                        Directory.CreateDirectory(Path.Combine(Destination, rel));
                }
                children.Add((info.FullName, depth + 1));
                continue;
            }

            if (info is not FileInfo file)
                continue;
            if (Masked(name, _options.ExcludeFileMasks))
                continue;
            var item = Item(file, Rel(Source, file.FullName));
            if (item is { } kept)
                files.Add(kept);
        }
    }

    private void OfferOne(string path, string rel)
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
        var item = Item(info, rel);
        if (item is { } kept)
            Offer([kept]);
    }

    private WorkItem? Item(FileInfo file, string rel)
    {
        long size;
        DateTime write;
        FileAttributes attr;
        try
        {
            size = file.Length;
            write = file.LastWriteTimeUtc;
            attr = file.Attributes;
        }
        catch (IOException)
        {
            return null;
        }
        if (_options.MinSizeBytes is { } min && size < min)
            return null;
        if (_options.MaxSizeBytes is { } max && size > max)
            return null;
        if (_options.MinAge is { } minAge && write > minAge.CutoffUtc())
            return null;
        if (_options.MaxAge is { } maxAge && write < maxAge.CutoffUtc())
            return null;
        return new WorkItem(file.FullName, rel, size, BucketFor(size), write, attr);
    }

    private void Offer(List<WorkItem> files)
    {
        if (files.Count == 0)
            return;
        lock (_gate)
        {
            foreach (var item in files)
            {
                _queues[(int)item.Bucket].Enqueue(item);
                var b = Progress.Buckets[(int)item.Bucket];
                b.Found++;
                b.Queued++;
                b.BytesFound += item.Size;
                Progress.FilesFound++;
                Progress.BytesFound += item.Size;
                _walked++;
            }
            Progress.CertaintyPercent = Math.Min(99, Progress.FilesFound == 0 ? 0 : 50);
        }
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
        Enter(item);
        try
        {
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
            NoteBytes(item.RelativePath, item.Size);
            sw.Stop();
            Done(item);
            Observe(item, "Done", sw.Elapsed);
            return Task.CompletedTask;
        }
        finally
        {
            Leave(item);
        }
    }
}
