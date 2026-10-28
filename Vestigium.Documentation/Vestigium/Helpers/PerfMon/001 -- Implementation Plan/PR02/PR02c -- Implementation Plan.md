# PerfMon family — PR02c implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02C
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** [`PR02a`](PR02a%20--%20Implementation%20Plan.md)
**Dump hits this paper uses** (`USNCDTWIN01`)

| Category | Identifier | Counters |
|---|---|---|
| Network Adapter | `NetworkAdapter` | 22 |
| Network Interface | `NetworkInterface` | 22 |
| IPv4 | `IPv4` | 17 |
| IPv6 | `IPv6` | 17 |
| ICMP | `ICMP` | 27 |
| ICMPv6 | `ICMPv6` | 33 |
| TCPv4 | `TCPv4` | 9 |
| TCPv6 | `TCPv6` | 9 |
| UDPv4 | `UDPv4` | 5 |
| UDPv6 | `UDPv6` | 5 |

The other 53 `NetworkObjects` entries and all five Gpu objects are **missing** on this dump. They stay name-only. Do not generate empty classes. Do not add CLR Networking.

Preferred call site after this paper:

```csharp
NetworkAdapter.BytesTotalPerSec
NetworkInterface.BytesReceivedPerSec
```

---

## 1. Goal

Those ten Network categories become typed classes. `NetworkCounterCatalog` known lists for Adapter / Interface become the class `Counters` arrays. `NetworkPaths.InterfaceShort` points at `NetworkInterface` constants. Gpu catalog unchanged except the types wait on a GPU-capable dump.

`PN01_` stays green.

## 2. What this version is not

Helpers.Network ping. WinNAT / IPsec / WFP / SMB / HTTP / Teredo / PacketDirect classes invented from the allow-list. Gpu engine types. `.NET CLR Networking`.

## 3. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02c.001** | P0 | Network shard: only the ten present categories. | JSON has those ten. No WinNAT. |
| **PR02c.002** | P0 | Generate the ten classes under `Catalog/`. Wire `CounterSet` known lists. | Agreement tests. |
| **PR02c.003** | P0 | `NetworkPaths` uses `NetworkInterface.BytesTotalPerSec` and siblings. | `PN01_` green. |
| **PR02c.004** | P1 | Assert no generated type named `WinNat` or `GpuEngine`. | Name-only remains. |
| **PR02c.005** | P2 | Filter `PR02c_`. No Helpers.Network project ref. | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02c_
```

## 4. Still parked

Second dump from a box that has GPU Engine and (optionally) IPsec / SMB / Hyper-V. When that dump exists, append **only allow-listed** categories to the shard and regenerate. Do not widen `NetworkObjects.All` or `GpuObjects.All` in this paper.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Ten Network types. Gpu deferred. |
