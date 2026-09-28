# PerfMon family — PR02c implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02C
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** PR02a generator
**Dump hits this paper uses**

| Category | Counters on USNCDTWIN01 |
|---|---|
| Network Adapter | 22 |
| Network Interface | 22 |
| IPv4 | 17 |
| IPv6 | 17 |
| ICMP | 27 |
| ICMPv6 | 33 |
| TCPv4 | 9 |
| TCPv6 | 9 |
| UDPv4 | 5 |
| UDPv6 | 5 |

The other 53 Network `NetworkObjects` entries and all five Gpu objects are **missing** on this dump. They stay name-only. Do not generate empty classes.

---

## 1. Goal

Those ten Network categories become typed classes (`NetworkAdapter.BytesTotalPerSec`). `NetworkPaths.InterfaceShort` points at `NetworkInterface` constants. Gpu catalog unchanged except a note that types wait on a GPU-capable dump.

## 2. What this version is not

Helpers.Network ping. WinNAT / IPsec / WFP / SMB / HTTP / Teredo / PacketDirect classes from thin air. Gpu engine types.

## 3. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02c.001** | P0 | Network shard for the ten present categories. | JSON has only those ten. |
| **PR02c.002** | P0 | Generate the ten classes. `NetworkCounterCatalog` known lists = class `Counters`. | Agreement tests. |
| **PR02c.003** | P0 | `NetworkPaths` uses `NetworkInterface.BytesTotalPerSec` and siblings. | `PN01_` green. |
| **PR02c.004** | P1 | Assert no type named `WinNat` / `GpuEngine` was generated. | Name-only remains. |
| **PR02c.005** | P2 | Filter `PR02c_`. No Helpers.Network project ref. | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02c_
```

## 4. Still parked

Second dump from a box that has GPU Engine and (optionally) IPsec / SMB / Hyper-V. When that dump exists, append categories to the shard and regenerate. Do not widen the probe allow-list.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Ten Network types. Gpu deferred. |
