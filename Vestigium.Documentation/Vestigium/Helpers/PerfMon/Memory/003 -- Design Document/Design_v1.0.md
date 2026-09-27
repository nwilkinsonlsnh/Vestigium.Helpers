# Vestigium.PerfMon.Memory — Design

**Document ID:** VEST-HLP-PERFMON-MEM-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`MemoryPerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

Commit percent without commit limit is a toy number. Both values travel on the same tick.

## 3. Tuesday-at-2am

32-bit view of a 64-bit box is a host problem. This library is AnyCPU / 64-bit host expected.

## 4. Still out

Per-process private bytes.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
