# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6 + PR12  
**Date:** 31 October 2026  
**Binding:** `Requirements_v1.6.md` wins on conflict

This page records *why* the library is shaped this way. It does not add requirements.

---

## 1. Intent

Hosts need workstation facts and protocol jobs without shelling out. One `net10.0` DLL serves Windows and Linux. The façade is `NetworkHelper`. Engines behind it stay internal.

```
host
  → NetworkHelper
       ├ Probe / inventory / snapshot / NetBIOS
       ├ IcmpEcho / Ping / IcmpTrace / Pathping
       ├ TcpConnect / UdpProbe / ProbeDns / WatchAdapter
       ├ ScanPorts / PortSet
       ├ PathMtu / SampleCounters / ProbeNeighbor
       ├ InferDevice / SummarizeTtl / ProbeSnmpSysDescr
       ├ LookupAsync (OS or wire)
       ├ tables: connections, stats, routes, neighbors
       ├ Add/Change/RemoveRoute          Option C
       ├ CreateEchoCampaign / OpenEchoCampaign
       ├ PlanShareProbe / CreateShareCampaign / OpenShareCampaign
       ├ prefix math                     SubnetEngine
       ├ MAC / OUI                       MacEngine + OuiLookupGuard + OuiPacked
       ├ bandwidth / P95                 BandwidthEngine + Analytics
       └ FileIo probes                   FileIoHelper.WriteProbe / AnalyzeDirectory
```

This library does not plot and will not grow a plot API.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| One façade, internal engines | Hosts cannot reach hooks, wire codecs, or IP Helper structs. |
| No process spawn | Output of `ping`/`ip`/`netsh`/`pathping` is not an API. |
| Bound ICMP when bind is set | BCL `Ping` cannot pin the NIC. Unbound jobs may keep Ping. |
| PMTU timeout is unknown | A dead host is not MTU 1. |
| ProbeNeighbor is one address | The table dump is a different job. |
| Pathping samples the settled protocol | ICMP samples of a TCP walk measure a different path. |
| Recipe persists bind | Open must send on the same NIC Create chose. |
| UdpProbe / ProbeDns / WatchAdapter | One datagram, one DNS ask, one NIC watch. Not a sweep. Not HTTP. Not a plot. |
| Default route is not offered | `0.0.0.0/0` and `::/0` must not come from this DLL. |
| No plot API | Never this library. |
| TcpConnect / UdpProbe are one port | Decision 41 stands on those doors. `ScanPorts` is the only list or range. |
| Sweep does not expand a CIDR | The calculator already answers that. A host tool expands and passes hosts. |
| No SYN | Connect scan only. Raw sockets are a different threat model. |
| Inference is pure | The scorer consumes evidence the caller already has. It does not probe, resolve a MAC, or walk hops. |
| TTL summary is an estimate | Conventional initials 64 / 128 / 255. Not a trace. |
| Banner is a short read | Optional, capped, off by default. No protocol conversation. |
| Confirm is a short HTTP probe | Known clear-text ports only. No TLS. Off by default. |
| Known UDP is one datagram | DNS and NTP only. Empty stays the default. |
| SNMP is one Get | `sysDescr` only. Community is never logged. |
| Local MTU vs path MTU | `NetworkAdapter.Mtu` is the NIC. `PathMtu` is the path. |

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` | Public façade |
| `EgressBind.cs` / `BoundIcmpEcho.cs` | Bind pin + bound echo |
| `PathMtuEngine.cs` | Too-big vs unknown |
| `NeighborResolve.cs` | One-address resolve |
| `UdpProbeEngine.cs` / `DnsProbeEngine.cs` / `AdapterWatchEngine.cs` | PR10 jobs |
| `DeviceInference.cs` / `TtlSummary.cs` / `TcpMss.cs` / `TcpBanner.cs` / `SnmpSysDescrEngine.cs` | PR11 doors |
| `PortSet.cs` / `PortScanEngine.cs` / `PortScanRollup.cs` / `ServiceConfirm.cs` / `UdpPayloads.cs` | PR12 doors |
| `Vestigium.Helpers.Network.csproj` | Version **2.0.0**. No plot package. |

---

## 6. What closed

| Pass | Outcome |
|---|---|
| PR09 | Package 1.1.0. |
| PR10 | Bound ICMP, PMTU status, neighbor resolve, pathping sample protocol, recipe bind, UdpProbe, ProbeDns, WatchAdapter. Package 1.2.0. |
| PR11 | Inference, TTL summary, TCP MSS, optional banner, SNMP sysDescr, local MTU documented. Package 1.7.0. |
| PR12 | `ScanPorts`, `PortSet`, rollup, optional discovery, confirm, known UDP. No SYN. No CIDR. Package 2.0.0. |

---

## 7. Still out of this DLL

Scheduler package. HTTP reachability beyond the short confirm probe. Demo gallery. Plot API. Packing the IEEE OUI registry. CIDR expansion inside the scanner. SYN or half-open scan. Default-route write. Actual hop walk from the inference path. Neighbor or MAC resolution from the inference path. SNMP walk or set. OS fingerprinting.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Package 1.1.0. |
| 1.6 + PR10-09 | 24 Sep 2026 | PR10 doors. Package 1.2.0. |
| 1.6 + PR11-07 | 28 Oct 2026 | PR11 doors. Package 1.7.0. |
| 1.6 + PR12-07 | 31 Oct 2026 | PR12 doors. Decision 41 amended only for `ScanPorts`. No SYN. Package 2.0.0. |
