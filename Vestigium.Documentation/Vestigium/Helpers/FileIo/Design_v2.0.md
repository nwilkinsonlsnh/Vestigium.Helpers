# Vestigium.Helpers.FileIo — Design

**Document ID:** VEST-HLP-FILEIO-DES-200  
**Version:** 2.0  
**Status:** Accepted  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.FileIo` 1.1.0  
**Contract:** [`Requirements_v2.0.md`](Requirements_v2.0.md)  
**Host notes:** [`DevelopersGuide_v2.0.md`](DevelopersGuide_v2.0.md)

This file is the architecture of the running library. Requirements say *what*. This file says *where it lives* and *how it is wired*. If they disagree, Requirements win.

---

## 1. Package graph

```
Host process
  └─ VestigiumLogger.Initialize
        FileIoCatalog.Register
        AnalyticsCatalog.Register   (when Stats matter)
  └─ FileIoHelper / FileIoJob
        FileIoLog ──► Vestigium.Logging 1.7.1
        NumericSeries ──► Vestigium.Helpers.Analytics 1.0.1
        HashFile / HashString ──► Vestigium.Helpers.Hashing (csproj until 1.3.0 is on nuget.org)
```

FileIo **does not** reference Charts, Encryption, Gallery, or Demo.

MSBuild:

- Repo-root `Directory.Build.props` injects `Vestigium.Logging` `$(VestigiumLoggingVersion)` = 1.7.1 into every project.
- FileIo.csproj also lists that PackageReference so the project file is readable without opening props.
- Analytics is PackageReference 1.0.1.
- Hashing is ProjectReference. Pack FileIo for nuget.org only after that becomes PackageReference 1.3.0.
- `EventCatalog/fileio.json` packs to `contentFiles/any/any/EventCatalog/`.
- `InternalsVisibleTo` Tests.

Version: **1.1.0** (catalog + persist + mask after engine 1.0.0).

---

## 2. Source map

| File | Role |
|---|---|
| `FileIoHelper.cs` | Façade: Identity, Probe, verbs, Compare, Prune, SecureDelete, CleanIndex, IndexRoot/Path |
| `FileIoJob.cs` | Coordinator: create, JobId, Pause/Resume/Cancel, RunAsync, Finish, RememberDest |
| `FileIoJob.Recon.cs` | Walk, enqueue, filters, consume dispatcher, delete path |
| `FileIoJob.Copy.cs` | DoCopyAsync, stream copy, collision, purge, progress helpers |
| `FileIoDestIndex.cs` | jsonl load / append |
| `FileIoMask.cs` | Shared `*` / `?` matcher |
| `FileIoAnalyze.cs` | AnalyzeDirectory + probe option types + bucket census |
| `FileIoLog.cs` | Thin write + named helpers + Subcategories + guards |
| `FileIoCatalog.cs` | Taxonomy + EVENTID rows |
| `FileIoEvents.cs` | Constants 12500–12610 |
| `EventCatalog/fileio.json` | JSON twin packed in the nupkg |
| `FileIoTypes.cs` | Options, result, progress, collision, shred, age, size |
| `UniqueName.cs` | Numeric / A## mint + cap |
| `FileIoJobStats.cs` | Analytics snapshots at finalize |
| `README.md` | nupkg readme |

Tests: `FileIoLoggingTests`, `FileIoPR02Tests`, `FileIoPR03Tests`, `FileIoPR04Tests`. Legacy session/coverage files stay `<Compile Remove>`.

---

## 3. JobId and correlation

`JobId = "fio-" + FileIoLog.NewId()`.

`NewId` is 12 lowercase hex characters. `correlationId` on `VestigiumLog.Write` is that JobId for every job-scoped line. Probe / Compare / Prune / CleanIndex / Analyze use null correlation unless a helper passes one.

---

## 4. RunAsync pipeline

```
Create (validate options, source exists)
  JobStart 12535
  if CopyOnlyUniqueContent
      Load jsonl
      Walk dest → destIndex[digest] = path
      IndexBuilt 12585
  ReconStart 12550
  Start ReconAsync
  Wait ReconLeadTime or recon done
  if cancelled → Finish Cancelled
  ConsumersReleased 12560
  Start ConsumeAsync (per-bucket worker counts)
  Await recon → ReconComplete 12555, certainty 100
  Await consume
  Optional Mirror Purge
  Optional PruneEmptyDirectories
  Finish → Stats 12600 + JobComplete 12540 or JobCancelled 12545
