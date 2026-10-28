# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6 + PR10  
**Date:** 24 September 2026  
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
       ├ PathMtu / SampleCounters / ProbeNeighbor
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
| TcpConnect / UdpProbe are one port | Not a sweep. |

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` | Public façade |
| `EgressBind.cs` / `BoundIcmpEcho.cs` | Bind pin + bound echo |
| `PathMtuEngine.cs` | Too-big vs unknown |
| `NeighborResolve.cs` | One-address resolve |
| `UdpProbeEngine.cs` / `DnsProbeEngine.cs` / `AdapterWatchEngine.cs` | PR10 jobs |
| `Vestigium.Helpers.Network.csproj` | Version **1.2.0**. No plot package. |

---

## 6. What closed

| Pass | Outcome |
|---|---|
| PR09 | Package 1.1.0. |
| PR10 | Bound ICMP, PMTU status, neighbor resolve, pathping sample protocol, recipe bind, UdpProbe, ProbeDns, WatchAdapter. Package 1.2.0. |

---

## 7. Still out of this DLL

Scheduler package. HTTP reachability. Demo gallery. Plot API. Packing the IEEE OUI registry. Port sweep. Default-route write.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Package 1.1.0. |
| 1.6 + PR10-09 | 24 Sep 2026 | PR10 doors. Package 1.2.0. |
