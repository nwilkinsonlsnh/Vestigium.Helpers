# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.6  
**Status:** Draft → PR02 contract lock in progress. Decision 11 / 11b landed in PR02.001. Linux mutate exception type lands in PR02.002.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md), [`Design_v1.6.md`](Design_v1.6.md)  
**Subnet lock:** [`ARCHIVE/PR01/SubnetCalculator_v1.3.md`](ARCHIVE/PR01/SubnetCalculator_v1.3.md)  
**MAC / bandwidth lock:** [`ARCHIVE/PR01/MacAndBandwidth_v1.4.md`](ARCHIVE/PR01/MacAndBandwidth_v1.4.md)  
**Share lock (not shipped):** [`ARCHIVE/PR01/ShareCampaign_v1.5.md`](ARCHIVE/PR01/ShareCampaign_v1.5.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost on Windows or Linux

If implementation and this file disagree, this file wins **except** where a later PR plan in this folder explicitly amends a lock and lands the amendment here in the same change. For prefix math, the subnet addendum wins over this summary. For MAC / bandwidth numbers, the v1.4 addendum wins over this summary.

This package is a **library of resources**, not a tool. Hosts subscribe. It speaks **protocols** (ICMP Echo, ICMP Time Exceeded, DNS, ARP/ND, TCP/UDP tables, the OS routing table) and **prefix / MAC / bandwidth arithmetic**. It does not spawn `ping`, `ping.exe`, `traceroute`, `tracert.exe`, `ip`, `ss`, `netstat`, `route`, `nbtstat`, `arp`, `netsh`, `nslookup`, `ipcalc`, or `ipv6calc`.

A future `Vestigium.Scheduler` (name not locked) may start jobs. This library does not install cron, schtasks, or systemd units.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Library | `net10.0` / .NET 10 LTS. Tools subscribe. No CLI host in this project. |
| 1b | Platforms | **Windows + Linux required.** Same public API. macOS is best-effort BCL (not a v1 test gate). |
| 2 | Protocols, not shells | ICMP / DNS / ARP-ND / stack tables / routing APIs / prefix math / MAC / bandwidth math. No process spawn on either OS. |
| 3 | Name Ping | Alias of IcmpEcho for PingIQ. JSONL kind is `icmpEcho`. |
| 4 | Default echo count | **4**. Override `>= 1`, or `0` continuous. |
| 5 | Continuous | Count = 0 until Cancel / token / MaxDuration. See §2.4. |
| 6 | Campaign scheduler | Clock windows + date range + append-only JSONL. In-process. Host stays alive. |
| 7 | Who keeps time | In-process runner. No schtasks, no cron install. |
| 8–10 | Json stats | Recipe `.json`, stats `.jsonl` via Vestigium.Helpers.Json. Append one compact line. |
| 11 | route | **Print both OS.** Mutate is **Windows only** in v1 (IP Helper + persistent HKLM). Linux mutate → `NetworkRouteDenied` (“Linux route write is not in v1”). Never spawn `route` / `ip`. |
| 11b | IPv6 routes | **Print required** both OS. IPv6 mutate is not v1 (PR04). |
| 12 | Snapshot | Read-only interface snapshot. Same object both OS. |
| 13 | Windows-only | NetBIOS (nbtstat-class). Not emulated with Samba/`nmblookup`. |
| 14 | Logging | HelperLog audit. APPID Network. |
| 15 | Two JSONL channels | Audit under Vestigium.Logging data dir. Stats under Network campaign dir. |
| 16 | IPv4 and IPv6 | First-class both OS. Includes prefix calculator. IPv6 **route mutate** is out until PR04. |
| 17 | Probe | On-box only. |
| 18 | Linux ICMP | Prefer unprivileged ICMP DGRAM when the kernel allows it. Confirmation of that path is PR02.005. |
| 19 | Linux payload | Custom Echo buffer rejected → empty retry + `PayloadRestricted`. |
| 20 | Subnet calculator | IPv4 **and** IPv6. Shipped. |
| 21 | Classful | A/B/C/D/E is a label from the first IPv4 octet. It never selects the mask. |
| 22 | IPv6 class | `TraditionalClass = None`. Use `Kind` (ULA, GUA, link-local, multicast, docs). |
| 23 | Usable hosts | IPv4 excludes network+broadcast except `/31` (2) and `/32` (1). IPv6 usable = prefix size. |
| 24 | Fit or throw | Parent too small → Reject + throw. No silent shrink. List cap `MaxList` default 1024. |
| 25 | OUI live lookup | HTTPS only. Default host `api.macvendors.com`. Custom host requires `AllowCustomRegistry` **and** `AllowedRegistryHosts`. No auto-redirect. Body cap 4 KiB. Vendor trim 200 chars. Block loopback, link-local, RFC1918, CGNAT, metadata. `ParseMac` does not HTTP. |
| 26 | Test hooks | `NetworkTestHooks` is **internal**. Hosts cannot set `CampaignRoot`, `UtcNow`, or `ProcRoot`. Tests use `InternalsVisibleTo`. |
| 27 | ICMP duration / interval | See §2.4. |
| 28 | DNS wire peer | UDP datagram accepted only when `RemoteEndPoint` is the queried server IP + port. TXID check stays. TCP is a connected socket; peer mismatch is a typed fail. |
| 29 | Route interface | No guessed IfIndex `1`. Caller may pass `InterfaceIndex >= 1`. Windows with no up IPv4 NIC and no caller index → Reject + `ArgumentException`. |
| 30 | Campaign paths | Recipe and results must resolve under the campaign root after `Path.GetFullPath`. `..` escape → Reject + `ArgumentException`. Campaign JSONL does not `CreateDirectory` above that root. One-shot echo `StatsPath` is not this lock. |
| 31 | HTTP | Never this library. HttpIQ. |
| 32 | Share campaigns | Locked in the v1.5 addendum. Not in this DLL until PR03. |

Until PR02.002 ships, Linux mutate still throws `PlatformNotSupportedException`. Hosts should catch `NetworkRouteDenied` after 002; 002 will wrap the Linux deny so there is one type.

---

## 2.1 Platform matrix

| Capability | Windows | Linux | Notes |
|---|---|---|---|
| Adapter inventory | Yes | Yes | `NetworkInterface` |
| Default gateway, DNS servers | Yes | Yes | Linux: adapter + `/etc/resolv.conf` / resolved |
| DHCP enabled | Yes | Best-effort | |
| DHCP server + lease | Yes | Best-effort / null | Never invent timestamps |
| NetBIOS-over-TCP + nbtstat reads | Yes | **No** | |
| ICMP Echo / Trace | Yes | Yes | Duration / interval floors §2.4 |
| DNS explicit server | Yes | Yes | UDP/TCP 53. Peer-bound. |
| Connections / statistics | Yes | Yes | Linux PID best-effort |
| Neighbors | Yes | Yes | |
| Routes print (IPv4 + IPv6) | Yes | Yes | |
| Routes mutate IPv4 | Yes | **No** — `NetworkRouteDenied` | Not `ip`. Not netlink in v1. |
| Routes mutate IPv6 | **No** | **No** | PR04 |
| Interface snapshot | Yes | Yes | |
| Campaign recipe + JSONL | Yes | Yes | Paths confined to campaign root |
| Prefix describe / plan / VLSM / classify | Yes | Yes | Pure math |
| MAC / EUI / OUI / bandwidth / P95 bill | Yes | Yes | Live OUI is HTTPS + allowlist |
| WPF Demo gallery | Yes | **No** | Out of this package’s PR01–PR04 docs work |

---

## 2.2 Paths

| Use | Windows | Linux |
|---|---|---|
| Campaign default root | `%ProgramData%\Vestigium\Network\Campaigns\` | `/var/lib/vestigium/network/campaigns/` |
| Audit logs | Vestigium.Logging host path | Vestigium.Logging host path |
| Tests | `NetworkTestHooks.CampaignRoot` temp | same |

Prefix math writes no files. Live OUI writes no files. File OUI is host-supplied path (8 MiB / 200_000 row cap).

---

## 2.3 Privileges

The library never calls `sudo`, never prompts UAC, never `setcap`s itself. Prefix / MAC / bandwidth math need no privilege. Route mutate and persistent-route registry writes need administrator on Windows.

---

## 2.4 ICMP duration and interval (PR01.003)

| Job | Duration | Interval | `AllowBurst` |
|---|---|---|---|
| `Count >= 1` | Ends on count | `0` allowed | Not required |
| `Count == 0`, `MaxDuration` omitted | Becomes **60 s** | Default 1 s is fine | — |
| `Count == 0`, duration **≤ 1 min** | Required or defaulted | **≥ 200 ms** | May go faster; duration still ≤ 1 min |
| `Count == 0`, duration **> 1 min** up to **24 h** | Required | **≥ 1 s** | Rejected |
| `MaxDuration <= 0` or `> 24 h` | Reject | — | — |

Named spans (`1 minute`, `1 hour`) are `TimeSpan` values on `MaxDuration`. No extra duration type.

---

## 3. Goals

G1. Same façade on Windows and Linux.  
G2. ICMP Echo, not ping(8) / ping.exe.  
G3. Default 4 echoes; override; continuous under §2.4.  
G4. Campaign windows + JSONL on both OS, confined to campaign root.  
G5. Route **print** both OS; **mutate Windows only** in v1.  
G6. DNS to a specified server; accept only that peer.  
G7. Json for recipe/stats.  
G8. Sparse HelperLog.  
G9. On-box Probe.  
G10. Linux boxes do not need a Windows VM.  
G11. Prefix calculator for IPv4 and IPv6, plus IPv4 classful labels A–E.  
G12. Fail closed on attacker-controlled strings (OUI URL, campaign path, ICMP flood, DNS spoof, guessed IfIndex).

---

## 4. Workstation inventory

Same fields as v1.2. Linux: NetBIOS-over-TCP is `Unknown`.

---

## 5. Protocol jobs

Unchanged from v1.2 except §2.4, §28, §29, and decisions 11 / 11b.

### 5.1 Prefix calculator

Normative text: subnet addendum v1.3.

Façade: `ClassifyAddress`, `DescribePrefix`, `PlanByHosts`, `PlanByNetworks`, `SplitPrefix`, `SplitPrefixByCount`, `PackVlsm`, `Contains`, `Overlaps`, `Summarize`, `NextBlock`.

### 5.2 MAC / bandwidth

Normative text: MAC/bandwidth addendum v1.4, plus decision 25 for live OUI.

Façade: `ParseMac`, `MacFromInteger`, `FormatMac`, `ToModifiedEui64`, `ToEui48`, `ToLinkLocal`, `LookupOuiAsync`, `LoadOuiRegistry`, `LookupOuiFile`, `Bandwidth`, `ConvertBandwidth`, `TransferTime`, `RequiredRate`, `Transferred`, `VolumeFromRate`, `RateFromVolume`, `EstimateWebsite`, `BandwidthSeconds`, `BillP95`, `BillPercentile`.

---

## 6. Campaigns

Clock windows + date range + timezone + 15 min grace. Recipe `.json`, results `.jsonl`.

`ResultsPath` and `RecipePath` must stay under the campaign root (§2.2, decision 30). Tests inject the temp root only.

Window shape today is `EchoWindow(LocalTime, Count)`. Duration-per-window is a later addendum, not PR01.

Share-transfer campaigns are PR03.

---

## 7–8. Surface and defaults

v1.2 surface plus §5.1 and §5.2. Count default 4. Grace 15 min. `MaxList` default 1024. Continuous default duration 60 s. Long continuous interval floor 1 s.

---

## 9. Logging

APPID Network. Sparse. Campaign echoes live in stats JSONL only. Subnet / MAC / bandwidth lines are query + summary. No packet bytes. Absolute-path prefixes are stripped from Network log / Reject lines (PR01.009).

---

## 10. Demo

WPF gallery remains Windows. Subnet / MAC / Bandwidth / Share tabs are PR03. This library’s contract does not depend on a demo project.

---

## 11. Tests

Portable suite plus PR01 fixtures `PR01_001` … `PR01_011` and PR02 fixtures as they land.

No public Internet. No live ProgramData / `/var/lib/vestigium` in tests. OUI tests inject `OuiLookupOptions.Handler`.

Linux CI for `FullyQualifiedName~Network` remains waived until PR04 splits the test TFM. Windows-latest is the gate.

---

## 12. Non-goals

Spawn CLI tools on any OS. Install cron/schtasks/systemd units. HTTP client. NetBIOS on Linux. Full NetworkManager / netplan writers. Packet capture. Classful mask inference when prefix and mask are both omitted. Classless in-addr.arpa fabrication. DHCP scope design. Guessing IfIndex. Following OUI HTTP redirects. Accepting DNS answers from a foreign UDP source. Linux `ip route` / netlink write in v1. IPv6 route write in v1.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.2 | Windows + Linux first-class |
| v1.3 | Subnet calculator IPv4 + IPv6 + classful labels — **shipped** |
| v1.4 | MAC / EUI / OUI / bandwidth / P95 — **shipped** |
| v1.5 | Share-transfer campaigns — **locked, PR03** |
| v1.6 | PR01 security locks — **shipped** |
| PR02.001 | Route contract: Windows write / Linux print — **this amendment** |
| PR02 remainder | Deny type, DNS length, JSONL lock, ICMP note, P95 empty |
| PR04 | Linux test TFM; optional IPv6 / Linux route write |
| later | Scheduler package; macOS as a test gate; pathping-class |

HTTP reachability stays out of this package.

---

## 14. Mapping

| Windows cousin | Linux cousin | Protocol | API |
|---|---|---|---|
| ping.exe | ping(8) | ICMP Echo | IcmpEcho |
| tracert | traceroute | ICMP / UDP TTL | IcmpTrace |
| route print | ip route | forwarding table | GetRoutes |
| route add/delete | ip route add/del | forwarding table | AddRoute / DeleteRoute — **Windows write only.** Linux → `NetworkRouteDenied`. Cousin never spawned. |
| netstat / ss | ss | TCP/UDP tables | GetConnections |
| arp -a | ip neigh | ARP / ND | GetNeighbors |
| nbtstat | — | NetBIOS | Windows only |
| nslookup | dig / nslookup | DNS | LookupAsync |
| netsh show | ip addr / iw | snapshot | GetSnapshot |
| ipcalc | ipcalc / sipcalc | prefix math | DescribePrefix / Plan* |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

PR01 is accepted. PR02.001 accepts decisions 11 and 11b. Status stays Draft until PR02 close. Network stays `net10.0`, one DLL for Windows and Linux.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 10 Sep 2026 | Windows + Linux first-class. |
| 1.3 | 10 Sep 2026 | Subnet calculator. |
| 1.4 | Sep 2026 | MAC / bandwidth addendum (separate file). |
| 1.5 | Sep 2026 | Share campaign addendum (separate file, not shipped). |
| 1.6 | 19 Sep 2026 | PR01.001–006 locks. Live docs restored next to the PR plans. |
| 1.6 + PR02.001 | 19 Sep 2026 | Decision 11 / 11b: Windows mutate, Linux print-only, IPv6 write out of v1. |
