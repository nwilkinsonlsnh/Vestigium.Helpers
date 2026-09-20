# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6 + PR04.001  
**Date:** 19 September 2026  
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
       ├ Add/Change/RemoveRoute          Windows IPv4 write; Linux typed deny
       ├ CreateEchoCampaign / OpenEchoCampaign
       ├ PlanShareProbe / CreateShareCampaign / OpenShareCampaign
       ├ prefix math                     SubnetEngine
       ├ MAC / OUI                       MacEngine + OuiLookupGuard
       ├ bandwidth / P95                 BandwidthEngine + Analytics
       └ FileIo probes                   FileIoHelper.WriteProbe / AnalyzeDirectory

host (optional)
  → Vestigium.Helpers.Charts            plots Network numbers; not referenced here
```

HTTP reachability is HttpIQ, not this DLL. A later scheduler package starts jobs; this DLL only runs a job when called. Plotting is the host.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| One façade, internal engines | Hosts cannot reach hooks, wire codecs, or IP Helper structs. |
| No process spawn | Output of `ping`/`ip` is not an API. |
| `Ping` is an alias of `IcmpEcho` | PingIQ name. JSONL kind stays `icmpEcho`. |
| Continuous is `Count = 0` plus a duration cap | Unlimited + `Interval = 0` is a flood. |
| Duration bands | Jobs longer than one minute must wait ≥ 1 s. |
| Campaign is a recipe, not a daemon | Process lifetime is the host’s. |
| Campaign paths under one root | `..` must not write `%WINDIR%` or `/etc`. |
| `NetworkTestHooks` internal | Plugins must not retarget ProgramData or `/proc`. |
| OUI allowlist + no redirect | SSRF. |
| DNS accept only the queried peer | UDP is connectionless. |
| No guessed IfIndex `1` | Wrong NIC. |
| Linux route write is typed deny | v1 does not speak netlink. Option A in PR04. |
| IPv6 mutate parked | Option A. Open B/C only in writing on the PR04 plan. |
| Prefix / MAC / bandwidth are numbers | No wire, no plot control. |
| **No Charts reference** | Network returns `BandwidthAmount`, `PercentileBill`, `ShareCampaignResult`. A host that wants a picture calls Charts. |
| Share I/O is FileIo | Network does not open `FileStream`. |
| Logging is sparse | APPID Network. No packet bytes. No credentials. |

---

## 3. Job vs schedule

An ICMP **job** is `IcmpEchoOptions`. A campaign **recipe** is windows on a local clock. A **scheduler** (future package or host) wakes the process. Network does not install cron, schtasks, or systemd.

---

## 4. Exception policy

Unchanged from PR02: `NetworkRouteDenied` for Linux / ACL route write; `InvalidOperationException` for empty P95; OUI HTTP failures are `Source = None`, not throws.

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` | Public façade |
| `ShareCampaign.cs` / `ShareCampaignTypes.cs` / `ShareCampaignEngine.cs` / `ShareProbePlanner.cs` | Share campaigns |
| `NetworkRouteMutation.cs` | Windows IPv4 write; Linux typed deny |
| `BandwidthEngine.cs` / `PercentileBillEngine.cs` | Rate math + P95 via Analytics |
| `Vestigium.Helpers.Network.csproj` | Json + Analytics + FileIo. **Not Charts.** |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 6. What closed

| Pass | Outcome |
|---|---|
| Phases 0–11 | Inventory through P95 |
| PR01 | Security harden |
| PR02 | Route contract + remaining harden |
| PR03 | Share campaigns. Demo skipped. |
| PR04.001 | Charts stay on the host |

---

## 7. Still out of this DLL

Linux netlink / IPv6 route write unless PR04 Option B/C is chosen in writing. Scheduler package. HTTP client. Demo gallery. **Charts.** Repo-wide portable test TFM / ubuntu workflow.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 | 19 Sep 2026 | First standalone Design. |
| 1.6 + PR02.001 | 19 Sep 2026 | Route contract. |
| 1.6 + PR04.001 | 19 Sep 2026 | No Charts reference. Hosts plot results. |
