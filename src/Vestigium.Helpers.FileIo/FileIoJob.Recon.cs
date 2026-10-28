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
            foreach (var root in _roots)
            {
                if (File.Exists(root))
                {
                    OfferOne(root, _roots.Length > 1 ? root : Path.GetFileName(root));
                    continue;
                }
                WalkDirectory(root, 0, seed, files);
            }
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
            var item = Item(file, _roots.Length > 1 ? file.FullName : Rel(Source, file.FullName));
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
        var shares = FileIoThreads.Split(_options.MaxThreads);
        Array.Copy(shares, _share, shares.Length);
        _destIndex = destIndex;
        PublishShares();
        var tasks = _workers;
        for (var bucket = 0; bucket < BucketTable.Length; bucket++)
        {
            for (var i = 0; i < shares[bucket]; i++)
                tasks.Add(TakeAsync(BucketTable[bucket].Id, destIndex, token));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private void MaybeDonate()
    {
        if (!_reconComplete || _destIndex is null)
            return;
        var changed = false;
        changed |= Feed(1, 0);
        changed |= Feed(2, 0);
        if (Closed(0) && Closed(1) && Closed(2))
            changed |= Double(3);
        if (Closed(3))
            changed |= Double(4);
        if (!changed)
            return;
        PublishShares();
        Emit();
    }

    private bool Feed(int donor, int recipient)
    {
        if (_donated[donor] || !Closed(donor))
            return false;
        var gift = _share[donor];
        _share[donor] = 0;
        _share[recipient] += gift;
        _donated[donor] = true;
        for (var i = 0; i < gift; i++)
            _workers.Add(TakeAsync(BucketTable[recipient].Id, _destIndex!, _cts.Token));
        return true;
    }

    private bool Double(int bucket)
    {
        if (_donated[bucket] || _share[bucket] < 1)
            return false;
        var gift = _share[bucket];
        _share[bucket] = gift * 2;
        _donated[bucket] = true;
        for (var i = 0; i < gift; i++)
            _workers.Add(TakeAsync(BucketTable[bucket].Id, _destIndex!, _cts.Token));
        return true;
    }

    private bool Closed(int bucket)
    {
        var bin = Progress.Buckets[bucket];
        return bin.Found > 0 && bin.Done + bin.Skipped + bin.Failed >= bin.Found;
    }

    private void PublishShares()
    {
        for (var i = 0; i < _share.Length && i < Progress.Buckets.Length; i++)
            Progress.Buckets[i].Workers = _share[i];
    }

    private bool TrySteal(FileIoBucket home, out WorkItem item)
    {
        item = default!;
        var best = -1;
        var queued = -1;
        for (var i = 0; i < _queues.Length; i++)
        {
            if ((FileIoBucket)i == home)
                continue;
            var count = _queues[i].Count;
            if (count > queued)
            {
                best = i;
                queued = count;
            }
        }
        return best >= 0 && queued > 0 && _queues[best].TryDequeue(out item);
    }

    private async Task DrainDelete(FileIoBucket bucket, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        for (var n = 0; n < 64 && !_cancelled && !token.IsCancellationRequested; n++)
        {
            if (!_queues[(int)bucket].TryDequeue(out var more))
                return;
            await HandleAsync(more, destIndex, token).ConfigureAwait(false);
        }
    }

    private async Task TakeAsync(FileIoBucket bucket, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var home = Progress.Buckets[(int)bucket];
        lock (_gate)
            home.Active++;
        try
        {
            while (!_cancelled && !token.IsCancellationRequested)
            {
                await GateAsync().ConfigureAwait(false);
                if (_cancelled || token.IsCancellationRequested)
                    return;
                if (!_queues[(int)bucket].TryDequeue(out var item))
                {
                    var left = home.Found - home.Done - home.Skipped - home.Failed;
                    if (_reconComplete && left <= 0)
                        return;
                    await Task.WhenAny(_reconDone?.Task ?? Task.CompletedTask, Task.Delay(15, CancellationToken.None)).ConfigureAwait(false);
                    continue;
                }
                try
                {
                    await HandleAsync(item, destIndex, token).ConfigureAwait(false);
                    if (Verb == FileIoVerb.Delete)
                        await DrainDelete(bucket, destIndex, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                MaybeProgressLog();
            }
        }
        finally
        {
            lock (_gate)
                home.Active--;
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
            MakeWritable(item.SourcePath);
            if (_options.Shred is { } recipe)
            {
                FileIoLog.Pending(FileIoLog.Subcategories.SecureDelete, $"Shred path={item.SourcePath} passes={recipe}", JobId);
                var last = 0d;
                FileIoHelper.SecureDelete(item.SourcePath, recipe, fraction =>
                {
                    if (fraction - last < 0.01 && fraction < 1)
                        return;
                    last = fraction;
                    NoteBytes(item.RelativePath, (long)(item.Size * Math.Clamp(fraction, 0, 1)));
                });
            }
            else if (File.Exists(item.SourcePath))
                File.Delete(item.SourcePath);
            NoteBytes(item.RelativePath, item.Size);
            sw.Stop();
            Done(item);
            Observe(item, "Done", sw.Elapsed);
            Leave(item);
            return Task.CompletedTask;
        }
        catch
        {
            throw;
        }
    }

    private static void MakeWritable(string path)
    {
        try
        {
            var attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            /* delete will report the failure */
        }
    }
}
