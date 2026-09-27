# Vestigium.Helpers.PerfMon.Cpu — PC01 implementation plan

**Document ID:** VEST-HLP-PERFMON-CPU-PLAN-PC01
**Version:** 1.0
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Cpu` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.Cpu/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 landed)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PC01 -- Backlog.md`](PC01%20--%20Backlog.md)

This probe names counters and returns shared `SampleRecord` rows. It does not own interval math, prime reads, or PDH construction.

---

## 1. Goal

A host can:

1. Ask `_Total` utilization, privileged/user split, and queue length.
2. Opt in to per-core rows, capped at 256.
3. Get frozen `SampleJobResult` from shared `SampleJob`.
4. Register events inside `VestigiumLogger.Initialize`.

Done when the fixtures in §7 pass against a fake `ICounterSource` and this project still has no Charts / Analytics / Processes reference.

## 2. What this version is not

- A second sample loop
- A process table or PID filter
- Thread stacks, ETW, PMC
- Affinity or frequency writes
- Charts or Analytics
- NuGet publish

If a step needs one of those, it is the wrong step.

---

## 3. Shape

```
host
  → CpuPerf.RunAsync(options)
       → CpuPaths.For(options)          this package
       → new SampleJob(paths, shared)   Vestigium.Helpers.PerfMon
            → ICounterSource            shared PDH or test fake
       → SampleJobResult
CpuPerfCatalog.Register is host-only
```

Shared already primes rate counters. This package does not call `NextValue` itself.

---

## 4. Public surface

Namespace: `Vestigium.Helpers.PerfMon.Cpu`.

```csharp
public static class CpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        CpuSampleOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class CpuSampleOptions
{
    public string Instance { get; init; } = "_Total";
    public bool IncludeCores { get; init; }
    public bool IncludeParking { get; init; } = true;
    public int InstanceCap { get; init; } = 256;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
}

public static class CpuPerfCatalog
{
    public const string AppId = "PerfMon.Cpu";
    public const string Category = "PerfMon";
    public static void Register(VestigiumLoggerOptions cfg);
}
```

`CpuPaths` stays internal. Tests reach it through `InternalsVisibleTo`.

Empty `Instance` after trim becomes `_Total`. A caller-named instance is used as-is. `IncludeCores` adds one row set per instance from `ListInstances`, excluding `_Total`, capped at `InstanceCap`. Topology is listed once at job start. Mid-job core add/remove is Partial via shared.

No method accepts a PID.

---

## 5. Path set v1

Preferred category: `Processor Information`. If that category is missing on the source, fall back to `Processor`. Queue stays on `System`.

| Counter | Object | Instance | Unit | Prime |
|---|---|---|---|---|
| `% Processor Time` | Processor Information / Processor | `_Total` or named / cores | `%` | Yes |
| `% Privileged Time` | same | same | `%` | Yes |
| `% User Time` | same | same | `%` | Yes |
| `Processor Queue Length` | System | `""` | `count` | No |
| `Parking Status` | Processor Information | same as utilization | `flag` | No |

Parking is omitted when the category or counter is missing. That is not a failed job. Do not invent `0`.

`%` values are what PDH reports after shared prime. This package does not clamp 0–100.

---

## 6. Files

All under `src/Vestigium.Helpers.PerfMon.Cpu/`. Small files. No hub.

| File | Role |
|---|---|
| `CpuSampleOptions.cs` | Probe options. Shared job options are mapped, not copied as a second clock. |
| `CpuPaths.cs` | Builds the `CounterPath` list |
| `CpuPerf.cs` | Façade |
| `CpuPerfEvents.cs` | Constants, count by 5 |
| `CpuPerfLog.cs` | Internal. Same habit as `PerfMonLog` |
| `CpuPerfCatalog.cs` | Replace skeleton `Register(object)` |
| `EventCatalog/cpu.json` | Shard |

