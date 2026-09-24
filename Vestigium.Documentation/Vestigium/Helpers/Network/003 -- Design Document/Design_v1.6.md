# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6 + PR07.009  
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
       ├ IcmpEcho / Ping / IcmpTrace
       ├ LookupAsync (OS or wire)
       ├ tables: connections, stats, routes, neighbors
       ├ Add/Change/RemoveRoute          Option C (see §2)
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
| No process spawn | Output of `ping`/`ip`/`netsh` is not an API. |
| `Ping` is an alias of `IcmpEcho` | PingIQ name. JSONL kind stays `icmpEcho`. |
| Continuous is `Count = 0` plus a duration cap | Unlimited + `Interval = 0` is a flood. |
| Campaign is a recipe, not a daemon | Process lifetime is the host’s. |
| Campaign paths under one root | `..` must not write `%WINDIR%` or `/etc`. |
| `NetworkTestHooks` internal | Plugins must not retarget ProgramData or `/proc`. |
| OUI allowlist + no redirect | SSRF. Packed snapshot is offline and incomplete. |
| DNS accept only the queried peer | UDP is connectionless. |
| No guessed IfIndex `1` | Wrong NIC. Linux write requires an index. Windows IPv6 uses caller `>= 1` or first up IPv6 NIC. Never an IPv4 table index on a v6 write. |
| Route write is Option C | Windows IPv4 IP Helper + HKLM persist (`NetworkRouteKeys`). Windows IPv6 `CreateIpForwardEntry2`. Linux netlink IPv4+IPv6. Cap/admin miss → `NetworkRouteDenied`. |
| Default route is not offered | `0.0.0.0/0` and `::/0` must not come from this DLL. |
| Prefix / MAC / bandwidth / share results are network facts | Addresses, RTTs, tables, prefixes. No plot type. |
| No plot API | Never this library. Not a deferred feature. |
| Share I/O is FileIo | Network does not open `FileStream`. No password field. |
| Logging is sparse | APPID Network. No packet bytes. No credentials. |

---

## 3. Job vs schedule

An ICMP **job** is `IcmpEchoOptions`. A campaign **recipe** is windows on a local clock. A **scheduler** (future package or host) wakes the process. Network does not install cron, schtasks, or systemd.

---

## 4. Exception policy

| Type | When |
|---|---|
| `NetworkRouteDenied` | No admin / no `CAP_NET_ADMIN` / default route / persist ACL |
| `ArgumentException` | Family mismatch, IPv4 dest required only when the other side is IPv4 |
| `ArgumentOutOfRangeException` | Prefix outside 0–32 (v4) or 0–128 (v6) |
| `InvalidOperationException` | Empty P95 samples |
| OUI HTTP miss | `Source = None`, not a throw |

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` | Public façade |
| `NetworkRouteMutation.cs` | Windows IPv4 write + persist via `NetworkRouteKeys` |
| `NetworkRouteWindowsV6.cs` | `CreateIpForwardEntry2` |
| `NetworkRouteNetlink.cs` | Linux IPv4+IPv6 |
| `NetworkRouteSpec.cs` / `NetworkRouteKeys.cs` | Parse + HKLM path |
| `ShareCampaign*.cs` / `ShareProbePlanner.cs` | Share campaigns |
| `OuiPacked.cs` / `_Data/oui-snapshot.txt` | Offline OUI stub |
| `Vestigium.Helpers.Network.csproj` | Json + Analytics + FileIo. No plot package. |

---

## 6. What closed

| Pass | Outcome |
|---|---|
| Phases 0–11 | Inventory through P95 |
| PR01 | Security harden |
| PR02 | Contract lock |
| PR03 | Share campaigns. Demo skipped. |
| PR04 | Packed OUI. Option C route write. |
| PR05.001–002 | Persist key. Requirements catch-up. |
| PR07 | Trace finish log. IPv6 IfIndex. Echo recipe persist. Events 14530–14540. Package 1.0.1. |

---

## 7. Still out of this DLL

Scheduler package. HTTP reachability. Demo gallery. Plot API (never this DLL). Repo portable test TFM / ubuntu workflow. Live Ubuntu route verification (PR05 §4).

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 | 19 Sep 2026 | First standalone Design. |
| 1.6 + PR02.001 | 19 Sep 2026 | Then: Windows write / Linux print. |
| 1.6 + PR04.001 | 19 Sep 2026 | Plotting is not a Network surface. |
| 1.6 + PR05.003 | 19 Sep 2026 | Option C + persist key + packed OUI. |
| 1.6 + PR07.009 | 24 Sep 2026 | IPv6 IfIndex. Echo recipe. Events through 14540. Plot API never offered. |
