# Vestigium.Helpers.PerfMon.PageFile — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-PF-SRS-000
**Version:** 1.1
**Status:** Locked
**Date:** 28 September 2026
**Package:** `Vestigium.Helpers.PerfMon.PageFile` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample pagefile usage and peak on the Windows `Paging File` object.

## 2. What this version is not

Not a recommendation engine for pagefile size. Not Memory.Available. Not `Memory\Pages/sec`.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `PageFilePerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.PageFile`. EVENTID reserved 20000–20499. Count by 5. |
| 3 | Catalog object | `Paging File` only. |
| 4 | Default instance | `_Total` unless the caller names one (`C:\pagefile.sys`). |
| 5 | Default short job | `% Usage`, `% Usage Peak`. |
| 6 | Units | Percent. |
| 7 | Sibling | Memory owns commit, available, and `Pages/sec`. This probe does not copy those counters. |
| 8 | Missing object | No pagefile: Unavailable, not zero percent. |

## 4. Non-goals

Creating or resizing pagefiles. Crash dump settings. Mixing Memory object rows onto this job.

## 5. Acceptance

Catalog compiles on shared `CounterSet`. Short job is PF01.002+. No live pagefile required to close catalog tests.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
| 1.1 | 28 Sep 2026 | Paging File only. Memory rates stay on Memory. |
