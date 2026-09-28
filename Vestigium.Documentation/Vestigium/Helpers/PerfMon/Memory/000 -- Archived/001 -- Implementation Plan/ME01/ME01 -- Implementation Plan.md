# Vestigium.Helpers.PerfMon.Memory — ME01 implementation plan

**Document ID:** VEST-HLP-PERFMON-MEM-PLAN-ME01
**Version:** 1.0
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Memory` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.Memory/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 clock, PM02 catalog)
**Fingerprint:** [`PM02 -- Catalog Fingerprint.md`](../../../PerfMon/001%20--%20Implementation%20Plan/PM02/PM02%20--%20Catalog%20Fingerprint.md)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`ME01 -- Backlog.md`](ME01%20--%20Backlog.md)

This probe names memory-family PDH objects and returns shared `SampleRecord` rows. It does not own interval math and does not own the paging file.

---

## 1. Goal

A host can:

1. List, check, snapshot, and watch the five objects.
2. Run a short job on the `Memory` object (available, commit, cache bytes, pages/sec).
3. Register events inside `VestigiumLogger.Initialize`.

Done when the fixtures in §8 pass against a fake inventory / source and this project still has no Charts / Analytics / Processes / PageFile reference.

## 2. What this version is not

- A second sample loop
- `Paging File(*)\% Usage`
- Per-process working set
- RAM map / standby wipe
- Charts or Analytics
- NuGet publish

---

## 3. Shape

```
host
  → MemoryCounterCatalog                vocabulary + PM02 checks/watch
  → MemoryPerf.RunAsync                 later steps
       → SampleJob                      shared clock
```

`Memory` never falls back to PageFile. Different object, different package.

---

## 4. Public surface

Namespace: `Vestigium.Helpers.PerfMon.Memory`.

```csharp
public static class MemoryPerf
{
    public static Task<SampleJobResult> RunAsync(
        MemorySampleOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class MemorySampleOptions
{
    public string Instance { get; init; } = "";
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}
```

Empty instance stays empty on the Memory object. Do not coerce it to `_Total`.

---

## 5. Catalog objects and known counters

| Object | Known this plan |
|---|---|
| `Memory` | `Available MBytes`, `Available Bytes`, `Committed Bytes`, `Commit Limit`, `% Committed Bytes In Use`, `Cache Bytes`, `Pages/sec`, `Page Faults/sec`, `Pool Paged Bytes`, `Pool Nonpaged Bytes` |
| `Cache` | `Copy Read Hits %`, `Copy Reads/sec`, `Data Map Hits %`, `Lazy Write Flushes/sec`, `Lazy Write Pages/sec` |
| `NUMA Node Memory` | `Available MBytes`, `Free & Zero Page List MBytes` |
| `ReadyBoost Cache` | empty known — live lists the box |
| `Hyper-V Dynamic Memory Integration Service` | empty known — live lists the box |

Live never substitutes known when the object is missing.

## 6. Short job v1

Object: `Memory` only. Instance: `""`.

| Counter | Unit | Prime |
|---|---|---|
| `Available MBytes` | `MB` | No |
| `Committed Bytes` | `B` | No |
| `Commit Limit` | `B` | No |
| `% Committed Bytes In Use` | `%` | No |
| `Cache Bytes` | `B` | No |
| `Pages/sec` | `/sec` | Yes |

If `Memory` is missing, the path list is empty and `RunAsync` hits the shared empty-path reject. That is abnormal on Windows; still do not invent zeros.

---

## 7. Files

| File | Role |
|---|---|
| `MemoryObjects.cs` | Five PDH names |
| `MemoryCounterCatalog.cs` | Façade over `CounterSet` |
| `MemorySampleOptions.cs` | Later |
| `MemoryPaths.cs` | Later |
| `MemoryPerf.cs` | Later |
| `MemoryPerfEvents.cs` / `MemoryPerfLog.cs` / `MemoryPerfCatalog.cs` | Door |
| `EventCatalog/memory.json` | Shard |

Tests: `src/Vestigium.Helpers.Tests/PerfMonMemoryME01Tests.cs`.
Do not add PerformanceCounter to this project.

---

## 8. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **ME01.001** | P0 | `MemoryObjects` + `MemoryCounterCatalog` on shared `CounterSet`. | Catalog fixtures. |
| **ME01.002** | P0 | `MemoryPaths`. Empty instance stays empty. Object is `Memory`. | No PageFile leak. |
| **ME01.003** | P0 | Reserved. No per-disk-style expansion on the Memory object. | Skip or no-op fixture. |
| **ME01.004** | P0 | Missing Memory object stays Memory. No PageFile swap. | Fixture. |
| **ME01.005** | P0 | `MemoryPerf.RunAsync` onto `SampleJob`. | Count=1 against fake. |
| **ME01.006** | P1 | Logging door. APPID `PerfMon.Memory`. | No Initialize. |
| **ME01.007** | P1 | EVENTID 19000–19045. `memory.json`. | Count by 5. |
| **ME01.008** | P2 | Filter `ME01_`. Shared only. | Green with no live counters. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~ME01_
```

---

## 9. EVENTID

Reserved 19000–19499. Used this plan: 19000–19045.

| Id | Name | Severity | When |
|---|---|---|---|
| 19000 | ProbeEnter | Debug | enter |
| 19005 | ProbeStarted | Information | job handed to shared |
| 19010 | PathsBuilt | Debug | path count |
| 19015 | ProbeComplete | Information | done |
| 19020 | ProbeCancelled | Information | cancelled |
| 19025 | ProbeRejected | Error | empty paths or bad options |
| 19030 | ObjectMissing | Warning | Memory object absent |
| 19035 | SiblingSkipped | Debug | PageFile not used |
| 19040 | OptionalAbsent | Debug | Hyper-V / ReadyBoost / NUMA absent |
| 19045 | ProbeFailed | Error | unexpected after start |

Do not log the sample vector.

---

## 10. Still parked

| Item | Why |
|---|---|
| Cache / NUMA / ReadyBoost / Hyper-V short jobs | Catalog first |
| PageFile `% Usage` | Sibling |
| Per-process working set | Processes |
| Publish 0.1.0 | After a host consumes this |

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Locked. Five-object catalog. Memory-object short job. |
