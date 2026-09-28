# Vestigium.Helpers.PerfMon.Disk — PD01 PhysicalDisk implementation plan

**Document ID:** VEST-HLP-PERFMON-DISK-PLAN-PD01
**Version:** 1.0
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Disk` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.Disk/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 clock, PM02 catalog)
**Fingerprint:** [`PM02 -- Catalog Fingerprint.md`](../../../PerfMon/001%20--%20Implementation%20Plan/PM02/PM02%20--%20Catalog%20Fingerprint.md)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PD01 -- Backlog.md`](PD01%20--%20Backlog.md)

This plan is the PhysicalDisk short job. The 19-object catalog already exists. LogicalDisk and the Storage / ReFS / Storport / VHD objects stay catalog-only until their own plan.

---

## 1. Goal

A host can:

1. Check and watch `PhysicalDisk` through `DiskCounterCatalog`.
2. Run a short job: `_Total` bytes/sec, read/write latency, queue, `% Disk Time`.
3. Opt in to per-disk rows, capped at 256.
4. Register events inside `VestigiumLogger.Initialize`.

Done when the fixtures in §8 pass against a fake `ICounterSource` / `ICounterInventory` and this project still has no Charts / Analytics / FileIo / Processes reference.

## 2. What this version is not

- A second sample loop
- A LogicalDisk free-space job
- Storage Spaces / ReFS / Storport / VHD sampling
- SMART, RAID CLI, BitLocker
- FileIo copy timing
- Charts or Analytics
- NuGet publish

If a step needs one of those, it is the wrong step.

---

## 3. Shape

```
host
  → DiskCounterCatalog                  vocabulary + PM02 checks/watch
  → DiskPerf.RunAsync(options)
       → DiskPaths.Physical(options)    this package
       → new SampleJob(paths, shared)   Vestigium.Helpers.PerfMon
            → ICounterSource            shared PDH or test fake
       → SampleJobResult
DiskPerfCatalog.Register is host-only
```

Shared already primes rate counters. This package does not call `NextValue` itself.

PhysicalDisk never falls back to LogicalDisk. Different object. Missing PhysicalDisk is `Unavailable` on those paths, not a quiet swap.

---

## 4. Public surface

Namespace: `Vestigium.Helpers.PerfMon.Disk`.

```csharp
public static class DiskPerf
{
    public static Task<SampleJobResult> RunAsync(
        DiskSampleOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class DiskSampleOptions
{
    public string Instance { get; init; } = "_Total";
    public bool IncludeDisks { get; init; }
    public int InstanceCap { get; init; } = 256;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}

public static class DiskPerfCatalog
{
    public const string AppId = "PerfMon.Disk";
    public const string Category = "PerfMon";
    public static void Register(VestigiumLoggerOptions cfg);
}
```

`DiskPaths` stays internal. Tests reach it through `InternalsVisibleTo`.

Empty `Instance` after trim becomes `_Total`. A caller-named instance (`0 C:`, `1`) is used as-is. `IncludeDisks` adds one row set per live instance excluding `_Total`, listed once at job start, capped at `InstanceCap`. Yank mid-job is Partial via shared.

No FileIo path. No volume letter API that invents an instance PDH does not have.

---

## 5. PhysicalDisk path set v1

Object: `PhysicalDisk` only.

| Counter | Instance | Unit | Prime |
|---|---|---|---|
| `Disk Bytes/sec` | `_Total` or named / disks | `/sec` | Yes |
| `Avg. Disk sec/Read` | same | `s` | Yes |
| `Avg. Disk sec/Write` | same | `s` | Yes |
| `Avg. Disk Queue Length` | same | `count` | Yes |
| `Current Disk Queue Length` | same | `count` | No |
| `% Disk Time` | same | `%` | Yes |

Latency stays seconds on the record. Hosts may display ms.

A known counter that is missing on the box is omitted from the short job (same habit as Cpu parking). Do not emit an Unavailable flood for an optional name. The six above are required for the short job: if the object exists and the counter does not, that row is Unavailable.

`% Disk Time` is not clamped.

---

## 6. Files

All under `src/Vestigium.Helpers.PerfMon.Disk/`. Small files. No hub.

