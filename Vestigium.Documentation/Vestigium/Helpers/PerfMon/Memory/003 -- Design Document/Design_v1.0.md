# Vestigium.Helpers.PerfMon.Memory — Design

**Document ID:** VEST-HLP-PERFMON-MEM-DSN-000
**Version:** 1.1
**Status:** Locked companion to SRS v1.1
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`MemoryPerf` names the Windows objects this probe reads. Shared `CounterSet` owns live checks and the watch. Shared `SampleJob` owns the clock.

## 2. Why these objects

| Object | Why |
|---|---|
| `Memory` | Commit, available, cache bytes, pages/sec. The short job. |
| `Cache` | File-system cache efficiency, not the Memory `Cache Bytes` counter. |
| `NUMA Node Memory` | Per-node free/available on multi-socket boxes. |
| `ReadyBoost Cache` | Present only when ReadyBoost is in use. |
| `Hyper-V Dynamic Memory Integration Service` | Guest-visible Dynamic Memory. Absent on a physical box is normal. |

Commit percent without commit limit is a toy number. Both values travel on the same tick.

## 3. Tuesday-at-2am

32-bit view of a 64-bit box is a host problem. This library is AnyCPU / 64-bit host expected. ReadyBoost or Hyper-V missing is omit, not an error. NUMA instance yanked mid-job is Partial via shared.

## 4. Still out

Per-process private bytes. PageFile `% Usage`. Balloon *writes*.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
| 1.1 | 27 Sep 2026 | Five-object catalog. PageFile stays a sibling. |
