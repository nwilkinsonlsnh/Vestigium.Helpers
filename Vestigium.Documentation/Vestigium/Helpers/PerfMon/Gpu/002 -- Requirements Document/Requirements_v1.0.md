# Vestigium.Helpers.PerfMon.Gpu — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-GPU-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Gpu` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample GPU engine utilization and dedicated memory that Windows already publishes.

## 2. What this version is not

Not NVML. Not ADL. Not DXGI factory enumeration as a public API. Not a frame-time plot.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `GpuPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Gpu`. EVENTID reserved 18500–18999. Count by 5. |
| 3 | Default instance | `first available engine or Unavailable` unless the caller names one. |
| 4 | Counters v1 | `GPU Engine(*)\Utilization Percentage` and `GPU Adapter Memory(*)\Dedicated Usage` when the categories exist. If neither category exists the job returns Unavailable and stops. It does not load a vendor DLL. |
| 5 | Units | Percent and bytes as published. |
| 6 | Sibling boundary | Shared has no GPU types. Only this package names GPU categories. |

## 4. Non-goals

Overclock. Temperature from vendor NVAPI. CUDA device count.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
