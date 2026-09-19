# Vestigium.Helpers.Network — Design

**Document ID:** VEST-HLP-NETWORK-DSN-000  
**Version:** 1.6  
**Status:** Locked companion to SRS v1.6  
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
       ├ Add/Change/RemoveRoute          Windows write; Linux typed deny
       ├ CreateEchoCampaign / OpenEchoCampaign
       ├ prefix math                     SubnetEngine
       ├ MAC / OUI                       MacEngine + OuiLookupGuard
       └ bandwidth / P95                 BandwidthEngine + Analytics
```

HTTP reachability is HttpIQ, not this DLL. A later scheduler package starts jobs; this DLL only runs a job when called.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| One façade, internal engines | Hosts cannot reach hooks, wire codecs, or IP Helper structs. |
| No process spawn | Output of `ping`/`ip` is not an API. Privileges and parsing drift by distro. |
| `Ping` is an alias of `IcmpEcho` | PingIQ name. JSONL kind stays `icmpEcho`. |
| Continuous is `Count = 0` plus a duration cap | Unlimited + `Interval = 0` is a flood. Finite count may still burst. |
| Duration bands | Short jobs may be faster. Jobs longer than one minute must wait ≥ 1 s. Burst cannot buy a long flood. |
| Campaign is a recipe, not a daemon | Process lifetime is the host’s. Grace covers “woke up late.” |
| Campaign paths under one root | Recipe/results strings are host-supplied. `..` must not write `%WINDIR%` or `/etc`. |
| `Append` vs `AppendCampaign` | One-shot echo `StatsPath` is a host file. Campaign JSONL must not create directories above the root. |
| `NetworkTestHooks` internal | A published package must not let a plugin retarget ProgramData or `/proc`. |
| OUI allowlist + no redirect | `HttpClient.GetAsync` on a host string is SSRF. Default vendor host stays; custom hosts opt in. |
| DNS accept only the queried peer | UDP is connectionless. A local attacker can answer first. TXID alone is not enough. |
| No guessed IfIndex `1` | Interface 1 is often Loopback or absent. A write would hit the wrong NIC or fail opaquely. |
| Linux route write is typed deny | v1 does not parse `ip route`. Print is enough. Write lock is restated in PR02. |
| IPv6 mutate parked | IPv4 IP Helper row is what shipped. IPv6 write is PR04. |
| Prefix / MAC / bandwidth are pure | No wire, no files except optional OUI registry and stats JSONL. |
| Logging is sparse | APPID Network. No packet bytes. Campaign per-echo rows go to stats JSONL. |

---

## 3. Job vs schedule

An ICMP **job** is `IcmpEchoOptions`: target, count, interval, timeout, max duration.

A campaign **recipe** is windows on a local clock plus a date range. `RunAsync` asks “is this window in grace right now?” It does not sleep until midnight.

A **scheduler** (future package or host) wakes the process and calls `CreateEchoCampaign` / `IcmpEcho`. That split is intentional so Network can publish without owning service lifetime.

---

## 4. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` | Required reference is null. |
| `ArgumentException` | Bad MAC, bad IPv4 dest/gw, OUI URL policy, campaign path escape, burst past one minute, DNS label > 63 (existing). |
| `ArgumentOutOfRangeException` | Count < 0, timeout/buffer/TTL bounds, interval floors, MaxDuration ≤ 0 or > 24 h, prefix length, InterfaceIndex < 1. |
| `PlatformNotSupportedException` | Linux route write. NetBIOS paths that are Windows-only at the engine. |
| `NetworkRouteDenied` | Windows IP Helper access denied / invalid parameter; persistent-route ACL (typed deny on delete is PR01.010). |
| `FileNotFoundException` | OUI registry / recipe file missing after confine. |

OUI HTTP failures (timeout, 404, 3xx, HTML body) are **not** throws. Result `Source = None`.

---

## 5. Files

| File | Role |
|---|---|
| `NetworkHelper.cs` | Public façade |
| `NetworkInventoryEngine.cs` / types | Workstation snapshot |
| `IcmpEchoEngine.cs` / `IcmpEchoTypes.cs` | Echo job + duration guards |
| `IcmpTraceEngine.cs` | TTL walk |
| `DnsClient.cs` | OS lookup + RFC 1035 + peer bind |
| `NetworkStackEngine.cs` / Linux / Windows tables | Connections, routes print, neighbors |
| `NetworkRouteMutation.cs` | Windows write; fail-closed interface |
| `IcmpEchoCampaign.cs` / `CampaignPaths.cs` / `CampaignJsonl.cs` | Recipe + confined JSONL |
| `SubnetEngine.cs` | Prefix math |
| `MacEngine.cs` / `OuiLookupGuard.cs` / `OuiRegistry.cs` | EUI + live/file OUI |
| `BandwidthEngine.cs` / `PercentileBillEngine.cs` | Rate math + P95 via Analytics |
| `NetworkTestHooks.cs` | Internal test injection |
| `HelperLog.cs` / `NetworkLog.cs` / `NetworkCatalog.cs` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`. PR01 fixtures: `NetworkPR01Tests.cs`, `NetworkPR01RouteTests.cs`, `NetworkPR01CampaignPathTests.cs`.

---

## 6. What closed to reach 1.6

| Pass | Outcome |
|---|---|
| Phases 0–11 | Inventory, ICMP, DNS, tables, campaigns, snapshot, subnet, MAC/bandwidth, P95 in the tree |
| PR01.001 | OUI HTTPS allowlist, no redirect, 4 KiB, blocked ranges |
| PR01.002 | Hooks internal |
| PR01.003 | Duration bands + interval floors + `AllowBurst` |
| PR01.004 | DNS UDP/TCP peer bind |
| PR01.005 | Route interface fail-closed; `FirstIpv4Index` no longer guesses `1` |
| PR01.006 | Campaign path confine + `AppendCampaign` |
| PR01.007 | These three live docs |

Still open on the PR01 plan: OUI file size/row cap, Reject-line path redact, persistent-delete access-denied typing.

---

## 7. Still out

Linux `ip route` writer. IPv6 route mutate (PR04). Share-transfer campaigns (PR03). Scheduler package. HTTP client. Following OUI redirects. Public test hooks. Demo tabs as a library requirement.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.6 | 19 Sep 2026 | First standalone Design. Lifted from archived Guide v1.2 + shipped phases + PR01.001–006. |