```

`ConsumeAsync` dequeues work items. Copy/Move → `DoCopyAsync`. Delete → delete/shred. Huge consumers wait on the Tiny+Small watermark.

---

## 5. Copy item path

```
unique-content digest hit? → SkipDuplicate 12590
name exists?
    Skip → Skip
    UniqueName next null → NameCap 12570
    UniqueName next → Decision UniqueName 12565
    Overwrite → Decision Overwrite 12565
AuditMode → Decision Would* ; Done; no stream; no RememberDest
else
    Create parent
    Track _created[path] = 1 if this job created the file
    CopyStreamAsync 64 KiB
    optional timestamps/attributes
    RememberDest → hash dest, destIndex[], append jsonl
    Move → delete source
    Done + Observe(rate)
```

`CopyStreamAsync`: if resume and `0 < dest.Length < source.Length`, seek both. Cancel mid-loop disposes dest and `File.Delete` if owned.

`HandleAsync` (recon file): wrap DoCopy in retry loop for IOException.

---

## 6. Index design

- One jsonl per destination **root**, name = SHA of `Path.GetFullPath(dest)` via `HashingHelper.HashString`.
- Append-only. Duplicate digests: last writer wins in the in-memory dictionary; jsonl may contain historical rows.
- Corrupt lines skipped on load.
- `IndexPath` creates IndexRoot if missing.
- Production root is CommonApplicationData. Tests must set override before Copy with unique-content or CleanIndex will touch ProgramData — forbidden by Requirements lock 21 / 17.

---

## 7. Mask design

`FileIoMask.Matches`:

1. Empty name or empty mask list → false.
2. Each mask: `Regex.Escape` then `\\*` → `.*`, `\\?` → `.`.
3. Anchor `^...$`, IgnoreCase + CultureInvariant.

Analyze used to `Contains(mask.Trim('*'))`, which made `*.tmp` match `notatmp.txt`. That is gone.

Job.Recon still calls a local `Masked` that must remain the same rule (delegate to `FileIoMask` or keep the identical regex). Behavior must not drift.

---

## 8. Logging design

`FileIoLog.Write(eventId, level, status, subcategory, message, exception: null, correlationId, properties)`.

Named helpers pin EVENTID + MESSAGE. Properties from `FileIoLog.Props(("k", v), ...)` omit null values.

`FileIoCatalog.Register` registers category Helpers, APPID FileIo, subcategories, and one row per FileIoEvents constant. JSON twin must stay in lockstep with `FileIoEvents` and the catalog C# rows.

Library never attaches `Exception`. Failed lines use reason strings (`InUse`, `Unauthorized`, `NameCap`).

---

## 9. Analytics design

`Observe` pushes `FileIoTransferObservation` into a ConcurrentBag. `FileIoJobStats.From` groups by bucket, calls `NumericSeries` only when count > 0. Wall-clock `BytesDone / elapsed` is a scalar on the stats object, not a series.

FileIo does not compute UCL/LCL. Charts, if a host wants them, consume `result.Stats.*.Series`.

---

## 10. Probe design

`FileIoHelper.Probe` builds `%TEMP%\\Vestigium.Helpers.FileIo.Probe\\{guid}\\Export|Archive`, writes `nathan.txt` both sides, Copy with lead 0 (expects UniqueName `nathan.01.txt`), Audit Copy, deletes the tree. EVENTID 12505 on success.

WriteProbe / ReadProbe are sized files for rate experiments; KeepProbe optional.

---

## 11. Concurrency and cancellation

- `_gate` lock for progress counters.
- `_cts` linked with caller token.
- `_paused` + `GateAsync` delay 15 ms between buffers.
- Dest index dictionary is ConcurrentDictionary.
- `_created` tracks owned dest paths for cancel cleanup.

No job scheduler. One `RunAsync` per instance. Do not reuse a finished job.

---

## 12. Threat / ALCOA+ notes

| Risk | Mitigation |
|---|---|
| Payload in logs | MESSAGE stable; no file bytes |
| Secret in by/reason | LooksLikeSecret + length caps at Create |
| Exception dump | Write exception argument always null |
| Index as PII store | Paths + digest hex only; no encrypt in v2.0 (explicit non-goal) |
| Test pollution | TEMP trees; IndexRootOverride |
| nuget graph lie | Do not push FileIo with a Hashing project reference |

---

## 13. File layout on disk (runtime)

```
%ProgramData%\\Vestigium\\
  FileIo\\Indexes\\{dest-hash}.jsonl
  Logs\\{host-APPID}\\vestigium-{APPID}-*.json     // host chooses APPID
```

If the host sets `AppId = FileIoCatalog.AppId`, logs land under `Logs\\FileIo\\`.
