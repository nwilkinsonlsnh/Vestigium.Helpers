# Vestigium.Helpers.FileIo — Requirements Specification

**Document ID:** VEST-HLP-FILEIO-SRS-000  
**Version:** 1.1  
**Status:** Accepted. Replaces the 9 September 2026 v1.0 draft.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.FileIo`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Build plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)

If implementation and this file disagree, this file wins.

This library is not `robocopy.exe`. Robocopy is the *behavior reference* for user-mode copy, move, delete, include/exclude, size/age filters, retries, and mirror-with-purge. FileIo is the *validated job*: the same verbs, a recon team that makes progress honest, Pause/Cancel, and an ALCOA+ record written through `Vestigium.Logging`. It does not take administrative rights. It does not schedule itself.

---

## 0. How to read this document

It records:

- one façade (`FileIoHelper`) for file and directory Copy, Move, Delete, Compare, Prune, Index, SecureDelete, and Audit Mode
- one job engine shared by Copy, Move, and Delete: multithreaded **recon** fills size buckets; **consumers** drain those buckets
- a recon **lead time** (default 15 s, allowed 0–180 s) so a long job can estimate before bytes move, without waiting more than three minutes
- progress for the **whole job** and for **each bucket**, plus a **certainty** figure that becomes 100 % when every recon worker has finished
- two reporting channels that are easy to confuse and must stay apart (see §6)
- collision default **UniqueName** using the locked numeric / `A##` algorithm
- optional unique-**content** skip via `Vestigium.Helpers.Hashing`
- Audit Mode: run the recipe, write the full log, change nothing on disk
- HelperLog only; Category `Helpers`; APPID `FileIo`; registered FileIo subcategories
- no last-access (LAD) filters in v1
- no scheduler, no admin/backup mode, no ACL/owner/SACL copy in v1
- Analytics handoff at finalize: file-size and transfer-rate NumericSeries for the whole job and each bin

---

## 1. Purpose

Give every Vestigium host one way to copy, move, or delete files and trees so an operator in a GxP / GMP setting can show *what was intended* and *what happened*. Robocopy can move the bytes. It cannot produce this record.

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

This is not Encryption. Encryption still Seals envelopes. This is not Hashing. Hashing still produces digests. FileIo moves, lists, and accounts for paths.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Recon lead time | Default **15 seconds**. Allowed **0–180 seconds**. Values above 180 are rejected (`ArgumentOutOfRangeException`). There is no 300-second option. If recon finishes before the lead time, transfer starts immediately. |
| 2 | Certainty | Every progress snapshot carries `CertaintyPercent` (0–100) and `ReconComplete`. When all recon workers have reported in, the population is known and certainty is **100**. Until then certainty must not be painted as a finished percent. |
| 3 | Two channels | **Live progress** is the `FileIoProgress` object pushed to the host (gallery bar, bucket meters). **Audit log** is JSONL through HelperLog. The bar may update several times a second. The log must not. See §6. |
| 4 | Audit Mode | First-class. Same recon, same buckets, same decision lines, **zero mutations**. Full log of what would have been copied, renamed, skipped, or deleted. |
| 5 | Delete | Same recon team, same five buckets, same lead time, same progress/certainty. Action on a work item is delete or shred instead of copy. |
| 6 | Last access (LAD) | **Not v1.** `/MAXLAD` `/MINLAD` live on the later roadmap. |
| 7 | Logging door | `HelperLog` only. Category = `Helpers`. APPID = `FileIo`. Library never calls `VestigiumLogger.Initialize`. Subcategories in §8 must be registered or lines become `Uncategorized`. |
| 8 | Default collision | **UniqueName.** Not Skip, not Overwrite. Pattern default `.##`. Algorithm in §5.3. |
| 9 | Pause / Cancel / retry | Pause stops new work and finishes the current buffer, then resumes from the committed byte. Cancel aborts the current write **immediately** and deletes a destination this job created. Network / sharing failures use `RetryCount` / `RetryWait`. Resume mid-file when the partial dest belongs to this job. |
| 10 | Admin / scheduler / ACL | **Not v1.** No backup mode, no VSS, no `/COPY:SOU`, no Windows service, no job scheduler. |
| 11 | Engine | BCL `FileStream`, `Directory`, `EnumerationOptions`. Hashing sibling for digests. No shell copy, no `robocopy.exe` spawn. |
| 12 | Large files | Stream. 64 KiB buffer. Never `File.ReadAllBytes` / `File.ReadAllText` on a payload. |
| 13 | Names | Visible paths in logs. Never file contents. Digest hex is allowed when uniqueness ran. |
| 14 | Secure delete | Opt-in recipe of `Zero` and `Random` passes, 64 KiB, flush, then `File.Delete`. Default delete is a normal delete. SSD/flash is best-effort. |
| 15 | Buckets | Five, fixed in v1. Tiny 0–256 KiB (8 workers), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1, watermark). Huge starts only when Tiny+Small queued count is below **32**. |
| 16 | Unique content | Optional. Digest from `Vestigium.Helpers.Hashing`, default SHA-256. Name collision and content identity are different switches. |
| 17 | Dry meaning | The product name is **Audit Mode**, not "dry run." |
| 18 | Default retries | `RetryCount = 3`, `RetryWait = 2 seconds`. Not robocopy’s 1,000,000 / 30 s. |
| 19 | Analytics | FileIo is the **host**. It accumulates per-file size, duration, and bytes/sec, then at **Finalize** constructs `NumericSeries` snapshots via `Vestigium.Helpers.Analytics`. Job-wide **and** each of the five bins. Empty bins are null series, never NaN. Audit Mode still describes sizes; transfer-rate series is empty because no bytes moved. Charting stays in Charts. One sparse `Stats` JSONL line. |

