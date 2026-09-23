using System.Diagnostics;
using System.Text.RegularExpressions;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.FileIo;

public sealed partial class FileIoJob
{
    private async Task DoCopyAsync(WorkItem item, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        var destIsDir = !_sourceIsFile || Directory.Exists(Destination);
        var destPath = destIsDir ? Path.Combine(Destination, item.RelativePath) : Destination;
        if (_options.CopyOnlyUniqueContent)
        {
            var digest = HashingHelper.HashFile(item.SourcePath, _options.HashAlgorithm);
            if (destIndex.TryGetValue(digest, out var hit))
            {
                FileIoLog.IndexHit(JobId, FileIoLog.Props(
                    ("digest", digest[..Math.Min(12, digest.Length)]),
                    ("name", Path.GetFileName(item.SourcePath)),
                    ("matched", hit)));
                Skip(item);
                Observe(item, "Skip");
                return;
            }
        }

        var finalPath = destPath;
        var exists = File.Exists(destPath);
        if (exists)
        {
            var parent = Path.GetDirectoryName(destPath) ?? Destination;
            var names = _options.Collision == FileIoCollision.UniqueName && Directory.Exists(parent)
                ? Directory.GetFiles(parent).Select(Path.GetFileName).OfType<string>().ToArray()
                : [];
            var plan = ResolveCollision(exists, _options.Collision, destPath, parent, Path.GetFileName(item.SourcePath), _options.UniqueNamePattern, names);
            if (plan.Skip)
            {
                FileIoLog.Decision(VerbSub(), "Skip", JobId, FileIoLog.Props(("path", destPath)));
                Skip(item);
                Observe(item, "Skip");
                return;
            }
            if (plan.Fail)
            {
                FileIoLog.NameCap(VerbSub(), JobId, FileIoLog.Props(("path", destPath)));
                Fail(item, "NameCap");
                Observe(item, "Fail");
                return;
            }
            finalPath = plan.FinalPath;
            if (plan.Unique)
                FileIoLog.Decision(VerbSub(), "UniqueName", JobId, FileIoLog.Props(
                    ("from", Path.GetFileName(item.SourcePath)),
                    ("to", Path.GetFileName(finalPath))));
            else
                FileIoLog.Decision(VerbSub(), "Overwrite", JobId, FileIoLog.Props(("path", destPath)));
        }

        if (_options.AuditMode)
        {
            var would = exists && _options.Collision == FileIoCollision.UniqueName ? "WouldUniqueName" : "WouldCopy";
            FileIoLog.Decision(VerbSub(), would, JobId, FileIoLog.Props(("path", finalPath), ("size", item.Size.ToString())));
            Done(item);
            Observe(item, "Done");
            return;
        }

        var parentDir = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrEmpty(parentDir))
            Directory.CreateDirectory(parentDir);

