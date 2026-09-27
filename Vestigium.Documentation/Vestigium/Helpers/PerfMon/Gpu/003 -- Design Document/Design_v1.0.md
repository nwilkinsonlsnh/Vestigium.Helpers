# Vestigium.PerfMon.Gpu — Design

**Document ID:** VEST-HLP-PERFMON-GPU-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`GpuPerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

Vendor SDKs split the family and fail on locked-down lab images. OS counters are enough to say 'busy or not' and 'how much dedicated memory'.

## 3. Tuesday-at-2am

Headless lab box, no GPU Engine category: Unavailable is the correct answer. Dual-GPU laptops: instance cap 256, names passed through, no merge of Intel + NVIDIA into one fake total.

## 4. Still out

Vendor SDKs. Encode/decode engine taxonomy beyond what the OS string already says.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