---

## 3. Goals

**G1.** One façade (`FileIoHelper`) plus a runnable `FileIoJob` owns identity, Probe, and jobs.  
**G2.** Copy, Move, and Delete a file or a directory tree with the same engine.  
**G3.** Recon walks the tree on several workers, stats each file, drops a work item in a size bucket. Consumers start after lead time (or sooner if recon is already done).  
**G4.** Progress reports the whole job and each bucket: found, queued, active, done, skipped, failed, bytes, rate.  
**G5.** Certainty is 100 % only after recon completion.  
**G6.** Default name collision writes `name.##.ext` (or the caller pattern). Cap is hard: never wrap into overwrite.  
**G7.** Audit Mode produces the complete log of decisions without writing or deleting.  
**G8.** Pause and Cancel are first-class. Cancel does not finish the current file. Pause resumes committed bytes.  
**G9.** Logs meet ALCOA+ through HelperLog. Category `Helpers`, APPID `FileIo`.  
**G10.** Permission failures are logged and do not require elevation. The job continues unless `StopOnError` is set.  
**G11.** `Probe` is in-memory / `%TEMP%` only. It must not write the Desktop and must not start a durable job.  
**G12.** Prune empty directories as an explicit finalize option.  
**G13.** Optional dest content index so "copy only unique files" means digest, not name.  
**G14.** At finalize, publish Analytics snapshots of file sizes and transfer rates for the job and for each bucket so a host can summarize without inventing statistics.

---

## 4. Job engine

### 4.1 Coordinator

One coordinator per `FileIoJob`.

```
Start
  → Recon running, consumers idle
  → recon complete  OR  lead time elapsed
  → Transfer / Delete running; recon may still be walking
  → recon complete AND every bucket empty AND no worker in a file
  → Finalize (optional prune, index flush, summary)
  → Done
```

Pause freezes the coordinator between files (and at the next 64 KiB boundary of the active file). Cancel jumps to Finalize with status Cancelled.

### 4.2 Recon team

- Directory queue seeded with the source root. Destination is also walked when unique-content, mirror-purge, or Audit Mode needs a dest inventory.
- Recon workers: `min(4, max(2, ProcessorCount / 2))`, cap **8**.
- Each directory: list files and subdirectories (honor `/LEV`, exclude names, size, mtime age). Subdirectories go back on the queue. Files become work items `{ relativePath, size, bucket, writeTimeUtc, attributes }`.
- Digest is computed during recon only when unique-content or Compare was requested. Otherwise consumers may hash as needed.
- Recon is cancellable. A cancelled recon still transfers or reports what was already found; the job log says recon was incomplete.

