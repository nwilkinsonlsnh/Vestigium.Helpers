# Vestigium.Helpers.PerfMon.Network — Requirements Specification

**Document ID:** VEST-HLP-PERFMON-NET-SRS-000
**Version:** 1.1
**Status:** Locked
**Date:** 28 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Network` 0.1.0 (not published)
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon`
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared rules in the `Vestigium.Helpers.PerfMon` SRS apply: no plot, no Initialize, no CLI spawn, Unavailable for missing instances, interval floor, 24 h cap, tests use a fake source.

## 1. Purpose

Sample adapter throughput, errors, queue, and the other Windows network PDH objects this catalog names.

## 2. What this version is not

Not ICMP ping, DNS lookup, traceroute, OUI, or workstation inventory. Those stay on `Vestigium.Helpers.Network`. Not a bandwidth bill. Not packet capture.

## 3. Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 1 | Façade | `NetworkPerf` static class. Hosts do not construct the PDH objects. |
| 2 | Identity | APPID `PerfMon.Network`. EVENTID reserved 17500–17999. Count by 5. |
| 3 | Catalog | Sixty-three PDH objects listed in `NetworkObjects.All` (adapter, TCP/IP, IPsec, HTTP, SMB, WFP, WinNAT, Teredo, Bluetooth, PacketDirect, Hyper-V VMBus, RemoteFX). |
| 4 | Default instance | `_Total` on `Network Interface` / `Network Adapter` unless the caller names one. |
| 5 | Default short job | `Network Interface`: Bytes Total/Received/Sent per sec, Packets/sec, receive/outbound errors, output queue. Other objects are live-listed this plan. |
| 6 | Units | Bytes and packets as published. Queue is a length. Do not convert to bits unless a host helper names bits. |
| 7 | Sibling | `Vestigium.Helpers.Network` stays protocol work. This probe is PDH series. Do not reference that project. |
| 8 | Missing object | IPsec / Teredo / WinNAT / PacketDirect absent is empty / omit. Not a failed install. |

## 4. Non-goals

Packet capture. Protocol decode. Remote adapter via RPC. Merging Helpers.Network ping into this job.

## 5. Acceptance

Catalog compiles on shared `CounterSet`. Short job is PN01.002+. No live NIC required to close catalog tests.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Initial lock. |
| 1.1 | 28 Sep 2026 | Sixty-three-object Network catalog. |
