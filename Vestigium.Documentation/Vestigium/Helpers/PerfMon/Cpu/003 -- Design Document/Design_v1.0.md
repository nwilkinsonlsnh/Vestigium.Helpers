# Vestigium.PerfMon.Cpu — Design

**Document ID:** VEST-HLP-PERFMON-CPU-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`CpuPerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

`% Processor Time` needs a prime read. That prime lives in shared source or this probe, not in every host.

## 3. Tuesday-at-2am

Core count changes under a live VM. Instance list is refreshed at job start only in v1. Mid-job topology change is Partial.

## 4. Still out

Per-process CPU. Hardware PMC.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
