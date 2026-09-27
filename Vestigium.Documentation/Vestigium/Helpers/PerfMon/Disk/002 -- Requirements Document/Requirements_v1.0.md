# Vestigium.PerfMon.Disk — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-DISK-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.PerfMon.Disk` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample physical and logical disk rates, latency, and queue.

## 2. What this version is not

Not FileIo copy jobs. Not SMART vendor logs. Not a space-planner.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `DiskPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Disk`. EVENTID reserved 19500–19999. Count by 5. |
| 3 | Default instance | `_Total` unless the caller names one. |
| 4 | Counters v1 | `PhysicalDisk(*)\Disk Bytes/sec`, `Avg. Disk sec/Read`, `Avg. Disk sec/Write`, `Avg. Disk Queue Length`, `Current Disk Queue Length`, `% Disk Time`. LogicalDisk free-space counters are optional on the same job when asked. |
| 5 | Units | Bytes/sec, seconds, lengths, percent. Latency stays in seconds on the record; hosts may display ms. |
| 6 | Sibling boundary | FileIo measures a copy. This probe measures the disk object while anything runs. |

## 4. Non-goals

RAID controller CLI. iSCSI session setup. BitLocker status.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
