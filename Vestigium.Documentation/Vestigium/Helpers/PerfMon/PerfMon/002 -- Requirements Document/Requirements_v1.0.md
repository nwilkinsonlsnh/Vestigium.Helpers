# Vestigium.Helpers.PerfMon — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-SRS-000
**Version:** 1.0
**Status:** Initial lock. Implementation follows this file.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon` 0.1.0 (not published)
**TFM:** `net10.0-windows` (.NET 10 LTS)
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

If implementation and this file disagree, this file wins except where a later plan in `001 -- Implementation Plan` amends a lock and lands the amendment here in the same change.

This package is a **library of resources**, not a tool. Hosts subscribe. It does not spawn `perfmon.exe`, `typeperf.exe`, `wmic`, or PowerShell.

This library does not plot and will not grow a plot API.

---

## 1. Purpose

Give the six probe libraries one sample record, one timed job, one counter source, and one logging door so Cpu / Gpu / Memory / Disk / PageFile / Network do not each invent a loop.

## 2. What this version is not

Not a dashboard. Not Analytics. Not Charts. Not process listing (`Vestigium.Helpers.Processes`). Not ICMP or adapter inventory (`Vestigium.Helpers.Network`). Not a Linux perf backend. Not NVIDIA NVML / AMD ADL as a v1 source.

## 3. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Library | `net10.0-windows`. Tools subscribe. No CLI host in this project. No Demo project. |
| 2 | Family | Six probe packages plus this shared package. Probe packages reference shared. Shared does not reference a probe. |
| 3 | Names | `Vestigium.Helpers.PerfMon` and `Vestigium.Helpers.PerfMon.{Probe}`. Same pattern as FileIo / Network / Analytics. Not a top-level `Vestigium.PerfMon` product. |
| 4 | Source | Windows Performance Data Helper / `System.Diagnostics.PerformanceCounter` behind `ICounterSource`. Hosts may inject a fake in tests. |
| 5 | Missing category | First-class `SampleStatus.Unavailable`. Not an exception when the OS has no instance. |
| 6 | Bad argument | Empty name, non-finite interval, inverted window → log Failed, then throw. Same `HelperGuard` habit as the rest of Helpers. |
| 7 | Job | One probe set, one interval, one duration or count or token. First limit wins. |
| 8 | Interval floor | Default 1 s. Faster than 200 ms is rejected unless `AllowBurst` is set. Burst cap is 50 ms. |
| 9 | Duration cap | 24 h in-process. Longer is a host campaign, not this loop. |
| 10 | Time | Sample UTC on the record. Local display is the host. |
| 11 | Instance | `_Total` is allowed. `*` expansion is a probe concern and must return a bounded list. Default cap 256 instances. |
| 12 | Logging | Never call `VestigiumLogger.Initialize`. `PerfMonCatalog.Register` is host-only. APPID stamp is `PerfMon`. Folder follows the host. |
| 13 | EVENTID | Reserved 17000–17499. Count by 5. |
| 14 | Analytics | Optional later. This package does not compute P95, UCL, or control limits. Hosts pass arrays to `Vestigium.Helpers.Analytics`. |
| 15 | Charts | Never this family. |
| 16 | Tests | Must not require live ProgramData. Must not fail CI because a counter category is missing on the agent. Fake source is the gate. |
| 17 | Privileges | Read what the caller token can read. Missing permission is `Unavailable`, not a hang. |
| 18 | Thread | Sampler is async. PerformanceCounter reads stay off the WPF dispatcher. |

---

## 4. Public surface (v1 intent)

| Type | Role |
|---|---|
| `SampleRecord` | Utc, machine, category, counter, instance, value, unit, status |
| `SampleStatus` | Ok, Unavailable, Partial, Cancelled, Rejected |
| `SampleJob<T>` | Interval + duration/count/token. `RunAsync` |
| `ICounterSource` | Read one path now |
| `PerfMonCatalog` | APPID + `Register` |
| `PerfMonEvents` | EVENTID constants |

Exact member names may tighten in implementation if the SRS table still matches.

## 5. Non-goals

Plotting. Billing. Packet capture. Process kill. GPU vendor SDK. Cross-machine RPC. Installing perfmon Data Collector Sets. Writing counter values.

## 6. Acceptance

Skeleton projects compile in `Vestigium.Helpers.slnx` under `Library/PerfMon`. Documents exist under `Vestigium.Documentation/Vestigium/Helpers/PerfMon/`. Sampling code is a later plan, not this lock.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial family lock. |
| 1.0a | 27 Sep 2026 | Names locked to `Vestigium.Helpers.PerfMon*`. |
