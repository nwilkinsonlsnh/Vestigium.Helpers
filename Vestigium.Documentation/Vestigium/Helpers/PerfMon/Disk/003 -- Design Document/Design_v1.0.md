# Vestigium.Helpers.PerfMon.Disk — Design

**Document ID:** VEST-HLP-PERFMON-DISK-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`DiskPerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

Physical vs logical are different objects. v1 exposes both and labels the object on the record so Charts cannot mix them.

## 3. Tuesday-at-2am

USB disk yanked mid-job: Partial. `_Total` remains if PDH still has it.

## 4. Still out

NVMe SMART. Storage Spaces jobs.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
