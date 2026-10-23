                committed += read;
                NoteBytes(relative, committed);
                await GateAsync().ConfigureAwait(false);