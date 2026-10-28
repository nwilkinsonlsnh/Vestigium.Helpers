# Vestigium.Helpers.FileIo — Developers Guide

**Document ID:** VEST-HLP-FILEIO-DEV-200  
**Version:** 2.0  
**Status:** Accepted  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.FileIo` 1.1.0  

**Contract:** [`Requirements_v2.0.md`](Requirements_v2.0.md)  
**Design:** [`Design_v2.0.md`](Design_v2.0.md)  
**Historical guide:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

Open `Vestigium.Helpers.slnx`. Implementation: `src/Vestigium.Helpers.FileIo/`. There is no FileIo Demo project.

---

## 1. Add the package

```xml
<PackageReference Include="Vestigium.Helpers.FileIo" Version="1.1.0" />
```

Until Hashing 1.3.0 is on nuget.org, consume FileIo from this repository as a project reference. Do not restore FileIo 1.1.0 from nuget.org yet.

Also restore:

| Package | Version |
|---|---|
| Vestigium.Helpers.Analytics | 1.0.1 |
| Vestigium.Logging | 1.7.1 |
| Vestigium.Helpers.Hashing | 1.3.0 when published; else sibling project |

---

## 2. Initialize logging (host only)

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = FileIoCatalog.AppId;          // or the product APPID
    cfg.LogDirectory = logDir;                // tests: a TEMP folder
    cfg.MinimumDiskLevel = VestigiumLogLevel.Information;
    FileIoCatalog.Register(cfg);              // 12500–12610
    AnalyticsCatalog.Register(cfg);           // Stats / NumericSeries names
});
```

The library never calls `Initialize`. Until the host does, `FileIoLog` is a no-op and does not throw.

Do not pass `Exception` into `VestigiumLog.Write`. Do not log payload bytes. MESSAGE is stable (`job start`, `decision`, `stats`). Varying values go in properties.

JSONL path when AppId is FileIo:

```
%ProgramData%\\Vestigium\\Logs\\FileIo\\vestigium-FileIo-*.json
```

---

## 3. Host a job

```csharp
var job = FileIoJob.Copy(@"D:\\Export", @"E:\\Archive", new FileIoJobOptions
{
    Collision = FileIoCollision.UniqueName,
    UniqueNamePattern = ".##",
    ReconLeadTime = TimeSpan.FromSeconds(15),
    RetryCount = 3,
    RetryWait = TimeSpan.FromSeconds(2),
    RequestedBy = "wilkinson",     // ≤ 50, no PEM / long hex
    Reason = "export-archive",     // ≤ 80
    AuditMode = false,
    CopyOnlyUniqueContent = false,
    Purge = false,
    StopOnError = false
});

job.ProgressChanged += (_, p) =>
{
    ui.Bar = p.BytesFound == 0 ? 0 : p.BytesDone / (double)p.BytesFound;
    ui.Certainty = p.CertaintyPercent;   // 100 only when recon finished
    ui.Eta = p.EtaUtc;                   // null until certainty is 100
};

var result = await job.RunAsync(cancellation);

var sizes = result.Stats?.FileSizes;
var series = sizes?.Series;
var tiny = result.Stats?.Buckets[(int)FileIoBucket.Tiny];
```

`job.JobId` looks like `fio-a1b2c3d4e5f6`. Every job JSONL line uses that as `correlationId`.

Same options object works on `FileIoHelper.Copy` / `Move` / `Delete` / `Mirror`.

---

## 4. Audit Mode first

```csharp
var preview = FileIoHelper.Copy(src, dst, new FileIoJobOptions
{
    ReconLeadTime = TimeSpan.Zero,
    AuditMode = true
});
var previewResult = await preview.RunAsync();
// dest unchanged. JSONL has WouldCopy / WouldUniqueName / WouldDelete.
```

Then run the same options with `AuditMode = false`.

---

## 5. UniqueName

Default. Numeric `.##` → `report.01.txt`. Alpha `A##` → `report.A01.txt` … `A99` then `B01`. Width does not shrink.

Hitting the cap is `NameCap`: that item fails; the original dest file is **not** overwritten.

Skip and Overwrite are explicit (`FileIoCollision.Skip` / `Overwrite`).

---

## 6. Unique content and the dest index

```csharp
FileIoHelper.IndexRootOverride = tempIndexRoot;   // tests only
await FileIoHelper.Copy(src, dst, new FileIoJobOptions
{
    ReconLeadTime = TimeSpan.Zero,
    CopyOnlyUniqueContent = true
}).RunAsync();

FileIoHelper.CleanIndex(dst);
FileIoHelper.CleanIndexesOlderThan(TimeSpan.FromDays(30));
FileIoHelper.IndexRootOverride = null;
```