        var resume = _created.ContainsKey(finalPath);
        var deleteOnCancel = resume
            ? _created.TryGetValue(finalPath, out var flag) && flag == 1
            : !File.Exists(finalPath);
        _created[finalPath] = deleteOnCancel ? (byte)1 : (byte)0;
        var sw = Stopwatch.StartNew();
        await CopyStreamAsync(item.SourcePath, finalPath, token, resume, deleteOnCancel).ConfigureAwait(false);
        sw.Stop();
        if (_cancelled || token.IsCancellationRequested)
            return;
        if (_options.CopyTimestampsAndAttributes)
        {
            File.SetLastWriteTimeUtc(finalPath, item.WriteTimeUtc);
            File.SetAttributes(finalPath, item.Attributes);
        }
        RememberDest(finalPath, item.RelativePath, destIndex);
        if (Verb == FileIoVerb.Move && File.Exists(item.SourcePath))
            File.Delete(item.SourcePath);
        Done(item);
        Observe(item, "Done", sw.Elapsed);
        NoteRate(item.Size);
    }

    private async Task CopyStreamAsync(string source, string dest, CancellationToken token, bool resume, bool deleteOnCancel)
    {
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        try
        {
            await using var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var dst = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            long committed = 0;
            if (resume && dst.Length > 0 && dst.Length < src.Length)
                committed = dst.Length;
            if (committed > 0)
            {
                src.Seek(committed, SeekOrigin.Begin);
                dst.Seek(committed, SeekOrigin.Begin);
            }
            else
            {
                dst.SetLength(0);
            }

            int read;
            while ((read = await src.ReadAsync(buffer.AsMemory(0, StreamBufferSize), token).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                await GateAsync().ConfigureAwait(false);
                if (!_cancelled && !token.IsCancellationRequested) continue;
                await dst.DisposeAsync().ConfigureAwait(false);
                DropOwnedDest(dest, deleteOnCancel);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            DropOwnedDest(dest, deleteOnCancel);
            throw;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void DropOwnedDest(string dest, bool deleteOnCancel)
    {
        if (!deleteOnCancel)
            return;
        try { File.Delete(dest); } catch (IOException) { }
    }

    private int PurgeDest()
    {
        if (!Directory.Exists(Destination) || !Directory.Exists(Source))
            return 0;
        FileIoLog.Warning(FileIoLog.Subcategories.Mirror, $"Purge start dest={Destination}", JobId);
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
                FileIoLog.Decision(FileIoLog.Subcategories.Delete, "WouldDelete", JobId, FileIoLog.Props(("path", f)));
            else
            {
                File.Delete(f);
                FileIoLog.Decision(FileIoLog.Subcategories.Delete, "WouldDelete", JobId, FileIoLog.Props(("path", f), ("executed", "true")));
            }
            n++;
        }
        return n;
    }

    private async Task GateAsync()
    {
        while (_paused && !_cancelled)
            await Task.Delay(15, CancellationToken.None).ConfigureAwait(false);
    }

    private void Emit()
    {
        _options.Progress?.Report(Progress);
        ProgressChanged?.Invoke(this, Progress);
    }

    private void MaybeProgressLog()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastProgressLog < TimeSpan.FromSeconds(15))
            return;
        _lastProgressLog = now;
        FileIoLog.ProgressSnapshot(JobId, FileIoLog.Props(
            ("jobId", JobId),
            ("done", Progress.FilesDone.ToString()),
            ("found", Progress.FilesFound.ToString()),
            ("bytes", Progress.BytesDone.ToString()),
            ("certainty", Progress.CertaintyPercent.ToString())));
    }

    private void Skip(WorkItem item)
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

    private void Fail(WorkItem item, string reason)
    {
        lock (_gate)
        {
            Progress.FilesFailed++;
            Progress.Buckets[(int)item.Bucket].Failed++;
        }
        var failProps = FileIoLog.Props(("path", item.SourcePath), ("reason", reason));
        switch (reason)
        {
            case "NameCap":
                FileIoLog.NameCap(VerbSub(), JobId, failProps);
                break;
            case "Unauthorized":
                FileIoLog.ItemUnauthorized(VerbSub(), JobId, failProps);
                break;
            case "InUse":
                FileIoLog.ItemInUse(VerbSub(), JobId, failProps);
                break;
            default:
                FileIoLog.Failed(VerbSub(), reason, JobId, failProps);
                break;
        }
        Emit();
    }

    private void Done(WorkItem item)
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

    private void Observe(WorkItem item, string outcome, TimeSpan? elapsed = null)
    {
        double? durationMs = null;
        double? rate = null;
        if (elapsed is { } e && !_options.AuditMode && outcome == "Done")
        {
            durationMs = Math.Max(1, e.TotalMilliseconds);
            rate = item.Size * 1000.0 / durationMs.Value;
        }
        _observations.Add(new FileIoTransferObservation(
            item.Bucket,
            item.Size,
            DateTimeOffset.UtcNow,
            durationMs,
            rate,
            outcome));
    }

    private void NoteRate(long bytes)
    {
        var now = DateTimeOffset.UtcNow;
        var dt = Math.Max(0.001, (now - _lastRateAt).TotalSeconds);
        Progress.RateBytesPerSec = _options.AuditMode ? 0 : (long)(bytes / dt);
        _lastRateAt = now;
        if (Progress.CertaintyPercent != 100 || Progress.BytesFound <= Progress.BytesDone ||
            Progress.RateBytesPerSec <= 0) return;
        var remain = Progress.BytesFound - Progress.BytesDone;
        Progress.EtaUtc = now.AddSeconds(remain / (double)Progress.RateBytesPerSec);
    }

    private string VerbSub() => Verb switch
    {
        FileIoVerb.Move => FileIoLog.Subcategories.Move,
        FileIoVerb.Delete => FileIoLog.Subcategories.Delete,
        FileIoVerb.Mirror => FileIoLog.Subcategories.Mirror,
        _ => FileIoLog.Subcategories.Copy,
    };

    private string VerbLabel() => Verb switch
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

    internal readonly record struct CollisionPlan(string FinalPath, bool Skip, bool Fail, bool Unique, bool Overwrite);

    internal static CollisionPlan ResolveCollision(
        bool exists,
        FileIoCollision collision,
        string destPath,
        string parent,
        string sourceFileName,
        string uniqueNamePattern,
        IReadOnlyList<string> existingNames)
    {
        if (!exists)
            return new CollisionPlan(destPath, false, false, false, false);
        if (collision == FileIoCollision.Skip)
            return new CollisionPlan(destPath, true, false, false, false);
        if (collision == FileIoCollision.UniqueName)
        {
            var next = UniqueName.Next(existingNames, sourceFileName, uniqueNamePattern);
            if (next is null)
                return new CollisionPlan(destPath, false, true, false, false);
            return new CollisionPlan(Path.Combine(parent, next), false, false, true, false);
        }

        return new CollisionPlan(destPath, false, false, false, true);
    }

    private static bool Masked(string name, IReadOnlyList<string> masks)
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
        return full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full[(fullRoot.Length + 1)..] : Path.GetFileName(path);
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
