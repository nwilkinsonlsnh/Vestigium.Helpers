# Vestigium.PerfMon.Cpu — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-CPU-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.PerfMon.Cpu` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample processor utilization, privileged/user split, queue length, and optional per-core rows.

## 2. What this version is not

Not a process table. Not thread stacks. Not ETW sampling.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `CpuPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Cpu`. EVENTID reserved 18000–18499. Count by 5. |
| 3 | Default instance | `_Total` unless the caller names one. |
| 4 | Counters v1 | `Processor Information(*)\% Processor Time`, `% Privileged Time`, `% User Time`, `Processor Queue Length` (System), `Parking Status` when present. |
| 5 | Units | Percent 0–100 as PDH reports after the required two-read prime. Queue is a length. |
| 6 | Sibling boundary | `Helpers.Processes` lists processes. This probe does not accept a PID in v1. |

## 4. Non-goals

CPU affinity changes. Frequency scaling writes. PerfSpect / VTune.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