### 4.3 Lead time

| Rule | Value |
|---|---|
| Name | `ReconLeadTime` |
| Default | 15 seconds |
| Range | 0–180 seconds inclusive |
| 0 | First work item may be consumed immediately |
| Recon finishes early | Consumers start at that instant |
| Clock hits lead time | Consumers start even if the tree is still being walked |
| Recon after start | Continues. Certainty stays below 100 until it finishes |
| Above 180 | Reject. Do not clamp in silence |

### 4.4 Buckets and consumers

| Bucket | Size | Workers |
|---|---|---|
| Tiny | 0–256 KiB | 8 |
| Small | 256 KiB–4 MiB | 4 |
| Medium | 4–32 MiB | 2 |
| Large | 32–256 MiB | 1 |
| Huge | >256 MiB | 1, and only if Tiny+Small queued < 32 |

Work items never change bucket after enqueue. Delete uses these exact bins.

### 4.5 Pause, Cancel, retry, mid-file resume

**Pause.** No new file is started. The active worker finishes the current 64 KiB buffer, records `bytesCommitted`, and parks. Resume continues that file at `bytesCommitted`. Logged once (`Job` / Warning / `Paused`).

**Cancel.** The active write stops in the current buffer. If this job created the destination, the helper deletes that dest. Source is not deleted on a cancelled Copy. A cancelled Move that already copied a file and had not yet deleted the source leaves the source and removes the unfinished dest. Logged `Job` / Failed / `Cancelled`.

**Retry.** On `IOException` / sharing / transient network: wait `RetryWait`, retry up to `RetryCount`. After the last failure the work item is Failed and the job continues unless `StopOnError`.

**Mid-file resume.** A paused or retried copy does not start the file over when `bytesCommitted` matches dest length. After process crash, v1 does not auto-restart jobs.

### 4.6 Audit Mode

`AuditMode = true`:

- Recon runs for real.
- Consumers **do not** create, overwrite, rename, delete, or shred.
- Each work item still receives a decision: WouldCopy, WouldUniqueName, WouldSkipDuplicate, WouldDelete, WouldFail, WouldPrune.
- HelperLog writes the same subcategories and the same summary shape as a live job.
- Progress certainty still goes to 100 when recon completes.

---

## 5. Features (v1)

### 5.1 File and directory verbs

| Verb | File | Directory |
|---|---|---|
| Copy | Yes | Yes (`IncludeEmptyDirectories` on/off = robocopy `/E` vs `/S`) |
| Move | Yes | Yes (copy then delete source after that file succeeds) |
| Delete | Yes | Yes (optional SecureDelete on children) |
| Compare | Yes (two paths, chosen hash) | Not a tree-diff product in v1 |
| Prune empty dirs | — | Yes |
| SecureDelete | Yes | Children then remove empty dirs |

`MaxDepth` is robocopy `/LEV:n`. Omitted means unlimited. `1` is the root’s files only.

### 5.2 Collision

```text
enum FileIoCollision { UniqueName = 0, Skip = 1, Overwrite = 2 }
```

Default is **UniqueName**.

- **Skip** — dest name exists → do not write, log Skip.
- **Overwrite** — dest name exists → replace contents, log Overwrite.
- **UniqueName** — dest name exists → mint the next name per §5.3, log UniqueName `from=report.txt to=report.01.txt`.

UniqueName is about **names**. Unique-content (`CopyOnlyUniqueContent`) is about **digests**. Both may be on.

### 5.3 UniqueName algorithm

Two pattern families. Default pattern string is `.##`.

**Numeric** — `name.##.ext`

- `report.txt` → `report.01.txt`, `report.02.txt`, …
- Count of `#` is the minimum width and the cap (`##` stops at 99, `###` at 999).
- Scan existing siblings once per directory, take max used + 1.
- Hitting the cap: that file **fails** (`NameCap`). It is never overwritten.

**Alpha-numeric** — `name.A##.ext`

- Letter A–Z, then the numeric width, **fixed width the whole way**.
- `A01`…`A99`, then `B01`…`B99`, … `Z99`.
- After `Z99` (2,574 names for width 2): `NameCap`. Stop.
- `B1` after `A99` is forbidden.

