# Vestigium.Helpers.PerfMon.Gpu — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-GPU-SRS-000
**Version:** 1.1
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Gpu` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample GPU engine time and adapter / process memory that Windows already publishes.

## 2. What this version is not

Not NVML. Not ADL. Not DXGI factory enumeration as a public API. Not a frame-time plot.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `GpuPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Gpu`. EVENTID reserved 18500–18999. Count by 5. |
| 3 | Catalog objects | `GPU Engine`, `GPU Process Memory`, `GPU Adapter Memory`, `GPU Local Adapter Memory`, `GPU Non Local Adapter Memory`. |
| 4 | Default instance | No invented `_Total`. Use the caller name, or the live instance list capped at 256. Empty list is Unavailable, not a fake adapter. |
| 5 | Default short job | `GPU Engine\Utilization Percentage` plus `GPU Adapter Memory\Dedicated Usage` when those objects exist. Other catalog objects are live-listed this plan, sampled in later steps. |
| 6 | Missing GPU | Headless box: object missing is `false` / empty / Unavailable. Do not load a vendor DLL. |
| 7 | Units | Percent, 100-ns running time, and bytes as published. |
| 8 | Sibling boundary | Shared has no GPU types. Processes does not own GPU Engine PIDs. Instance strings stay opaque (`pid_…_luid_…_engtype_…`). |

## 4. Non-goals

Overclock. Temperature from NVAPI. CUDA device count. Merging Intel + NVIDIA into one fake total.

## 5. Acceptance

Catalog compiles on shared `CounterSet`. Short job is PG01.002+. No live GPU required to close catalog tests.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
| 1.1 | 27 Sep 2026 | Five GPU PDH objects. No invented `_Total`. |
