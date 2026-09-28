# Vestigium.Helpers.PerfMon.PageFile — Design

**Document ID:** VEST-HLP-PERFMON-PF-DSN-000
**Version:** 1.1
**Status:** Locked companion to SRS v1.1
**Date:** 28 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`PageFilePerf` names the `Paging File` object. Shared `CounterSet` owns live checks and the watch. Shared `SampleJob` owns the clock.

## 2. Why this object only

Operators ask "how full is pagefile.sys?" here. "Are we paging?" is `Memory\Pages/sec`, already on the Memory probe. Copying it onto this job would mix objects the same way LogicalDisk mixed into PhysicalDisk. Do not.

## 3. Tuesday-at-2am

No pagefile: category missing or instance list empty. Unavailable, not `0`. Multiple files (`C:\pagefile.sys`, `D:\pagefile.sys`) are PDH instance names, cap 256.

## 4. Still out

Creating files. Dump settings. Memory commit.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
| 1.1 | 28 Sep 2026 | No Memory companion rows. |