Production index: `%ProgramData%\\Vestigium\\FileIo\\Indexes\\{hash(full dest)}.jsonl`

Row: `path`, `size`, `mtime` (ISO UTC), `digest` (hex). Audit Mode does not append. Do not encrypt. Do not point tests at live ProgramData.

UniqueName (name) and unique-content (digest) are independent and may both be on.

---

## 7. Pause, Cancel, retry

```csharp
job.Pause();    // finishes current 64 KiB
job.Resume();
job.Cancel();   // aborts buffer; deletes dest this job created
```

`IOException` retries `RetryCount` times, then Failed `InUse`. `UnauthorizedAccessException` is Failed `Unauthorized`. The job continues unless `StopOnError` (status `Cancelled`).

A retry of a dest this job owns seeks to dest length when dest is shorter than source.

---

## 8. How recon works (operator view)

One coordinator. Recon walks source on 2–8 workers. Each file is assigned a size bucket **once**. Consumers wait until recon finishes **or** `ReconLeadTime` (default 15 s, hard cap 180 s). Values above 180 throw.

Certainty is 100 only when every recon worker is finished. Until then `BytesDone / BytesFound` may drop. Show `CertaintyPercent` next to the bar. `EtaUtc` stays null until certainty is 100.

Buckets (fixed): Tiny 0–256 KiB (8), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1, Tiny+Small queued < 32).

---

## 9. Two channels

Live `FileIoProgress` is chatty (UI). JSONL is sparse: job start → recon start/complete → consumers released → pause/resume/cancel → exception decisions only → Progress every 15 s → Stats → job end.

Do not expect a quiet Success per file.

---

## 10. EVENTID cheat sheet

| Id | Helper | When |
|---|---|---|
| 12500 / 12505 | Probe enter / complete | `Probe()` |
| 12510 / 12515 / 12520 / 12525 | Operation enter / complete / failed / warning | Compare, Analyze miss, prune, shred |
| 12530 | Path rejected | Guard |
| 12535 / 12540 / 12545 | Job start / complete / cancelled | RunAsync |
| 12550 / 12555 | Recon start / complete | |
| 12560 | Consumers released | Lead elapsed or recon done |
| 12565 | Decision | Would*, Skip, UniqueName, Overwrite |
| 12570 / 12575 / 12580 | NameCap / InUse / Unauthorized | Item fail |
| 12585 / 12590 | Index built / hit | Unique-content |
| 12595 | Progress | Every 15 s |
| 12600 | Stats | Finalize |
| 12605 / 12610 | Paused / Resumed | |

Block reserved through 12999. Count by 5.

---

## 11. Masks

```csharp
FileIoMask.Matches("foo.tmp", ["*.tmp"]);      // true
FileIoMask.Matches("notatmp.txt", ["*.tmp"]);  // false
FileIoMask.Matches("REPORT.TMP", ["*.tmp"]);   // true
```

Job exclude masks and Analyze use this helper. `*` and `?`, case-insensitive, whole name.

---

## 12. Analyze, Compare, Probe, shred

```csharp
var analysis = FileIoHelper.AnalyzeDirectory(root, new FileIoAnalyzeOptions
{
    ExcludeFileMasks = ["*.tmp"],
    MaxDepth = 3
});
var cmp = FileIoHelper.CompareFiles(left, right);
FileIoHelper.SecureDelete(path, FileIoShredRecipe.ThreeRandomThenZero);
FileIoHelper.PruneEmptyDirectories(root);
var probe = FileIoHelper.WriteProbe(dir, FileIoSize.From(8, FileIoSizeUnit.Mebibytes));
```

Library `Probe()` is a UniqueName + Audit self-check under `%TEMP%`. It must not touch Desktop.

---

## 13. Analytics

At finalize FileIo constructs `NumericSeries` snapshots (sizes always; rates only when bytes moved). Empty bins are Count = 0 and Series = null, never NaN. Audit Mode records sizes, not rates. Charts may draw `result.Stats.*.Series`. FileIo does not reference Charts.

---

## 14. Locked (do not “fix” in a host)

- Lead time default 15 s, range 0–180 s. Above 180 throws.
- Certainty is 100% only when recon is complete.
- Product name for the no-write pass is **Audit Mode**.
- Stream 64 KiB. Never `File.ReadAllBytes` on a payload.
- Same recon team and buckets for delete.
- Library never calls `VestigiumLogger.Initialize`.
- No FileIo Demo this revision.
- LAD, scheduler, admin, ACL copy: later.

---

## 15. Commands

```text
dotnet restore src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet build src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~FileIo
```

Pack FileIo for nuget.org only after Hashing 1.3.0 is a PackageReference.
