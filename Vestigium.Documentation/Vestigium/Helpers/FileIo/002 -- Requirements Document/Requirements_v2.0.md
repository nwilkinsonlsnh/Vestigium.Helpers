# Vestigium.Helpers.FileIo — Requirements Specification

**Document ID:** VEST-HLP-FILEIO-SRS-200  
**Version:** 2.0  
**Status:** Accepted. Supersedes Requirements v1.1 for new work. v1.1 remains historical in `Requirements_v1.0.md`.  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.FileIo` **1.1.0**  
**TFM:** `net10.0`  
**Companions:** [`Design_v2.0.md`](Design_v2.0.md), [`DevelopersGuide_v2.0.md`](DevelopersGuide_v2.0.md)

If implementation and this file disagree, this file wins. SRS v1.1 §§1–7 and 10–15 remain in force unless the delta table or locks 20–24 say otherwise.

This library is not `robocopy.exe`. Robocopy is the behavior reference. FileIo is the validated job: recon, Pause/Cancel, ALCOA+ JSONL through `FileIoLog`. No admin. No scheduler. No FileIo Demo in this revision.

## 0. Lossless delta from SRS v1.1

| Topic | v1.1 | v2.0 |
|---|---|---|
| Logging door | HelperLog / HelperCompat | `FileIoLog` → `VestigiumLog.Write`. `FileIoCatalog.Register`. EVENTID **12500–12610**. |
| Correlation | Job id in MESSAGE | `correlationId = JobId` (`fio-` + 12 hex) |
| Analytics | Project reference allowed | PackageReference **1.0.1** |
| Logging package | Implied | `Vestigium.Logging` **1.7.1** via `Directory.Build.props` |
| Hashing | Sibling | Project reference until Hashing **1.3.0** is on nuget.org. Do not publish FileIo while Hashing is a project reference. |
| Dest index | Specified | jsonl `{path,size,mtime,digest}`. Load + dest walk. Append after live copy. Audit Mode does not write. |
| Masks | Job regex; Analyze Contains | `FileIoMask.Matches`: `*` / `?`, case-insensitive, whole name |
| Demo | Required §9 | **Not shipped.** |
| Public extras | Implied | AnalyzeDirectory, WriteProbe/ReadProbe, FileIoMask, FileIoCatalog, FileIoEvents, Cancel() |
| Version | 1.0.0 | Library **1.1.0** |

## 2. Decisions locked

1. Lead time default 15 s, range 0–180 inclusive. Above 180 throws. No clamp.
2. Certainty is 100 only when ReconComplete. EtaUtc null until then. Job percent may drop.
3. Live FileIoProgress is chatty. JSONL is sparse.
4. Audit Mode: same recon and decisions, zero mutations. Product name is Audit Mode.
5. Delete uses the same recon team and five buckets.
6. LAD is not this revision.
7. FileIoLog only. Category Helpers. APPID FileIo. Library never Initialize. Host registers FileIoCatalog.
8. Default collision UniqueName `.##`. Cap is NameCap. Never wrap to overwrite.
9. Pause finishes current 64 KiB. Cancel aborts and deletes dest this job created. Retries 3 / 2 s. Mid-file resume at dest length when owned. No crash auto-restart.
10. No admin, VSS, ACL copy, scheduler.
11. BCL streams. No robocopy.exe.
12. 64 KiB. Never ReadAllBytes/ReadAllText on a payload.
13. Paths and digest hex allowed. File contents and Exception objects forbidden.
14. SecureDelete is opt-in Zero/Random passes then File.Delete. Default delete is normal delete.
15. Five buckets: Tiny 0–256 KiB (8), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1 iff Tiny+Small queued < 32).
16. Unique-content is digest (Hashing, default SHA-256). UniqueName is name. Both may be on.
17. Product name is Audit Mode, not dry run.
18. RetryCount 3, RetryWait 2 s.
19. Analytics 1.0.1 host. Empty bins Count 0 Series null. Audit Mode sizes yes rates no. No Charts reference.
20. JobId = fio- + 12 hex = correlationId.
21. Index under %ProgramData%\\Vestigium\\FileIo\\Indexes\\{hash(dest)}.jsonl. Tests use IndexRootOverride. CleanIndex / CleanIndexesOlderThan explicit. Audit Mode does not append.
22. FileIoMask.Matches for Job and Analyze.
23. No FileIo.Demo this revision.
24. Do not nuget push FileIo while Hashing is a ProjectReference.

## 3. Goals