Tests: `src/Vestigium.Helpers.Tests/PerfMonCpuPC01Tests.cs`.
Add a project reference from Tests to `Vestigium.Helpers.PerfMon.Cpu`.

This project already references shared. Do not add PerformanceCounter here. Shared owns PDH.

---

## 7. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PC01.001** | P0 | Options + `_Total` path set. Empty instance → `_Total`. | Fixtures in §8. |
| **PC01.002** | P0 | Category fallback `Processor Information` → `Processor`. Queue always `System`. | Fake without PI still emits Processor paths. |
| **PC01.003** | P0 | `IncludeCores`. `ListInstances` at start only. Cap 256. Drop `_Total` from the core list. | Cap truncates. No throw. |
| **PC01.004** | P0 | Parking included only when present. Missing parking is skip, not Unavailable flood. | Job with no parking counter still Ok. |
| **PC01.005** | P0 | `CpuPerf.RunAsync` maps onto `SampleJob`. Source default is shared PDH. | Count=1 against fake returns frozen rows. |
| **PC01.006** | P1 | Catalog + log door. APPID `PerfMon.Cpu`. Folder follows the host. | No Initialize in this library. |
| **PC01.007** | P1 | EVENTID 18000–18045. `cpu.json` agrees. | Count by 5. |
| **PC01.008** | P2 | Tests project reference. Filter `PC01_`. | Green with no live counters. |

Live PDH smoke may exist as `PC01_005_live_total`. It returns when the category is missing. It does not close the plan.

---

## 8. Tests that close this plan

All against `FakeCounterSource` unless noted.

| Fixture | Covers |
|---|---|
| `PC01_001_empty_instance_becomes_total` | Default |
| `PC01_001_named_instance_is_kept` | Named core |
| `PC01_002_falls_back_to_processor` | Category miss |
| `PC01_003_cores_respect_cap` | Cap |
| `PC01_003_cores_omit_total` | No duplicate `_Total` |
| `PC01_004_missing_parking_is_omitted` | Optional counter |
| `PC01_005_run_uses_shared_job` | Façade |
| `PC01_005_pid_is_not_an_api` | No PID member on options |
| `PC01_006_register_is_host_only` | Door |
| `PC01_006_writes_noop_until_host_starts` | Logging |
| `PC01_007_catalog_matches_constants` | JSON |
| `PC01_008_probe_references_shared_only` | No Charts / Analytics / Processes |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PC01_
```

---

## 9. EVENTID

Count by 5. Reserved 18000–18499. Used this plan: 18000–18045.

| Id | Name | Severity | When |
|---|---|---|---|
| 18000 | ProbeEnter | Debug | `RunAsync` entered |
| 18005 | ProbeStarted | Information | Paths built, job handed to shared |
| 18010 | PathsBuilt | Debug | Path count + category chosen |
| 18015 | ProbeComplete | Information | Shared result returned |
| 18020 | ProbeCancelled | Information | Shared cancelled |
| 18025 | ProbeRejected | Error | Bad options, then throw |
| 18030 | CategoryFallback | Information | PI missing, Processor used |
| 18035 | ParkingSkipped | Debug | Parking counter absent |
| 18040 | CoresCapped | Warning | Instance list truncated |
| 18045 | ProbeFailed | Error | Unexpected after start |

Do not log the sample vector.

---

## 10. Still parked

| Item | Why |
|---|---|
| Per-process CPU | Processes / later plan |
| `Processor Frequency` | Not in SRS v1 |
| Interrupt / DPC time | Later probe revision |
| C-state residency | Parking is the v1 stand-in |
| Analytics reduction | Neighbor |
| Publish 0.1.0 | After a host consumes this |

---

## 11. Acceptance

PC01.001–005 and 006–008 green against the fake. Live smoke present and silent on a box with no counters.

When this plan closes, move `PC01/` under `000 -- Archived/001 -- Implementation Plan/` and leave a pointer README in the live folder.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Locked. Shared clock. Cpu façade and path set only. |
