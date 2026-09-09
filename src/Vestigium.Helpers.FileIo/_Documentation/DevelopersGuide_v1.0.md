# Vestigium.Helpers.FileIo — Developers Guide

**Document ID:** VEST-HLP-FILEIO-DEV-000  
**Version:** 1.1  
**Status:** Accepted with SRS v1.1. Phase 5 hardening is the running engine.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md) is the phase map. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.FileIo/`.

## Host a job

```csharp
var job = FileIoJob.Copy(@"D:\Export", @"E:\Archive", new FileIoJobOptions
{
    Collision = FileIoCollision.UniqueName,   // default
    ReconLeadTime = TimeSpan.FromSeconds(15), // default
    RetryCount = 3,
    RetryWait = TimeSpan.FromSeconds(2),
    RequestedBy = "wilkinson",                // ≤ 50, no PEM / long hex
    Reason = "export-archive",                // ≤ 80
    AuditMode = false
});
job.ProgressChanged += (_, p) => ui.Render(p);
var result = await job.RunAsync(cancellation);
var sizes = result.Stats?.FileSizes;           // five-number, P95, mean CI
var series = sizes?.Series;                    // NumericSeries, or null if n = 0
var tiny = result.Stats?.Buckets[(int)FileIoBucket.Tiny];
```

The class library never calls `VestigiumLogger.Initialize`. The host does (`HelperWpfHost` or `HelperLog.InitializeHost`). Category `Helpers`, APPID `FileIo`.

## How recon works

One coordinator per `FileIoJob`. Recon walks the source on several workers (2–8). Each file is assigned a size bucket once and never moves. Consumers wait until recon finishes **or** `ReconLeadTime` elapses (default 15 s, hard cap 180 s). After that, recon may still be walking while Tiny files already copy.

Certainty is not a fake percent. It is 100 only when every recon worker is finished. Until then the painted job percent (`BytesDone / BytesFound`) is allowed to **drop** when recon finds more files. Show `CertaintyPercent` next to the bar so an operator does not call 90% “done” at 20% certainty. `EtaUtc` stays null until certainty is 100.

Buckets (v1, fixed): Tiny 0–256 KiB (8 workers), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1, and only while Tiny+Small queued < 32).

## Two channels

Live `FileIoProgress` is chatty (UI). JSONL is sparse: job start (full recipe, optional `by=` / `reason=`), recon start/complete, consumers released, pause/resume/cancel, exception decisions only, a Progress line every 15 s, Stats at finalize, job end. Do not log a quiet Success per file. Never log payload bytes or pass `Exception` into HelperLog.

## Audit Mode

Set `AuditMode = true` before a regulated copy. Recon and decisions run for real. Disk does not change. JSONL still contains WouldCopy / WouldUniqueName / WouldDelete / WouldSkipDuplicate. Use it to print the protocol, then run the same options with Audit Mode off.

## UniqueName

Default collision. Numeric `.##` → `report.01.txt`. Alpha `A##` → `report.A01.txt` then `A02` … `A99` then `B01`. Width does not shrink. Hitting the cap is `NameCap`: that item fails and the original dest is **not** overwritten. Skip and Overwrite are explicit.

## Pause, Cancel, retry

Pause finishes the current 64 KiB buffer and parks. Resume continues that stream. Cancel aborts the current buffer. A dest this job **created** is deleted; an overwrite of a pre-existing dest is not deleted. `IOException` (including sharing) retries `RetryCount` times with `RetryWait`, then the item is Failed `InUse`. `UnauthorizedAccessException` is Failed `Unauthorized`. The job continues unless `StopOnError`. A retry of a dest this job owns resumes at dest length when that length is less than the source — it does not rewrite bytes 0..committed.

## Analytics

At finalize FileIo constructs `NumericSeries` snapshots (sizes always; rates only when bytes actually moved). Empty bins are Count = 0 and null statistics, never NaN. Audit Mode records sizes, not rates. Charts may draw those series; FileIo does not reference Charts.

## Locked

- Lead time default 15 s, range 0–180 s. Values above 180 throw.
- Certainty is 100% only when recon is complete.
- Product name for the no-write pass is **Audit Mode**.
- Stream 64 KiB. Never `File.ReadAllBytes` on a payload.
- Same recon team and buckets for delete.
- LAD, scheduler, admin, ACL copy: later.

## Sibling

Hashing for digests. Encryption for envelopes. Analytics for descriptors. Logging for JSONL.

The WPF gallery (`Vestigium.Helpers.FileIo.Demo`) hosts APPID FileIo. Probe is `%TEMP%` only. WPF tabs: Overview, Copy, Move, Delete, Mirror, Audit Mode, UniqueName, Compare, Index, Stats, JSONL. Demo volumes live under `%TEMP%\Vestigium.Helpers.FileIo.Demo`. Gallery recon lead is 2 s (library default remains 15 s).