G1–G14 from SRS v1.1 (façade + job, verbs, recon, progress, certainty, UniqueName, Audit Mode, Pause/Cancel, ALCOA+, no elevation, Probe TEMP, prune, dest index, Analytics).  
G15 named EVENTIDs. G16 one wildcard rule.

## 4. Engine (same as v1.1, recorded)

Coordinator: Start → recon → lead or recon-complete → consume (recon may continue) → finalize → done.

Recon workers min(4, max(2, ProcessorCount/2)) cap 8. Work item {relativePath, size, bucket, writeTimeUtc, attributes}.

Pause: EVENTID 12605 / 12610. Cancel: 12545, status Cancelled. IOException → retry then InUse 12575. UnauthorizedAccessException → Unauthorized 12580. StopOnError → Cancel.

Audit Mode: no mutations, no index append, Would* decisions, certainty still 100.

## 5. Features

Verbs: Copy, Move, Delete, Mirror (Purge default false), CompareFiles, AnalyzeDirectory, Prune, SecureDelete, Probe/WriteProbe/ReadProbe.

UniqueName numeric .## and alpha A## as SRS v1.1 §5.3 (width is cap; NameCap never overwrites).

Filters: IncludeEmptyDirectories, MaxDepth, Exclude*Masks via FileIoMask, Min/Max size, Min/Max age (mtime), Retry, CopyTimestampsAndAttributes. No LAD, ADS, ACL.

Index row: {"path","size","mtime","digest"}. Load jsonl then dest walk. Append after live copy.

SecureDelete presets: ThreeRandomThenZero, SevenRandomThenZero, ZeroRandomZero.

Analytics: Observe sizes on Done/Skip; rates on live Done only. Finalize snapshots + Stats line 12600.

## 6. JSONL EVENTIDs

12535 job start, 12540 complete, 12545 cancelled, 12550/12555 recon, 12560 consumers released, 12565 decision, 12570 NameCap, 12575 InUse, 12580 Unauthorized, 12585/12590 index, 12595 progress 15 s, 12600 stats, 12605/12610 pause/resume, 12500/12505 probe, 12510–12530 operation/guard.

Stable MESSAGE. Properties carry varying values. No Exception argument. No quiet Success per file.

## 7. Public surface

FileIoHelper: Identity, Probe, Copy/Move/Delete/Mirror, AnalyzeDirectory, WriteProbe, ReadProbe, CompareFiles, PruneEmptyDirectories, SecureDelete, CleanIndex, CleanIndexesOlderThan.

FileIoJob: JobId, Verb, Source, Destination, Options, Progress, IsPaused, ProgressChanged, RunAsync, Pause, Resume, Cancel.

FileIoMask.Matches, FileIoCatalog.AppId + Register, FileIoEvents 12500–12610.

Options as SRS v1.1 plus implemented setters. RequestedBy ≤50, Reason ≤80, secret-shaped strings rejected. Lead time outside 0–180 throws. Missing source throws FileNotFoundException before RunAsync.

Internal: IndexRootOverride, IndexRoot, IndexPath.

## 8–9. Taxonomy and Demo

Subcategories: Probe, Identity, Guard, Job, Recon, Copy, Move, Delete, Mirror, Index, Progress, Compare, Prune, SecureDelete, Analyze, Stats.

Demo is not required in v2.0.

## 10. Tests

TEMP only. IndexRootOverride for index tests. Fixtures in FileIoPR02/03/04Tests and FileIoLoggingTests: UniqueName + NameCap, Audit Mode, Cancel, lead 181 throws, empty Stats bins, InUse + continue, StopOnError Cancelled, CleanIndex, persist jsonl, Audit writes no index, *.tmp vs notatmp.txt, Analyze count, IndexRoot ProgramData when override null, Probe 12505, missing Analyze 12520, no host does not throw, JobId fio- + 12 hex.

## 11–15. Non-goals, roadmap, siblings, glossary, acceptance

Non-goals: LAD, scheduler, VSS/ACL, ADS, monitor, encrypt index, hash API on FileIo, robocopy.exe, crash restart, Charts reference, tree-diff Compare, Demo this revision.

Next: publish Hashing 1.3.0, swap FileIo to PackageReference, pack FileIo 1.1.0.

Never: filter drivers, elevation as default, logging contents, ReadAllBytes on a payload.

Siblings: Hashing 1.3.0 (project until feed), Analytics 1.0.1 package, Logging 1.7.1 package, Encryption/Charts not referenced.

Acceptance: this file + Design v2.0 + Developers Guide v2.0 on main; §§4–8 match code; §10 tests green; no FileIo nuget push while Hashing is a project reference.
