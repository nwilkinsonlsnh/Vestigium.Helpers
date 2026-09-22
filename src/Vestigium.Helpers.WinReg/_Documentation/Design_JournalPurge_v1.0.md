# Vestigium.Helpers.WinReg — Journal Purge Design

**Document ID:** VEST-HLP-WINREG-DSN-PURGE-000  
**Version:** 1.0  
**Date:** 15 September 2026

## Options

```csharp
public sealed class RegistryPurgeOptions
{
    public int? KeepLastBatches { get; init; }
    public TimeSpan? OlderThan { get; init; }
    public bool UndoneOnly { get; init; }
    public string? BatchId { get; init; }
    public string? ArchivePath { get; init; }
    public bool DryRun { get; init; }
    public bool Confirm { get; init; }
}
```

`PurgeJournal(path, confirm)` remains delete-all.

`PurgeJournal(path, options)` compact-rewrites unless the filter removes every batch — then delete the file.

## Algorithm

1. Deny if `confirm` is false (even dry run can run without confirm — **dry run does not require confirm**, compact does).
2. `ReadInfo` + scan lines into batches (header, batch, muts, batch-end, mut-undo).
3. Mark each committed batch keep/drop:
   - If `BatchId` set: drop that id only.
   - Else start with all committed as candidates.
   - `KeepLastBatches`: drop all but newest N committed.
   - `OlderThan`: drop if `startedAt` < UtcNow − X.
   - Both set: drop unless last N **and** young enough.
   - `UndoneOnly`: drop batch if every mut has a `mut-undo`.
   - Open batch at EOF: keep.
4. Dry run: return counts.
5. If `ArchivePath`: write a journal of **dropped** batches (new header, those batches only).
6. Write `path.tmp` header + kept batches + related mut-undo. Flush. Replace `path`.
7. If keep set is empty: delete `path` (same as purge all).

Do not load the journal for append while compacting. Caller must not hold `RegistryJournal` open on that path.
