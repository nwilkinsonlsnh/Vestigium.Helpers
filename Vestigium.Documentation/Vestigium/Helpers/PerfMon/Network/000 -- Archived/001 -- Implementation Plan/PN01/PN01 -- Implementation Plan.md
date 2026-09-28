# Vestigium.Helpers.PerfMon.Network — PN01 implementation plan

**Document ID:** VEST-HLP-PERFMON-NET-PLAN-PN01
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Network` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon.Network/`
**TFM:** `net10.0-windows`
**Depends on:** `Vestigium.Helpers.PerfMon` (PM01 clock, PM02 catalog)
**Fingerprint:** [`PM02 -- Catalog Fingerprint.md`](../../../PerfMon/001%20--%20Implementation%20Plan/PM02/PM02%20--%20Catalog%20Fingerprint.md)
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PN01 -- Backlog.md`](PN01%20--%20Backlog.md)

This probe names network PDH objects and returns shared `SampleRecord` rows. It does not own interval math and does not own ICMP.

---

## 1. Goal

A host can list the sixty-three objects, watch `Network Interface`, and run a short adapter job. Done when PN01 fixtures pass against a fake source and this project still has no `Vestigium.Helpers.Network` reference.

## 2. What this version is not

Ping, DNS, traceroute, packet capture, Charts, Analytics, NuGet publish.

## 3. Shape

```
host
  → NetworkCounterCatalog
       → CounterSet
  → NetworkPerf.RunAsync          later steps
       → SampleJob
```

Prefer `Network Interface`. Do not fall back to Helpers.Network.

## 4. Short job v1

Object: `Network Interface`. Default instance `_Total`.

| Counter | Unit | Prime |
|---|---|---|
| `Bytes Total/sec` | `/sec` | Yes |
| `Bytes Received/sec` | `/sec` | Yes |
| `Bytes Sent/sec` | `/sec` | Yes |
| `Packets/sec` | `/sec` | Yes |
| `Packets Received Errors` | `count` | No |
| `Packets Outbound Errors` | `count` | No |
| `Output Queue Length` | `count` | No |

`IncludeAdapters` adds live instances excluding `_Total`, cap 256.

## 5. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PN01.001** | P0 | `NetworkObjects` + `NetworkCounterCatalog`. | Landed with this lock. |
| **PN01.002** | P0 | `NetworkPaths`. Empty → `_Total`. Always `Network Interface`. | Fixture. |
| **PN01.003** | P0 | `IncludeAdapters`. Cap 256. Drop `_Total`. | Cap truncates. |
| **PN01.004** | P0 | Missing Interface stays Interface. No Helpers.Network swap. | Fixture. |
| **PN01.005** | P0 | `NetworkPerf.RunAsync`. | Count=1 against fake. |
| **PN01.006** | P1 | Logging door. APPID `PerfMon.Network`. | No Initialize. |
| **PN01.007** | P1 | EVENTID 17500–17545. `network.json`. | Count by 5. |
| **PN01.008** | P2 | Filter `PN01_`. Shared only. No Helpers.Network ref. | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PN01_
```

## 6. EVENTID

Reserved 17500–17999. Used this plan: 17500–17545.

| Id | Name | Severity | When |
|---|---|---|---|
| 17500 | ProbeEnter | Debug | enter |
| 17505 | ProbeStarted | Information | job handed to shared |
| 17510 | PathsBuilt | Debug | path count |
| 17515 | ProbeComplete | Information | done |
| 17520 | ProbeCancelled | Information | cancelled |
| 17525 | ProbeRejected | Error | bad options |
| 17530 | ObjectMissing | Warning | Network Interface absent |
| 17535 | AdaptersCapped | Warning | list truncated |
| 17540 | OptionalAbsent | Debug | IPsec / Teredo / WinNAT absent |
| 17545 | ProbeFailed | Error | unexpected after start |

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. 63-object catalog. Interface short job. |