| File | Role |
|---|---|
| `DiskObjects.cs` | Landed. 19 PDH names |
| `DiskCounterCatalog.cs` | Landed. Façade over `CounterSet` |
| `DiskSampleOptions.cs` | Probe options |
| `DiskPaths.cs` | PhysicalDisk short path set |
| `DiskPerf.cs` | Façade |
| `DiskPerfEvents.cs` | Constants, count by 5 |
| `DiskPerfLog.cs` | Internal |
| `DiskPerfCatalog.cs` | Replace skeleton `Register(object)` |
| `EventCatalog/disk.json` | Shard |

Tests: `src/Vestigium.Helpers.Tests/PerfMonDiskPD01Tests.cs` (already holds catalog fixtures).
Tests already reference this project. Do not add PerformanceCounter here.

---

## 7. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PD01.001** | P0 | `DiskObjects` + `DiskCounterCatalog` on shared `CounterSet`. | Landed. |
| **PD01.002** | P0 | `DiskPaths.Physical`. Empty instance → `_Total`. Named instance kept. Object is always `PhysicalDisk`. | Fixtures in §8. |
| **PD01.003** | P0 | `IncludeDisks`. Live instances at start only. Cap 256. Drop `_Total`. | Cap truncates. No throw. |
| **PD01.004** | P0 | Missing required counter on a present object is Unavailable, not a substitute LogicalDisk row. | No LogicalDisk category on the short job. |
| **PD01.005** | P0 | `DiskPerf.RunAsync` maps onto `SampleJob`. | Count=1 against fake returns frozen rows. |
| **PD01.006** | P1 | Logging door. APPID `PerfMon.Disk`. Folder follows the host. | No Initialize. |
| **PD01.007** | P1 | EVENTID 19500–19545. `disk.json` agrees. | Count by 5. |
| **PD01.008** | P2 | Filter `PD01_`. Probe references shared only. | Green with no live disks. |

Live PDH smoke may exist as `PD01_005_live_total`. It returns when `PhysicalDisk` is missing. It does not close the plan.

---

## 8. Tests that close this plan

| Fixture | Covers |
|---|---|
| `PD01_001_*` | Catalog fingerprint. Landed |
| `PD01_002_empty_instance_becomes_total` | Default |
| `PD01_002_named_instance_is_kept` | `0 C:` |
| `PD01_002_paths_are_physical_disk` | No LogicalDisk leak |
| `PD01_003_disks_respect_cap` | Cap |
| `PD01_003_disks_omit_total` | No duplicate `_Total` |
| `PD01_004_missing_object_stays_physical` | No fallback |
| `PD01_005_run_uses_shared_job` | Façade |
| `PD01_006_register_is_host_only` | Door |
| `PD01_006_writes_noop_until_host_starts` | Logging |
| `PD01_007_catalog_matches_constants` | JSON |
| `PD01_008_probe_references_shared_only` | No Charts / FileIo / Analytics |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PD01_
```

---

## 9. EVENTID

Count by 5. Reserved 19500–19999. Used this plan: 19500–19545.

| Id | Name | Severity | When |
|---|---|---|---|
| 19500 | ProbeEnter | Debug | `RunAsync` entered |
| 19505 | ProbeStarted | Information | Paths built, job handed to shared |
| 19510 | PathsBuilt | Debug | Path count |
| 19515 | ProbeComplete | Information | Shared result returned |
| 19520 | ProbeCancelled | Information | Shared cancelled |
| 19525 | ProbeRejected | Error | Bad options, then throw |
| 19530 | ObjectMissing | Warning | PhysicalDisk absent on the box |
| 19535 | DisksCapped | Warning | Instance list truncated |
| 19540 | CounterOmitted | Debug | Optional name not on the box |
| 19545 | ProbeFailed | Error | Unexpected after start |

Do not log the sample vector.

---

## 10. Still parked

| Item | Why |
|---|---|
| LogicalDisk `% Free Space` job | Later plan |
| ReFS / Storage Spaces / Storport / VHD | Catalog only |
| SMART / NVMe health | Not PDH |
| FileIo copy timing | Neighbor |
| Analytics reduction | Neighbor |
| Publish 0.1.0 | After a host consumes this |

---

## 11. Acceptance

PD01.001 landed. PD01.002–005 and 006–008 green against the fake. Live smoke present and silent on a box with no `PhysicalDisk`.

When this plan closes, move `PD01/` under `000 -- Archived/001 -- Implementation Plan/` and leave a pointer README in the live folder.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Locked. PhysicalDisk short job. Catalog already landed. |
