# Vestigium.Helpers.FileIo — PR02 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR02  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P0  
**Depends on:** PR01 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR02 finishes Vestigium.Logging inside FileIo. The package is already referenced. The catalog is too coarse.

---

## Goal

A host that calls `FileIoCatalog.Register` gets a real EVENTID block. Job lines carry `correlationId = JobId`. `FileIoJob` talks to `FileIoLog` only. `HelperCompat.cs` is deleted.

## Baseline (already true)

- `Directory.Build.props` → `Vestigium.Logging` 1.7.1.
- `FileIoLog.Write` calls `VestigiumLog.Write` when `VestigiumLogger.IsInitialized`.
- Current events: 12500 ProbeEnter, 12505 ProbeComplete, 12510 OperationEnter, 12515 OperationComplete, 12520 OperationFailed, 12525 OperationWarning, 12530 PathRejected.
- Pause, UniqueName, Stats, job summary, recon complete all share 12510–12525. MESSAGE text carries the meaning. That fights Logging flood identity `(APPID, CATEGORY, LEVEL, MESSAGE)`.

## Locked logging rules

- Library never calls `VestigiumLogger.Initialize`.
- Never pass `Exception` into `VestigiumLog.Write` from FileIo.
- Never log file contents. Paths and digest hex are allowed.
- MESSAGE is stable (`"UniqueName"`, `"WouldCopy"`, `"Pause"`). Varying values go in `properties`.
- `appId:` on the write is `FileIoCatalog.AppId`. Folder follows the **host** APPID.

## Event block (keep 12500–12530; add by 5)

| EVENTID | Name | Level | Subcategory | Stable MESSAGE |
|---|---|---|---|---|
| 12500 | ProbeEnter | Debug | Probe | enter Probe |
| 12505 | ProbeComplete | Information | Probe | probe complete |
| 12510 | OperationEnter | Debug | Job | enter operation |
| 12515 | OperationComplete | Information | Job | operation complete |
| 12520 | OperationFailed | Error | Job | operation failed |
| 12525 | OperationWarning | Warning | Job | operation warning |
| 12530 | PathRejected | Error | Job | rejected path |
| 12535 | JobStart | Information | Job | job start |
| 12540 | JobComplete | Information | Job | job complete |
| 12545 | JobCancelled | Warning | Job | job cancelled |
| 12550 | ReconStart | Debug | Recon | recon start |
| 12555 | ReconComplete | Information | Recon | recon complete |
| 12560 | ConsumersReleased | Information | Job | consumers released |
| 12565 | Decision | Information | Copy/Move/Delete/Mirror | decision |
| 12570 | NameCap | Error | Job | name cap |
| 12575 | ItemInUse | Error | Job | in use |
| 12580 | ItemUnauthorized | Error | Job | unauthorized |
| 12585 | IndexBuilt | Information | Index | index built |
| 12590 | IndexHit | Information | Index | skip duplicate |
| 12595 | ProgressSnapshot | Information | Progress | progress snapshot |
| 12600 | StatsFinalize | Information | Stats | stats |
| 12605 | JobPaused | Warning | Job | pause |
| 12610 | JobResumed | Information | Job | resume |

`Decision` properties include `kind` (`UniqueName`, `Skip`, `Overwrite`, `WouldCopy`, `WouldUniqueName`, `WouldDelete`, `WouldSkipDuplicate`). Do not invent a new EVENTID per kind in this PR.

Three files stay twins: `FileIoEvents.cs`, `FileIoCatalog.Rows`, `EventCatalog/fileio.json`. No orphan id.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR02.001** | P0 | Add constants 12535–12610 to `FileIoEvents`. | Open |
| **PR02.002** | P0 | Register the same rows in `FileIoCatalog` and `fileio.json`. | Open |
| **PR02.003** | P0 | `FileIoLog.Write` accepts `correlationId` and `properties`. Default correlation is null for Probe / Analyze / Compare without a job. | Open |
| **PR02.004** | P0 | Named helpers on `FileIoLog` (`JobStart`, `Decision`, `Stats`, `Pause`, …) or pass explicit event ids from `FileIoJob`. Stop stuffing every line through generic Success/Failed. | Open |
| **PR02.005** | P0 | `FileIoJob` uses `FileIoLog.Subcategories` and `FileIoLog.NewId`. Delete `HelperCompat.cs`. Remove `using Vestigium.Helpers;` if it is leftover. | Open |
| **PR02.006** | P1 | Job JSONL includes `correlationId` = `JobId` (`fio-` + 12 hex). | Open |
| **PR02.007** | P1 | Tests in `FileIoLoggingTests` (and a new `FileIoPR02Tests` if the file gets large): catalog register; Probe still 12505; job start 12535; finalize 12600; cancel 12545; no host does not throw. | Open |
| **PR02.008** | P2 | Developers Guide: host calls `FileIoCatalog.Register(cfg)` (and `AnalyticsCatalog.Register` when Stats matter). Strike HelperLog / HelperWpfHost. | Open |

## Do not

- Change copy/move/delete behavior.
- Log a quiet Success per file.
- Attach `Exception.ToString()`.
- Call `Initialize` from the library.
- Persist the dest index (PR04).
- Re-enable session tests (PR03) except the logging fixtures this PR adds.

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02_
```

A Copy job under an initialized logger produces 12535, then 12600, then 12540, same `correlationId`. `HelperCompat.cs` is gone. Build does not mention `HelperLog`.

Commit: `FileIo PR02: EVENTID catalog and retire HelperCompat`.

## Out of PR02

Dest index files, Analyze mask fix, Demo, Hashing NuGet, engine session tests beyond logging.
