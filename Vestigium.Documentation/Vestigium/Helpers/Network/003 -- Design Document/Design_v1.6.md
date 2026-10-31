# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6 + PR13  
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
       ├ ScanPorts / PortSet / HostsFromPrefix / PortScanCsv
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
| Progress has a total | A host can show percent. `Sent` is expected probes after discovery. |
| OpenOnly is off | Closed and timed-out hits stay unless the caller asks for open only. |
| ReadBanner is separate from Confirm | Speak-first services need a read. HTTP confirm is a different ask. |
| Lab sets are names | `windows`, `printers`, `databases`. Still under the cap. |
| Host list is not a scan | `HostsFromPrefix` uses the calculator and returns strings. The scanner still does not expand a CIDR. |
| CSV is text | Lab notes need a table. No file write in the library. |
| No SYN in this DLL | Half-open is `Vestigium.Helpers.Network.RawSocket`. |

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` / `NetworkHelper.Hosts.cs` | Public façade. `HostsFromPrefix`. |
| `PortSet.cs` / `PortScanEngine.cs` / `PortScanRollup.cs` / `PortScanCsv.cs` | PR12 and PR13 doors |
| `Vestigium.Helpers.Network.csproj` | Version **2.0.0**. No plot package. |

---

## 6. What closed

| Pass | Outcome |
|---|---|
| PR12 | `ScanPorts`, `PortSet`, rollup, optional discovery, confirm, known UDP. No SYN. No CIDR. Package 2.0.0. |
| PR13 | Progress total, `OpenOnly`, `ReadBanner` on the scan, lab sets, `HostsFromPrefix`, CSV. SYN stays out. |

---

## 7. Still out of this DLL

Scheduler package. HTTP reachability beyond the short confirm probe. Demo gallery. Plot API. Packing the IEEE OUI registry. CIDR expansion inside the scanner. SYN or half-open scan. Default-route write. Actual hop walk from the inference path. Neighbor or MAC resolution from the inference path. SNMP walk or set. OS fingerprinting.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR12-07 | 31 Oct 2026 | PR12 doors. Decision 41 amended only for `ScanPorts`. No SYN. Package 2.0.0. |
| 1.6 + PR13-07 | 31 Oct 2026 | PR13 doors. Decisions 76–83. |
