# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.6  
**Status:** Locked companion to PR01–PR09.  
**Date:** 24 September 2026  
**Package:** `Vestigium.Helpers.Network` **1.1.0**  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md), [`Design_v1.6.md`](Design_v1.6.md)  
**Subnet lock:** [`ARCHIVE/PR01/SubnetCalculator_v1.3.md`](ARCHIVE/PR01/SubnetCalculator_v1.3.md)  
**MAC / bandwidth lock:** [`ARCHIVE/PR01/MacAndBandwidth_v1.4.md`](ARCHIVE/PR01/MacAndBandwidth_v1.4.md)  
**Share lock (shipped PR03):** [`ARCHIVE/PR01/ShareCampaign_v1.5.md`](ARCHIVE/PR01/ShareCampaign_v1.5.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost on Windows or Linux

If implementation and this file disagree, this file wins **except** where a later PR plan in this folder explicitly amends a lock and lands the amendment here in the same change. For prefix math, the subnet addendum wins over this summary. For MAC / bandwidth numbers, the v1.4 addendum wins. For share campaigns, the v1.5 addendum wins.

This package is a **library of resources**, not a tool. Hosts subscribe. It speaks **protocols** (ICMP Echo, ICMP Time Exceeded, DNS, ARP/ND, TCP/UDP tables, the OS routing table) and **prefix / MAC / bandwidth arithmetic**. It does not spawn `ping`, `ping.exe`, `traceroute`, `tracert.exe`, `pathping`, `ip`, `ss`, `netstat`, `route`, `nbtstat`, `arp`, `netsh`, `nslookup`, `ipcalc`, or `ipv6calc`.

A future `Vestigium.Scheduler` (name not locked) may start jobs. This library does not install cron, schtasks, or systemd units. This library does not plot and will not grow a plot API. Results are network facts.

---

## 2. Decisions locked in this version

Unchanged from PR08 except the PR09 locks below.

| # | Decision | Locked as |
|---|---|---|
| 29 | Route interface | No guessed IfIndex `1`. Caller may pass `InterfaceIndex >= 1`. Omit or `0` leaves the stack to choose. Never rewrite `0` to `1`. |
| 36 | Egress bind | Optional `InterfaceIndex` and `SourceAddress` on echo, trace, DNS, pathping, TcpConnect, and PMTU. Source, when set, must belong to that interface. |
| 37 | Trace family | Optional `RouteFamily` on trace and pathping. Unset keeps the current resolver. |
| 38 | Pathping | Walk then sample. Each hop reports RTT, hop loss, and link loss to the next hop. Link loss is never a negative gain. |
| 39 | PTR | Optional name on each trace and pathping hop. Miss stays empty. The hop still counts. |
| 40 | TCP probe | Third trace probe. ICMP, then UDP if ICMP is forbidden, then TCP if UDP is silent. Refused connect names the hop. No payload past the handshake. |
| 41 | TcpConnect | One host, one port. Connected, refused, or timed out, plus elapsed ms. No port loop. No HTTP. |
| 42 | Counter sample | One adapter, one duration. Bytes in/out, errors, discards. Optional interval samples. Does not bill. Does not plot. |
| 43 | Window duration | `EchoWindow` may carry `Duration`. Recipe writes `durationMs` only when set. Old recipes open as count-only. Both set: first limit wins. Neither: reject. |
| 44 | PMTU | Largest don't-fragment echo that passed. Uses the echo door and bind. Not a trace. |
| 45 | Neighbor probe | `ProbeNeighbor(address)` is one ARP/ND ask. MAC or none. `GetNeighbors` stays the table. |

---

## 5. Protocol jobs

PR09 façade additions: `Pathping`, `TcpConnect`, `SampleCounters`, `PathMtu`, `ProbeNeighbor`. Echo, trace, and DNS options carry bind. Trace and pathping carry family and TCP port. Probe protocol is ICMP, UDP, or TCP.

---

## 6. Campaigns

Clock windows + date range + timezone + 15 min grace. Recipe `.json`, results `.jsonl`. Create persists Echo options that Create already accepted. Window shape is `EchoWindow(LocalTime, Count, Duration?)`. Duration-per-window is shipped (PR09-06). Count stays on the window.

---

## 9. Logging

APPID Network. Sparse. Named events used through **14560** (`BindRejected`). Earlier IDs stand.

---

## 12. Non-goals

Spawn CLI tools on any OS. Install cron/schtasks/systemd units. HTTP reachability client. NetBIOS on Linux. Packet capture. Guessing IfIndex. Default-route write. Plotting. Demo gallery. Port sweep. `net use` / stored share passwords.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.6 | PR01 security locks — **shipped** |
| PR07 | Post-1.0.0 truth pass (1.0.1) — **shipped** |
| PR08 | Fail IDs through 14555 — **shipped** |
| PR09 | Diagnostic jobs + bind — **this amendment. Package 1.1.0.** |
| later | Scheduler package; macOS as a test gate; repo Linux CI |

HTTP reachability stays out of this package.

---

## 14. Mapping

| Windows cousin | Linux cousin | Protocol | API |
|---|---|---|---|
| ping.exe | ping(8) | ICMP Echo | IcmpEcho |
| tracert | traceroute | ICMP / UDP / TCP TTL | IcmpTrace |
| pathping | — | walk + sample | Pathping |
| — | — | TCP handshake | TcpConnect |
| arp -a | ip neigh | ARP / ND | GetNeighbors / ProbeNeighbor |
| nslookup | dig / nslookup | DNS | LookupAsync |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

PR01–PR08 stand. PR09 accepts decisions 36–45 and package **1.1.0**. `1.0.1` does not contain these doors. Network stays `net10.0`. Route write stays Option C. Plot API is never offered.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 | 19 Sep 2026 | PR01 locks. |
| 1.6 + PR08.005 | 24 Sep 2026 | Option C stands. OUI is a URL fetched on request. Events through 14555. |
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Events through 14560. Package 1.1.0. |
