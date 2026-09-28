# Vestigium.Helpers.PerfMon.Memory — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-MEM-SRS-000
**Version:** 1.1
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Memory` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample machine commit, available, cache, and pressure counters Windows already publishes.

## 2. What this version is not

Not per-process working set (Processes later). Not pagefile sizing advice. Not Hyper-V host balloon control.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `MemoryPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Memory`. EVENTID reserved 19000–19499. Count by 5. |
| 3 | Catalog objects | `Cache`, `Hyper-V Dynamic Memory Integration Service`, `Memory`, `NUMA Node Memory`, `ReadyBoost Cache`. |
| 4 | Default instance | Memory and Cache are typically single-instance (`""`). NUMA / ReadyBoost / Hyper-V use live names. Do not invent `_Total` on Memory. |
| 5 | Default short job | `Memory` object: `Available MBytes`, `Committed Bytes`, `Commit Limit`, `% Committed Bytes In Use`, `Cache Bytes`, `Pages/sec`. Other catalog objects are live-listed this plan. |
| 6 | Units | Bytes, megabytes, percent, /sec as the counter defines. Do not silently mix MB and bytes on one record. |
| 7 | Sibling boundary | PageFile owns `Paging File(*)\% Usage`. Memory may still publish `Pages/sec` because that counter lives on the Memory object. |
| 8 | Missing object | Hyper-V / ReadyBoost / NUMA absent is empty / `false` / omit from the short job. Not a failed install. |

## 4. Non-goals

Heap dumps. RAM map. Standby list wipe. Guest-side Dynamic Memory *control*.

## 5. Acceptance

Catalog compiles on shared `CounterSet`. Short job is ME01.002+. No live RAM counters required to close catalog tests.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
| 1.1 | 27 Sep 2026 | Five Memory-family PDH objects. |
