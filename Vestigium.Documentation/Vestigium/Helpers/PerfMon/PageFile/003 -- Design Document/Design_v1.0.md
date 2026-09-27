# Vestigium.PerfMon.PageFile — Design

**Document ID:** VEST-HLP-PERFMON-PF-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`PageFilePerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

Operators ask 'are we paging?' and 'how full is pagefile.sys?' as one question. The objects are still two. This package answers both and keeps commit in Memory.

## 3. Tuesday-at-2am

No pagefile: category present with no instance or Usage unavailable. Unavailable, not zero percent.

## 4. Still out

Multiple pagefiles on specific volumes beyond PDH instance names.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
