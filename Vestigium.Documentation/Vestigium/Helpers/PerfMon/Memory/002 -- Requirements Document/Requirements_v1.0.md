# Vestigium.Helpers.PerfMon.Memory — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-MEM-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Memory` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample commit, available, cache, and pressure counters for the machine.

## 2. What this version is not

Not per-process working set (that is Processes later). Not pagefile sizing advice.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `MemoryPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Memory`. EVENTID reserved 19000–19499. Count by 5. |
| 3 | Default instance | `` unless the caller names one. |
| 4 | Counters v1 | `Memory\Available MBytes`, `Committed Bytes`, `Commit Limit`, `% Committed Bytes In Use`, `Cache Bytes`, `Pages/sec`. Working set is machine-level only in v1. |
| 5 | Units | Bytes or megabytes as the counter defines. Do not silently mix MB and bytes on one record. |
| 6 | Sibling boundary | PageFile probe owns `Paging File(*)\% Usage` and related. Memory may mention Pages/sec because that is the Memory object. |

## 4. Non-goals

Heap dumps. RAM map. Standby list wipe.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
