# Vestigium.Helpers.PerfMon.Gpu — PG01 implementation plan

**Document ID:** VEST-HLP-PERFMON-GPU-PLAN-PG01
**Version:** 1.0
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Gpu` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.Gpu/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 clock, PM02 catalog)
**Fingerprint:** [`PM02 -- Catalog Fingerprint.md`](../../../PerfMon/001%20--%20Implementation%20Plan/PM02/PM02%20--%20Catalog%20Fingerprint.md)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PG01 -- Backlog.md`](PG01%20--%20Backlog.md)

This probe names GPU PDH objects and returns shared `SampleRecord` rows. It does not own interval math and does not load a vendor DLL.

---

## 1. Goal

A host can:

1. List, check, snapshot, and watch the five GPU objects.
2. Run a short job: engine utilization + adapter dedicated memory on live instances.
3. Register events inside `VestigiumLogger.Initialize`.

Done when the fixtures in §8 pass against a fake inventory / source and this project still has no Charts / Analytics / Processes / vendor SDK reference.

## 2. What this version is not

- A second sample loop
- NVML, ADL, DXGI as a public API
- A frame-time plot
- A merged Intel+NVIDIA total
- Charts or Analytics
- NuGet publish

---

## 3. Shape

```
host
  → GpuCounterCatalog                   this package (vocabulary)
       → CounterSet                     shared
            → ICounterInventory
  → GpuPerf.RunAsync                    later steps
       → SampleJob                      shared clock
```

---

## 4. Public surface

Namespace: `Vestigium.Helpers.PerfMon.Gpu`.

```csharp
public static class GpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        GpuSampleOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class GpuSampleOptions
{
    public string? Instance { get; init; }
    public bool IncludeAllInstances { get; init; } = true;
    public int InstanceCap { get; init; } = 256;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}
```

Empty / null `Instance` does **not** become `_Total`. The short job uses live instances. A caller-named instance is used as-is.

---

## 5. Catalog objects and known counters

| Object | Known counters this plan |
|---|---|
| `GPU Engine` | `Utilization Percentage`, `Running Time` |
| `GPU Process Memory` | `Dedicated Usage`, `Shared Usage`, `Total Committed`, `Local Usage`, `Non Local Usage` |
| `GPU Adapter Memory` | `Dedicated Usage`, `Shared Usage`, `Total Committed` |
| `GPU Local Adapter Memory` | `Local Usage` |
| `GPU Non Local Adapter Memory` | `Non Local Usage` |

Live lists the box. Known never stands in when the object is missing.

## 6. Short job v1

When the object is present:

| Counter | Object | Unit | Prime |
|---|---|---|---|
| `Utilization Percentage` | GPU Engine | `%` | Yes |
| `Dedicated Usage` | GPU Adapter Memory | `B` | No |

If `GPU Engine` is missing, skip those rows. If `GPU Adapter Memory` is missing, skip those rows. If both missing, the path list is empty and `RunAsync` throws the shared empty-path reject. That is the headless box.

---

## 7. Files

| File | Role |
|---|---|
| `GpuObjects.cs` | Five PDH names |
| `GpuCounterCatalog.cs` | Façade over `CounterSet` |
| `GpuSampleOptions.cs` | Later steps |
| `GpuPaths.cs` | Later steps |
| `GpuPerf.cs` | Later steps |
| `GpuPerfEvents.cs` / `GpuPerfLog.cs` / `GpuPerfCatalog.cs` | Door |
| `EventCatalog/gpu.json` | Shard |

Tests: `src/Vestigium.Helpers.Tests/PerfMonGpuPG01Tests.cs`.
Do not add PerformanceCounter to this project.

---

## 8. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PG01.001** | P0 | `GpuObjects` + `GpuCounterCatalog` on shared `CounterSet`. | Catalog fixtures. |
| **PG01.002** | P0 | `GpuPaths`. Named instance kept. No invented `_Total`. | Fixtures. |
| **PG01.003** | P0 | Live instances, cap 256. | Cap truncates. |
| **PG01.004** | P0 | Missing object omitted. Both missing rejects empty path list. | No vendor fallback. |
| **PG01.005** | P0 | `GpuPerf.RunAsync` onto `SampleJob`. | Count=1 against fake. |
| **PG01.006** | P1 | Logging door. APPID `PerfMon.Gpu`. | No Initialize. |
| **PG01.007** | P1 | EVENTID 18500–18545. `gpu.json`. | Count by 5. |
| **PG01.008** | P2 | Filter `PG01_`. Shared only. | Green with no live GPU. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PG01_
```

---

## 9. EVENTID

Reserved 18500–18999. Used this plan: 18500–18545.

| Id | Name | Severity | When |
|---|---|---|---|
| 18500 | ProbeEnter | Debug | enter |
| 18505 | ProbeStarted | Information | job handed to shared |
| 18510 | PathsBuilt | Debug | path count |
| 18515 | ProbeComplete | Information | done |
| 18520 | ProbeCancelled | Information | cancelled |
| 18525 | ProbeRejected | Error | empty paths or bad options |
| 18530 | ObjectMissing | Warning | one GPU object absent |
| 18535 | InstancesCapped | Warning | list truncated |
| 18540 | Headless | Information | both short-job objects absent |
| 18545 | ProbeFailed | Error | unexpected after start |

Do not log the sample vector.

---

## 10. Still parked

| Item | Why |
|---|---|
| Per-PID filter | Instance strings already carry `pid_` |
| Local / Non Local short jobs | Catalog first |
| Vendor SDK | Non-goal |
| Publish 0.1.0 | After a host consumes this |

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Locked. Five-object catalog. Short job Engine + Adapter Dedicated. |
