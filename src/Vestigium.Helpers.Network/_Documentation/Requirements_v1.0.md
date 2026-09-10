# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.1  
**Status:** Draft. Supersedes the 9 September 2026 v1.0 draft. Awaiting acceptance before the public API grows past Identity + Probe.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**TFM:** `net10.0` (.NET 10 LTS). Not Windows-only; Windows-gated verbs are marked.  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost, and any other Vestigium diagnostic host that **subscribes** to this library

If implementation and this file disagree, this file wins.

This package is a **library of resources**, not a tool. PingIQ / DnsIQ / TraceIQ / ProbeHost reference it and call it. It is not `ping.exe`, `tracert.exe`, `netstat.exe`, `nbtstat.exe`, `arp.exe`, `route.exe`, `netsh.exe`, or `nslookup.exe`. Those programs are the *behavior reference*. Network speaks the **protocols** those programs use (ICMP Echo, ICMP Time Exceeded, DNS, ARP/ND, TCP/UDP tables, the OS routing table) and returns structured results plus JSONL statistics.

---

## 0. How to read this document

It records:

- one façade (`NetworkHelper`) that hosts subscribe to
- protocol-level jobs: ICMP Echo, ICMP TTL-walk, DNS (RFC 1035), ARP/ND, TCP/UDP tables, routing table
- a workstation snapshot: adapters, addresses, DHCP, DNS, gateways, NetBIOS-over-TCP
- `route` as a first-class cousin of `netstat -r` (print in v1; add/change/delete explicit)
- `netsh` in v1 as a **read-only informational snapshot**
- an ICMP Echo **campaign scheduler** with clock windows, a date range, and append-only JSONL stats
- `Vestigium.Helpers.Json` as the payload helper for campaign recipes (`.json`) and statistics (`.jsonl`)
- HelperLog only for the suite audit trail; Category `Helpers`; APPID `Network`
- statistics live in campaign JSONL, not as a second logger under `%ProgramData%\Vestigium\Logs\`
- no HTTP client product in v1

---

## 1. Purpose

Give every Vestigium host one subscribed API for workstation inventory and protocol jobs (ICMP, DNS, routing table) with structured results and JSONL statistics.

`NetworkHelper.Ping(...)` is an allowed alias of `IcmpEcho(...)`. Neither spawns `ping.exe`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Library | `net10.0` / .NET 10 LTS class library. Tools subscribe; this project does not ship a CLI host. |
| 2 | Protocols, not shells | ICMP Echo Request/Reply (RFC 792 IPv4, RFC 4443 IPv6). ICMP Time Exceeded + Echo for trace. RFC 1035 for DNS. ARP / NDP for neighbors. OS routing APIs for `route`. No `ping.exe` / `tracert.exe` / `route.exe` / `nslookup.exe` spawn. |
| 3 | Name Ping | Product nickname and PingIQ façade only. Protocol name in types, logs, and JSONL is `icmpEcho`. |
| 4 | Default echo count | **4**. Override `>= 1`, or `0` for continuous-until-cancelled. |
| 5 | Continuous | `Count = 0` until Cancel, CancellationToken, or MaxDuration. |
| 6 | Campaign scheduler | First-class. Clock windows + inclusive date range + append-only JSONL. See §6. |
| 7 | Who keeps time | Library computes due windows and runs ICMP when due. A host process must stay alive (ProbeHost / IHostedService). Network does not install Windows Task Scheduler jobs. |
| 8 | Statistics store | Campaign and job statistics are JSONL payload files via Vestigium.Helpers.Json. Not HelperLog, not CSV. |
| 9 | Json sibling | Network references Vestigium.Helpers.Json. Recipes `.json`. Results `.jsonl`. Compact lines (WriteIndented = false). |
| 10 | Append | One JSON object per line, append + flush. Do not load the whole history into a JsonSession to add one echo. |
| 11 | route | Print / list is required. Add / change / delete are explicit mutation APIs (admin, logged Warning). |
| 12 | netsh | Read-only informational snapshot. No set / add / delete. |
| 13 | Other mutations | ARP write, nbtstat cache reload, adapter enable/disable, static IP — not v1 (except route add/change/delete). |
| 14 | Logging door | HelperLog only for suite audit. Category Helpers. APPID Network. Never VestigiumLogger.Initialize. |
| 15 | Two JSONL channels | Audit = Vestigium.Logging under Logs\Network\. Stats = campaign/job .jsonl the host chose. Do not mix them. |
| 16 | IPv4 and IPv6 | First-class. |
| 17 | Probe | On-box only. |
| 18 | Analytics | Optional NumericSeries at window/job finalize. Charting stays in Charts. |
| 19 | Siblings | Json required. Analytics optional. Must not reference Charts, FileIo, Csv, ClosedXml, Encryption, WinReg, Processes in v1. |

---

## 3. Goals

G1. Hosts subscribe to one façade (`NetworkHelper`).  
G2. Reachability is ICMP Echo, not a process named ping.  
G3. Default 4 echoes; override count; continuous with cancel.  
G4. A campaign fires different echo counts at named local times across a date range and appends every echo and window summary to one JSONL file.  
G5. Routing table print matches `route print` / `netstat -r`. Explicit add/change/delete match `route add` / `change` / `delete`.  
G6. DNS can target a specified nameserver on UDP/TCP 53.  
G7. Json owns recipe and stats document shape; Network owns ICMP and the clock.  
G8. HelperLog stays sparse. Stats JSONL is the warehouse.  
G9. Probe stays on-box.

---

## 4. Workstation inventory

`GetWorkstation()` / `GetAdapters()` / `GetAdapter()` return adapter name, description, IPv4/IPv6, CIDR (`PrefixLength`) and IPv4 subnet mask, default gateway, MAC, DHCP (enabled, server, lease obtained/expires), DNS servers, NetBIOS-over-TCP, link speed, status. Snapshot is point-in-time.

---

## 5. Protocol jobs

### 5.2 ICMP Echo (behavior reference: ping)

Protocol: ICMP Echo Request / Echo Reply (ICMPv6 Echo for IPv6). Uses System.Net.NetworkInformation.Ping or raw ICMP sockets. Does not start ping.exe.

Default Count = 4. Count 0 = continuous until cancel. Optional StatsPath appends each echo + job summary as JSONL via Json.

### 5.3 ICMP Trace (behavior reference: tracert)

Send ICMP Echo with TTL = 1, 2, … and read ICMP Time Exceeded until Echo Reply or MaxHops. UDP fallback only when ICMP send is forbidden; record ProbeProtocol = Icmp | Udp.

### 5.4 DNS (behavior reference: nslookup non-interactive)

RFC 1035 on UDP 53, TCP 53 if truncated. Explicit Server for check-this-DNS-server. Types: A, AAAA, CNAME, MX, NS, PTR, SOA, TXT, SRV, ANY.

### 5.5 Connections (behavior reference: netstat)

Read-only TCP/UDP endpoints, states, PID, best-effort process name, statistics.

### 5.6 Neighbors (behavior reference: arp -a)

ARP cache (IPv4) and Neighbor Discovery cache (IPv6). Read-only in v1.

### 5.7 NetBIOS — Windows-only

Read name table, cache, remote query, sessions. Not v1: -R / -RR.

### 5.8 Routing table (behavior reference: route / netstat -r)

OS forwarding table, not a route.exe parse.

- GetRoutes: route print, route print -4 / -6. Fields: Destination, PrefixLength / Mask, Gateway, Interface, Metric, Family, IsPersistent, Protocol.
- AddRoute / ChangeRoute / DeleteRoute: explicit, HelperLog Warning, typed fail if access denied, no UAC prompt, never spawn route.exe.

### 5.9 Interface snapshot (behavior reference: netsh show)

Read-only adapters, DNS, routes, neighbors, WLAN radio/SSID/signal, WLAN profile names only. No keys, no netsh set.

---

## 6. ICMP Echo campaigns (scheduler)

Recipe plus runner. Inclusive dates, daily EchoWindow (LocalTime + Count), one target, one stats JSONL path, one recipe JSON path.

Required example:

| Local time | Count |
|---|---|
| 00:00 | 100 |
| 08:00 | 200 |
| 12:00 | 100 |
| 15:00 | 200 |
| 17:00 | 500 |
| 21:00 | 100 |

Range 2026-09-09 through 2026-09-30. Append every echo and every window summary to the same JSONL file.

Clock rules:
- DateOnly in campaign TimeZone (default local)
- Duplicate LocalTime rejected
- Window Count >= 1 (continuous not legal inside a window)
- Grace default 15 minutes. After grace write windowMissed; do not backfill hours later
- Idempotent: campaignId + date + localTime. Existing windowSummary or windowMissed means skip
- Host must stay alive. No schtasks.
- On RunAsync, only today’s due-and-in-grace windows. Do not replay the whole range.

Json:
- Recipe .json via JsonHelper (indented OK)
- Default folder %ProgramData%\Vestigium\Network\Campaigns\
- Stats .jsonl compact WriteIndented = false via JsonHelper.ToJson then append line
- Do not OpenJsonl + Save the whole file per echo
- Prefer JsonHelper.AppendJsonl when Json grows it (Json v1.1 request)
- Replay with JsonHelper.OpenJsonl
- Never write stats under Logs\

Line kinds: campaignStart, windowStart, echo, windowSummary, windowMissed, campaignEnd. Every line has kind, campaignId, recordedUtc.

One-shot jobs may set StatsPath and use the same append rules.

---

## 7. Public surface (v1.1)

NetworkHelper: Identity, Probe, GetWorkstation, GetAdapters, GetAdapter, IcmpEcho, Ping (alias), IcmpTrace, Trace (alias), CreateEchoCampaign, OpenEchoCampaign, LookupAsync, LookupManyAsync, GetConnections, statistics, GetRoutes, AddRoute, ChangeRoute, DeleteRoute, GetNeighbors, NetBIOS reads, GetInterfaceSnapshot.

IcmpEchoOptions.Count default 4. IcmpEchoCampaignOptions: Target, RangeStartDate, RangeEndDate, TimeZone, Windows, ResultsPath, RecipePath, Grace 15 min, Echo options.

---

## 8. Defaults

Echo Count 4 (reject < 0). Timeout 4s (10ms–60s). BufferSize 32 (1–65500). Interval 1s. Campaign window Count required >= 1. Grace 15 min (0–12h). Trace MaxHops 30. DNS Timeout 3s. HelperGuard then throw. Do not clamp.

---

## 9. Logging (audit channel)

Category Helpers. APPID Network. Sparse: recipe + summaries, campaign window start/end/miss. Never a HelperLog line per successful campaign echo. Stats warehouse is the campaign JSONL.

---

## 10. Demo

Gallery: adapters, ICMP Echo (default 4, continuous), ICMP Trace, DNS, connections, neighbors, routes (print + guarded add/delete), NetBIOS, snapshot, campaign editor + JSONL tail via JsonHelper.OpenJsonl. Startup runs Probe only.

---

## 11. Tests

Identity, on-box Probe, default 4 echoes to 127.0.0.1, Count override, Count 0 + cancel <= 5s, campaign idempotent window, OpenJsonl enumerates echo + windowSummary, missed window writes windowMissed, GetRoutes returns a list, no live ProgramData/Desktop, no public Internet required for default CI.

---

## 12. Non-goals (v1.1)

Spawning CLI tools. Windows Task Scheduler wrappers. HTTP/TLS client. ARP write, NetBIOS reload, adapter admin, static IP (except route mutations). Full netsh write. Packet capture, pathping, iperf, SNMP, port scanning. Stats under Logs\.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.1 | This document |
| v1.2 | Json AppendJsonl exclusively; campaign compare report |
| v1.3 | HTTP reachability for HttpIQ; optional OS-scheduler export XML |
| later | pathping-class, packet capture, DNS cache flush |

---

## 14. Mapping — CLI tool → protocol → API

| Tool | Protocol / plane | API |
|---|---|---|
| ping | ICMP Echo Request/Reply | IcmpEcho / Ping alias |
| tracert | ICMP Echo + Time Exceeded | IcmpTrace / Trace alias |
| route print | OS forwarding table | GetRoutes |
| route add/change/delete | OS forwarding table | AddRoute / ChangeRoute / DeleteRoute |
| netstat | TCP/UDP tables | GetConnections, statistics |
| netstat -r | OS forwarding table | GetRoutes |
| arp -a | ARP / ND cache | GetNeighbors |
| nbtstat | NetBIOS | NetBIOS methods (Windows) |
| nslookup | DNS RFC 1035 | LookupAsync |
| netsh show | Interface snapshot | GetInterfaceSnapshot |
| scheduled ping | ICMP Echo + clock | CreateEchoCampaign |

---

## 15. Acceptance

Draft until Accepted on main. Then: this file + Developers Guide under Network _Documentation, project references Vestigium.Helpers.Json, §6 line kinds stable, no ping.exe or route.exe spawn. Do not grow NetworkHelper past Identity + Probe until Accepted.
