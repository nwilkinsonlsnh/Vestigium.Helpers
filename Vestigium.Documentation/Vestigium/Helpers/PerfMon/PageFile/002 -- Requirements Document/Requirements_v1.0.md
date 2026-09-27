# Vestigium.Helpers.PerfMon.PageFile — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-PF-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.PageFile` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample pagefile usage, peak, and paging rates tied to the paging-file object.

## 2. What this version is not

Not a recommendation engine for pagefile size. Not Memory.Available.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `PageFilePerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.PageFile`. EVENTID reserved 20000–20499. Count by 5. |
| 3 | Default instance | `_Total` unless the caller names one. |
| 4 | Counters v1 | `Paging File(*)\% Usage`, `% Usage Peak`. `Memory\Pages/sec`, `Page Reads/sec`, `Page Writes/sec` may be included as companion values labeled Memory so the host does not take both packages for one screen. |
| 5 | Units | Percent and pages/sec. |
| 6 | Sibling boundary | Memory owns commit and available. PageFile owns the paging-file object. Companion Memory paging rates are copies, not a second commit gauge. |

## 4. Non-goals

Creating or resizing pagefiles. Crash dump settings.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