The pattern is applied to the file stem. Extension stays. Copy tree into an existing dest root is a merge, not a rename of the root.

### 5.4 Filters (v1)

| Option | Robocopy cousin | Notes |
|---|---|---|
| `IncludeEmptyDirectories` | `/E` vs `/S` | Default false (`/S` behavior) |
| `MaxDepth` | `/LEV:n` | |
| `ExcludeFileMasks` | `/XF` | Name / wildcard |
| `ExcludeDirectoryMasks` | `/XD` | |
| `MinSizeBytes` / `MaxSizeBytes` | `/MIN` `/MAX` | |
| `MinAge` / `MaxAge` | `/MINAGE` `/MAXAGE` | mtime. Days or a date. Not last-access |
| `RetryCount` / `RetryWait` | `/R` `/W` | Default 3 / 2 s |
| `CopyTimestampsAndAttributes` | `/COPY:DAT` | BCL last-write + attributes. No ACL |

Not in v1: LAD, archive-bit choreography, `/COPY:SOU`, `/B`, alternate streams.

### 5.5 Unique content and index

When `CopyOnlyUniqueContent = true`:

1. Build or load a dest index (relative path, size, mtime, digest).
2. Hash source with `HashingAlgorithm` (default SHA-256) via `Vestigium.Helpers.Hashing`.
3. Dest already has that digest → SkipDuplicate.
4. Otherwise copy (and UniqueName if the name is busy).
5. After a successful copy, add the dest row to the index.

Index files live under `%ProgramData%\Vestigium\FileIo\Indexes\`. Default **keep**. `CleanIndex` and `CleanIndexesOlderThan` are explicit.

### 5.6 SecureDelete recipe

Opt-in `Zero` / `Random` passes, 64 KiB, flush, then `File.Delete`. Presets: `ThreeRandomThenZero`, `SevenRandomThenZero`, `ZeroRandomZero`. Default job delete does not shred. Never log contents. Encryption keeps Seal-side presets; FileIo is the general shredder.

### 5.7 Mirror (v1, purge explicit)

`Mirror` = copy tree with empty dirs included, then **if** `Purge = true` delete dest extras. Purge default **false**. First purge delete logs Warning on subcategory `Mirror`. Audit Mode + Purge logs WouldDelete and does not delete.

### 5.8 Compare

`CompareFiles(left, right, algorithm)` returns equal / not equal / missing, plus both hex digests. Uses Hashing.

### 5.9 Analytics handoff (v1.1)

FileIo does not invent statistics. It is a host of `Vestigium.Helpers.Analytics`.

During consume, each work item becomes a `FileIoTransferObservation`:

| Field | When |
|---|---|
| `SizeBytes` | Every Done and Skip (recon already knew the size) |
| `DurationMs`, `RateBytesPerSec` | Done, and only when this job actually moved or deleted bytes. `rate = size / max(1 ms, elapsed)` |
| neither duration nor rate | Audit Mode, Skip, Fail, cancelled mid-write |

At Finalize the job constructs:

- `file-size-bytes` — all Done/Skip sizes
- `transfer-rate-Bps` — positive rates only
- `duration-ms` — positive durations only
- the same three series **per bucket** (`file-size-bytes.Tiny` … `Huge`)

Empty input does not call `NumericSeries.From`. The snapshot is Count = 0, Series = null, descriptors null. Analytics A11 (empty throws) is honored by not constructing.

Each snapshot publishes the Analytics descriptor set the host already knows: five-number, mean, P90/P95/P99, stddev, CV, skewness, Tukey high-outlier count, mean 95% t-interval when n ≥ 2.

Job wall-clock rate (`BytesDone / elapsed`) is a single number on `FileIoJobStats`, not a series.

One JSONL line, subcategory `Stats`, at job end. Not a line per observation. Paths stay in the existing per-file decision lines; Stats never lists files.

`FileIoJobResult.Stats` is the handoff. Charts may later draw the series. FileIo does not reference Charts.

---

## 6. Progress — two channels

### 6.1 Live progress (the bar)

The job pushes `FileIoProgress` several times a second (250–500 ms). This is for a screen. It is **not** the regulated record.

The snapshot includes `Phase`, `IsPaused`, `ReconComplete`, `CertaintyPercent` (100 only when recon is complete), files/bytes found and done, skipped, failed, rate, `EtaUtc` (null until certainty is 100), and `Buckets[5]` with the same counts per bin.

Job percent is `BytesDone / BytesFound` (or files if bytes are 0). That fraction can **drop** when recon finds more files. The gallery shows certainty next to it.

Audit Mode still pumps this object. Rate stays 0.

### 6.2 Audit log (the record)

JSONL through HelperLog. Sparse.

- Job start: one `Job` Pending with the full recipe
- Recon: start; heartbeat every 10 seconds or every 10 % of known directories; recon complete
- Transfer / delete start: one line when consumers are released
- Pause / Resume / Cancel: one line each
- Per file: only decisions (UniqueName, Skip, SkipDuplicate, Overwrite, Unauthorized, InUse, NameCap, Failed, Would*)
- One `Progress` line every 15 seconds while consumers run
- Job end: one Success, Failed, or Cancelled summary
- Stats: one line at finalize (`sizes n=… mean=… P50=… P95=… rates n=… meanBps=… P95Bps=… jobBps=…`)

Do not write a JSONL line per 64 KiB or per quiet Success on a large tree.

---

## 7. Public surface (v1)

```csharp
public static class FileIoHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.FileIo"
    public static string Probe();

    public static FileIoJob Copy(string source, string destination, FileIoJobOptions? options = null);
    public static FileIoJob Move(string source, string destination, FileIoJobOptions? options = null);
    public static FileIoJob Delete(string path, FileIoJobOptions? options = null);
    public static FileIoJob Mirror(string source, string destination, FileIoJobOptions? options = null);

    public static FileIoCompareResult CompareFiles(string left, string right,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256);

    public static int PruneEmptyDirectories(string root);
    public static void SecureDelete(string path, FileIoShredRecipe recipe);
    public static void CleanIndex(string destinationRoot);
    public static void CleanIndexesOlderThan(TimeSpan age);
}

