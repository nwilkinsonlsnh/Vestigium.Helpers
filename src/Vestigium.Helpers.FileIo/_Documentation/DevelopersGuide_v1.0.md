# Vestigium.Helpers.FileIo — Developers Guide

**Document ID:** VEST-HLP-FILEIO-DEV-000  
**Version:** 1.0  
**Status:** Accepted with SRS v1.0.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.FileIo/`.

## Read first

Robocopy is the behavior reference, not a process we spawn. Category `Helpers`, APPID `FileIo`. The class library never calls `VestigiumLogger.Initialize`.

```csharp
var job = FileIoJob.Copy(@"D:\Export", @"E:\Archive", new FileIoJobOptions
{
    Collision = FileIoCollision.UniqueName,   // default
    ReconLeadTime = TimeSpan.FromSeconds(15), // default
    AuditMode = false
});
job.ProgressChanged += (_, p) => ui.Render(p);
await job.RunAsync(cancellation);
```

## Locked

- Lead time default 15 s, range 0–180 s. Values above 180 throw.
- Certainty is 100% only when recon is complete. Progress is job-wide and per bucket.
- Live `FileIoProgress` is chatty. JSONL is sparse.
- Product name for the no-write pass is **Audit Mode**.
- Default collision is UniqueName (`.##`). Cap is `NameCap`, never overwrite.
- Pause resumes committed bytes. Cancel aborts the current write and removes a dest this job created.
- Same recon team and buckets for delete.
- Stream 64 KiB. Never `File.ReadAllBytes` on a payload.
- LAD, scheduler, admin, ACL copy: later.

## UniqueName

Numeric `.##` → `report.01.txt`. Alpha `A##` → `report.A01.txt` then `A02` … `A99` then `B01`. Width does not shrink. Cap fails the file.

## Engine

Recon workers cap 8. Buckets: Tiny 0–256 KiB (8), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1, Tiny+Small queued < 32).

## Sibling

Hashing for digests. Encryption for envelopes. Logging for JSONL. FileIo does not absorb those façades.

The WPF gallery (`Vestigium.Helpers.FileIo.Demo`) and the web gallery both host APPID FileIo. Probe is `%TEMP%` only.

WPF tabs: Overview, Copy, Move, Delete, Mirror, Audit Mode, UniqueName, Compare, Index, JSONL. Demo volumes live under `%TEMP%\Vestigium.Helpers.FileIo.Demo`. Default collision UniqueName. Gallery recon lead is 2 s (library default remains 15 s).
