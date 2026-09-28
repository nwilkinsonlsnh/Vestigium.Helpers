# Vestigium.Helpers.PerfMon.PageFile — PF01 implementation plan

**Document ID:** VEST-HLP-PERFMON-PF-PLAN-PF01
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Package:** `Vestigium.Helpers.PerfMon.PageFile` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.PageFile/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 clock, PM02 catalog)
**Fingerprint:** [`PM02 -- Catalog Fingerprint.md`](../../../PerfMon/001%20--%20Implementation%20Plan/PM02/PM02%20--%20Catalog%20Fingerprint.md)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PF01 -- Backlog.md`](PF01%20--%20Backlog.md)

This probe names `Paging File` and returns shared `SampleRecord` rows. It does not own interval math and does not own Memory.

---

## 1. Goal

A host can check / watch `Paging File` and run `% Usage` + `% Usage Peak` on `_Total` or named files.

## 2. What this version is not

Memory commit. `Pages/sec`. Pagefile resize. Charts. NuGet publish.

## 3. Shape

```
host
  → PageFileCounterCatalog
       → CounterSet
  → PageFilePerf.RunAsync          later steps
       → SampleJob
```

No fallback to the Memory object.

## 4. Short job v1

Object: `Paging File`. Default instance `_Total`.

| Counter | Unit | Prime |
|---|---|---|
| `% Usage` | `%` | No |
| `% Usage Peak` | `%` | No |

`IncludeFiles` adds live instances excluding `_Total`, cap 256.

## 5. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PF01.001** | P0 | `PageFileObjects` + `PageFileCounterCatalog`. | Landed with this lock. |
| **PF01.002** | P0 | `PageFilePaths`. Empty → `_Total`. Always `Paging File`. | Fixture. |
| **PF01.003** | P0 | `IncludeFiles`. Cap 256. Drop `_Total`. | Cap truncates. |
| **PF01.004** | P0 | Missing object stays Paging File. No Memory swap. | Fixture. |
| **PF01.005** | P0 | `PageFilePerf.RunAsync`. | Count=1 against fake. |
| **PF01.006** | P1 | Logging door. APPID `PerfMon.PageFile`. | No Initialize. |
| **PF01.007** | P1 | EVENTID 20000–20045. `pagefile.json`. | Count by 5. |
| **PF01.008** | P2 | Filter `PF01_`. Shared only. No Memory project ref. | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PF01_
```

## 6. EVENTID

Reserved 20000–20499. Used this plan: 20000–20045.

| Id | Name | Severity | When |
|---|---|---|---|
| 20000 | ProbeEnter | Debug | enter |
| 20005 | ProbeStarted | Information | job handed to shared |
| 20010 | PathsBuilt | Debug | path count |
| 20015 | ProbeComplete | Information | done |
| 20020 | ProbeCancelled | Information | cancelled |
| 20025 | ProbeRejected | Error | bad options |
| 20030 | ObjectMissing | Warning | Paging File absent |
| 20035 | FilesCapped | Warning | list truncated |
| 20040 | SiblingSkipped | Debug | Memory not used |
| 20045 | ProbeFailed | Error | unexpected after start |

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Paging File only. |