public sealed class FileIoJob
{
    public string JobId { get; }
    public FileIoVerb Verb { get; }
    public FileIoJobOptions Options { get; }
    public FileIoProgress Progress { get; }
    public bool IsPaused { get; }
    public event EventHandler<FileIoProgress>? ProgressChanged;
    public Task<FileIoJobResult> RunAsync(CancellationToken cancellation = default);
    public void Pause();
    public void Resume();
}

public sealed class FileIoJobOptions
{
    public FileIoCollision Collision { get; init; } = FileIoCollision.UniqueName;
    public string UniqueNamePattern { get; init; } = ".##";
    public bool CopyOnlyUniqueContent { get; init; }
    public HashingAlgorithm HashAlgorithm { get; init; } = HashingAlgorithm.Sha256;
    public bool AuditMode { get; init; }
    public TimeSpan ReconLeadTime { get; init; } = TimeSpan.FromSeconds(15);
    public bool IncludeEmptyDirectories { get; init; }
    public int? MaxDepth { get; init; }
    public IReadOnlyList<string> ExcludeFileMasks { get; init; }
    public IReadOnlyList<string> ExcludeDirectoryMasks { get; init; }
    public long? MinSizeBytes { get; init; }
    public long? MaxSizeBytes { get; init; }
    public FileIoAge? MinAge { get; init; }
    public FileIoAge? MaxAge { get; init; }
    public int RetryCount { get; init; } = 3;
    public TimeSpan RetryWait { get; init; } = TimeSpan.FromSeconds(2);
    public bool CopyTimestampsAndAttributes { get; init; } = true;
    public bool Purge { get; init; }
    public bool PruneEmptyDirectories { get; init; }
    public bool StopOnError { get; init; }
    public FileIoShredRecipe? Shred { get; init; }
    public string? RequestedBy { get; init; }
    public string? Reason { get; init; }
    public IProgress<FileIoProgress>? Progress { get; init; }
}

