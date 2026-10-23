    private async Task DoCopyAsync(WorkItem item, ConcurrentDictionary<string, string> destIndex, CancellationToken token)
    {
        Enter(item);
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
                Leave(item);
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
                Leave(item);
                return;
            }
            if (plan.Fail)
            {
                FileIoLog.NameCap(VerbSub(), JobId, FileIoLog.Props(("path", destPath)));
                Fail(item, "NameCap");
                Observe(item, "Fail");
                Stay(item, "NameCap");
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
            Leave(item);
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
        await CopyStreamAsync(item.SourcePath, finalPath, token, resume, deleteOnCancel, item.RelativePath).ConfigureAwait(false);
        sw.Stop();
        if (_cancelled || token.IsCancellationRequested)
        {
            Leave(item);
            return;
        }
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
        Leave(item);
    }

    private async Task CopyStreamAsync(string source, string dest, CancellationToken token, bool resume, bool deleteOnCancel, string relative)
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
                committed += read;
                NoteBytes(relative, committed);
                Emit();
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