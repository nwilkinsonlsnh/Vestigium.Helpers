# Vestigium.PerfMon.Network — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-NET-SRS-000
**Version:** 1.0
**Status:** Initial lock.
**Date:** 27 September 2026
**Package:** `Vestigium.PerfMon.Network` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample local adapter throughput, errors, discards, and output queue over a job.

## 2. What this version is not

Not ICMP, DNS, routes, OUI, or workstation inventory. Those stay on `Vestigium.Helpers.Network`. Not a bandwidth bill. Not a plot.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `NetworkPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Network`. EVENTID reserved 17500–17999. Count by 5. |
| 3 | Default instance | `_Total` unless the caller names one. |
| 4 | Counters v1 | `Network Interface(*)\Bytes Total/sec`, `Bytes Received/sec`, `Bytes Sent/sec`, `Packets/sec`, `Packets Received Errors`, `Packets Outbound Errors`, `Output Queue Length`. Current Bandwidth is metadata, not a rate to chart here. |
| 5 | Units | Bytes and packets as the counter defines. Queue is a length. Do not convert to bits unless the host asks a helper method that names bits. |
| 6 | Sibling boundary | `Helpers.Network.SampleCounters` remains a one-adapter protocol-adjacent snapshot. This probe is the series. |

## 4. Non-goals

Packet capture. Protocol decode. Remote adapter via RPC.

## 5. Acceptance

Project compiles and references shared. No live sampling required in this pass.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
