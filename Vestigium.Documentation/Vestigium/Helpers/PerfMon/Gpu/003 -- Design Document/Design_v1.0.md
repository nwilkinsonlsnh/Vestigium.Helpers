# Vestigium.Helpers.PerfMon.Gpu — Design

**Document ID:** VEST-HLP-PERFMON-GPU-DSN-000
**Version:** 1.1
**Status:** Locked companion to SRS v1.1
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`GpuPerf` names the Windows objects this probe reads. Shared `CounterSet` owns live checks and the watch. Shared `SampleJob` owns the clock. This package does not own interval math and does not call vendor SDKs.

## 2. Why these objects

| Object | Why |
|---|---|
| `GPU Engine` | Engine busy time / utilization. Instance names already carry pid, LUID, engine type. |
| `GPU Process Memory` | Per-process dedicated / shared / committed bytes. |
| `GPU Adapter Memory` | Adapter dedicated / shared / committed bytes. |
| `GPU Local Adapter Memory` | Local segment on the adapter. |
| `GPU Non Local Adapter Memory` | Non-local segment. |

Vendor SDKs split the family and fail on locked-down lab images. OS counters are enough to say busy-or-not and how much memory.

## 3. Tuesday-at-2am

Headless lab box, no `GPU Engine` category: Unavailable is the correct answer. Dual-GPU laptops: instance cap 256, names passed through, no merge of Intel + NVIDIA into one fake total. Instance yanked mid-job is Partial via shared.

## 4. Still out

Vendor SDKs. Encode/decode taxonomy beyond the OS `engtype_` string. Frame times.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
| 1.1 | 27 Sep 2026 | Five-object catalog. Opaque instance strings. |