public sealed class FileIoJobResult
{
    public required string JobId { get; init; }
    public required FileIoVerb Verb { get; init; }
    public required string Status { get; init; }
    public int Copied { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public int Deleted { get; init; }
    public long Bytes { get; init; }
    public bool AuditMode { get; init; }
    public FileIoJobStats? Stats { get; init; }
}
```

Rules: blank paths throw. `ReconLeadTime` outside 0–180 s throws. `UniqueNamePattern` must be `.#`…`#####` or `A#`…`A#####`. `RequestedBy` / `Reason` reject PEM / long hex / long Base64. Stream copy uses 64 KiB. Missing single-file source throws `FileNotFoundException` before the job starts.

---

## 8. Logging

Category = `Helpers`. APPID = `FileIo`. JSONL under `%ProgramData%\Vestigium\Logs\FileIo\`.

Subcategories that **must** be added to `HelperLog.Subcategories` and registered in `CreateTaxonomy()`: `Job`, `Recon`, `Copy`, `Move`, `Delete`, `Mirror`, `Index`, `Progress`, `Compare`, `Prune`, `SecureDelete`, `Stats` (plus existing `Probe`, `Identity`, `Guard`).

ALCOA+: attributable job id + optional `by=` / `reason=`; append-only JSONL; Pending then Success/Failed/Cancelled; no exception dumps; never file contents.

---

## 9. Demo contract

WPF gallery APPID `FileIo`. Tabs: Overview, Copy, Move, Delete, Mirror, Audit Mode, UniqueName, Compare, Index, Stats, JSONL. Show job certainty, job progress, five bucket meters, and after a job the Analytics snapshot (five-number, P95, mean CI, per-bucket table). Pause and Cancel buttons. Audit Mode fills Would* lines and does not change dest. Tests never use the real Desktop.

---

## 10. Tests

xUnit, temp directories only.

Identity, Probe, copy bytes, `/S` vs `/E`, MaxDepth, UniqueName sequence and NameCap, A## width, Skip, Overwrite, unique-content SkipDuplicate, lead time 0 and 181 rejected, certainty 100 after recon, five buckets on progress, Audit Mode dest unchanged, delete buckets, Pause/Resume mid-file, Cancel leaves no complete dest, SecureDelete, Mirror purge off/on, no payload bytes in logs, no EXCEPTION payload, Analytics sizes n matches Done+Skip, five populated bins on the demo seed, Audit Mode rate series empty, Stats subcategory registered, Stats JSONL line present.

---

## 11. Non-goals (v1)

LAD filters, scheduler, admin/`/B`/VSS, ACL copy, archive-bit flags, alternate streams, Directory Monitor, encrypting payload, inventing a hash, spawning `robocopy.exe`, crash auto-restart.

---

## 12. Roadmap

**v1.0** — engine, Copy/Move/Delete, UniqueName default, Audit Mode, Pause/Cancel, progress+certainty, taxonomy, index, SecureDelete, prune, Mirror+explicit Purge, size/mtime/depth/exclude, gallery, tests.

**v1.1** — Analytics handoff: file-size and transfer-rate NumericSeries for the job and each bin; `Stats` subcategory; Stats gallery tab.

**Later** — LAD, monitor, attribute flags, streams, configurable bins, scheduler, ACL only if a future elevation story is accepted.

**Never** — filter drivers, elevation as a v1 feature, `robocopy.exe`, logging contents, `ReadAllBytes` on a capture.

---

## 13. Siblings

Hashing for digests. Encryption for envelopes. Analytics for descriptors (FileIo is the host; it does not absorb the façade). Logging is the only log engine. Charts may draw the series FileIo publishes; FileIo does not reference Charts.

---

## 14. Glossary

Job, Recon, Lead time (0–180 s), Bucket, Certainty (100 means recon finished), Live progress, Audit log, Audit Mode, UniqueName, Unique content, Pause, Cancel, Purge, Analytics handoff, File size series, Transfer rate series.

---

## 15. Acceptance

This SRS is accepted when this file is on `main` under `src/Vestigium.Helpers.FileIo/_Documentation/`, and implementation of §7 + §9 + §10 follows without spawning robocopy and without inventing hashing or encryption APIs.

Phase 5 hardening: retries with mid-file resume, `by=` / `reason=` on the Job Pending line, InUse/Unauthorized item failures without elevation, injected index cleanup, sparse JSONL without payload or EXCEPTION dumps, and the §10 suite.
