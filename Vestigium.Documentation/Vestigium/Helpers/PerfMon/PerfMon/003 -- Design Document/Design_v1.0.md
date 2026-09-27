# Vestigium.Helpers.PerfMon — Design

**Document ID:** VEST-HLP-PERFMON-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records why the family is shaped this way. It does not add requirements.

---

## 1. Intent

```
host
  → probe façade (Cpu / Disk / …)
       → SampleJob
            → ICounterSource
                 → PerformanceCounter / PDH   or test fake
       → SampleRecord[]
host may send values to Analytics
host may send values to Charts
PerfMon never draws and never reduces
```

One loop. Six vocabularies.

## 2. Locked shape

| Decision | Why |
|---|---|
| Shared DLL separate from probes | Cpu should not take a GPU package. Hosts take what they sample. |
| `ICounterSource` | CI agents lack categories. Live PDH is not a unit-test gate. |
| Status on the record | A laptop without a discrete GPU is normal. Throwing there trains hosts to swallow everything. |
| Windows TFM | PDH is the v1 backend. Linux is a different source, not a `#if` inside the same class. |
| No vendor GPU SDK in shared | Shared must stay ignorant of NVIDIA / AMD / Intel. Gpu probe maps OS names only. |
| Job in shared | Six copies of interval math will drift. |
| EVENTID blocks per package | Flood identity and catalog shards stay separable. |
| Off dispatcher | PerformanceCounter is not a bindable collection. |

## 3. Boundary with neighbors

| Neighbor | Owns | PerfMon does not |
|---|---|---|
| `Vestigium.Helpers.Network` | ICMP, DNS, routes, inventory, one-shot `SampleCounters` / `WatchAdapter` | Adapter PDH series over a job |
| `Vestigium.Helpers.Processes` | Process table and lifetime | Processor `_Total` and per-core time |
| `Vestigium.Helpers.Analytics` | P95, fences, run rules | Reduction |
| `Vestigium.Helpers.Charts` | Draw | Draw |
| `Vestigium.Logging` | JSONL disk | Initialize |

## 4. Failure

| Case | Result |
|---|---|
| Category missing | `Unavailable` sample, Warning event, job continues if other instances remain |
| Instance vanished mid-job | `Partial` on that tick, job continues |
| Cancel | `Cancelled`, last good samples returned |
| Interval too fast | Reject + throw before the first read |
| Source throws unexpected | Error event, then throw. Do not invent a zero. |

Tuesday-at-2am: a VM with no `GPU Engine` category and a laptop with Intel + NVIDIA both present. Unavailable is success for the first. Instance list is bounded for the second.

## 5. Files (when implementation starts)

| File | Role |
|---|---|
| `SampleRecord.cs` | Record |
| `SampleStatus.cs` | Enum |
| `SampleJob.cs` | Loop |
| `ICounterSource.cs` | Port |
| `PerformanceCounterSource.cs` | Windows adapter, internal |
| `PerfMonCatalog.cs` / `PerfMonEvents.cs` / `PerfMonLog.cs` | Logging |
| `EventCatalog/perfmon.json` | Shard |

Tests live under `src/Vestigium.Helpers.Tests/` when the first probe lands.

## 6. Still out

Linux. Vendor GPU. Data Collector Set authoring. Remote computer PDH. Hot-path ETW sessions.

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
