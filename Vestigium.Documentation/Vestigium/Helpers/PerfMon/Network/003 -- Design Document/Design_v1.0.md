# Vestigium.Helpers.PerfMon.Network — Design

**Document ID:** VEST-HLP-PERFMON-NET-DSN-000
**Version:** 1.0
**Status:** Locked companion to SRS v1.0
**Date:** 27 September 2026
**Binding:** `Requirements_v1.0.md` wins on conflict

## 1. Intent

`NetworkPerf` names the Windows objects this probe reads. Shared runs the clock. This package does not own interval math.

## 2. Why these counters

Operators mix 'network health' with ping. Ping is reachability. These counters are the NIC. Splitting the packages keeps PingIQ from taking a PDH dependency it does not need.

## 3. Tuesday-at-2am

VPN adapters appear and vanish. Vanished instance is Partial on that tick, not a zero-byte success.

## 4. Still out

Wireless RSSI. QoS queues. RDMA.

## 5. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial. |
