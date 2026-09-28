# Vestigium.Helpers.PerfMon.Network — Design

**Document ID:** VEST-HLP-PERFMON-NET-DSN-000
**Version:** 1.1
**Status:** Locked companion to SRS v1.1
**Date:** 28 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`NetworkPerf` names the Windows objects this probe reads. Shared `CounterSet` owns live checks and the watch. Shared `SampleJob` owns the clock.

## 2. Why this split

`Vestigium.Helpers.Network` already pings, traces, and resolves. That is a different job. This package is the PDH series for adapters and the stack objects Windows publishes next to them.

The catalog is large on purpose. Most lab boxes only have `Network Interface` / `Network Adapter` / `TCPv4`. IPsec, Teredo, WinNAT, PacketDirect, and Hyper-V VMBus stay named so a host can ask "is it here?" without a second library.

## 3. Tuesday-at-2am

Adapter yanked mid-job: Partial. `_Total` remains if PDH still has it. VPN adapter name with parentheses is passed through as-is. Do not parse it.

## 4. Still out

Wireshark. Helpers.Network `SampleCounters` merge. Bits-per-second as the stored unit.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
| 1.1 | 28 Sep 2026 | Full object list. Sibling boundary restated. |
