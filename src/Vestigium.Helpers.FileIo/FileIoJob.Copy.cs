            int read;
            while ((read = await src.ReadAsync(buffer.AsMemory(0, StreamBufferSize), token).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                committed += read;
                NoteBytes(relative, committed);
                await GateAsync().ConfigureAwait(false);
                if (!_cancelled && !token.IsCancellationRequested) continue;
                await dst.DisposeAsync().ConfigureAwait(false);
                DropOwnedDest(dest, deleteOnCancel);
                return;
            }