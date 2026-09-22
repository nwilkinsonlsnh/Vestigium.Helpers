# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.3  
**Status:** Draft + locked addendum. v1.2 platform locks stand. v1.3 adds the subnet calculator.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Subnet lock:** [`SubnetCalculator_v1.3.md`](SubnetCalculator_v1.3.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost on Windows or Linux

If implementation and this file disagree, this file wins. For prefix math, the subnet addendum wins over this summary.

This package is a **library of resources**, not a tool. Hosts subscribe. It speaks **protocols** (ICMP Echo, ICMP Time Exceeded, DNS, ARP/ND, TCP/UDP tables, the OS routing table) and **prefix arithmetic**. It does not spawn `ping`, `ping.exe`, `traceroute`, `tracert.exe`, `ip`, `ss`, `netstat`, `route`, `nbtstat`, `arp`, `netsh`, `nslookup`, `ipcalc`, or `ipv6calc`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Library | `net10.0` / .NET 10 LTS. Tools subscribe. No CLI host in this project. |
| 1b | Platforms | **Windows + Linux required.** Same public API. macOS is best-effort BCL (not a v1 test gate). |
| 2 | Protocols, not shells | ICMP / DNS / ARP-ND / stack tables / routing APIs / prefix math. No process spawn on either OS. |
| 3 | Name Ping | Alias of IcmpEcho for PingIQ. JSONL kind is `icmpEcho`. |
| 4 | Default echo count | **4**. Override `>= 1`, or `0` continuous. |
| 5 | Continuous | Count = 0 until Cancel / token / MaxDuration. |
| 6 | Campaign scheduler | Clock windows + date range + append-only JSONL. |
| 7 | Who keeps time | In-process runner. Host stays alive. No schtasks, no cron install. |
| 8–10 | Json stats | Recipe `.json`, stats `.jsonl` via Vestigium.Helpers.Json. Append one compact line. |
| 11 | route | Print required both OS. Add/change/delete explicit; typed fail without CAP_NET_ADMIN / admin. |
| 12 | Snapshot | Read-only interface snapshot. Windows netsh-class; Linux ip/iw-class. Same object. |
| 13 | Windows-only | NetBIOS (nbtstat-class). Not emulated with Samba/`nmblookup`. |
| 14 | Logging | HelperLog audit. APPID Network. |
| 15 | Two JSONL channels | Audit under Vestigium.Logging data dir. Stats under Network campaign dir. |
| 16 | IPv4 and IPv6 | First-class both OS. Includes prefix calculator. |
| 17 | Probe | On-box only. |
| 18 | Linux ICMP | Prefer unprivileged ICMP DGRAM when the kernel allows it. |
| 19 | Linux payload | Custom Echo buffer rejected → empty retry + `PayloadRestricted`. |
| 20 | Subnet calculator | IPv4 **and** IPv6 in the same Phase 9 slice. |
| 21 | Classful | A/B/C/D/E is a label from the first IPv4 octet. It never selects the mask. |
| 22 | IPv6 class | `TraditionalClass = None`. Use `Kind` (ULA, GUA, link-local, multicast, docs). |
| 23 | Usable hosts | IPv4 excludes network+broadcast except `/31` (2) and `/32` (1). IPv6 usable = prefix size. |
| 24 | Fit or throw | Parent too small → Reject + throw. No silent shrink. List cap `MaxList` default 1024. |

---

## 2.1 Platform matrix

| Capability | Windows | Linux | Notes |
|---|---|---|---|
| Adapter inventory | Yes | Yes | `NetworkInterface` |
| Default gateway, DNS servers | Yes | Yes | Linux: adapter + `/etc/resolv.conf` / resolved |
| DHCP enabled | Yes | Best-effort | |
| DHCP server + lease | Yes | Best-effort / null | Never invent timestamps |
| NetBIOS-over-TCP + nbtstat reads | Yes | **No** | |
| ICMP Echo / Trace | Yes | Yes | |
| DNS explicit server | Yes | Yes | UDP/TCP 53 |
| Connections / statistics | Yes | Yes | Linux PID best-effort |
| Neighbors | Yes | Yes | |
| Routes print / mutate | Yes | Yes | Mutate typed deny |
| Interface snapshot | Yes | Yes | |
| Campaign scheduler + JSONL | Yes | Yes | |
| **Prefix describe / plan / VLSM / classify** | Yes | Yes | Pure math |
| WPF Demo gallery | Yes | **No** | |

---

## 2.2 Paths

| Use | Windows | Linux |
|---|---|---|
| Campaign default root | `%ProgramData%\Vestigium\Network\Campaigns\` | `/var/lib/vestigium/network/campaigns/` |
| Audit logs | Vestigium.Logging host path | Vestigium.Logging host path |
| Tests | Injected temp root | Injected temp root |

Prefix math writes no files.

---

## 2.3 Privileges

The library never calls `sudo`, never prompts UAC, never `setcap`s itself. Prefix math needs no privilege.

---

## 3. Goals

G1. Same façade on Windows and Linux.  
G2. ICMP Echo, not ping(8) / ping.exe.  
G3. Default 4 echoes; override; continuous.  
G4. Campaign windows + JSONL on both OS.  
G5. Route print both OS; mutations explicit.  
G6. DNS to a specified server.  
G7. Json for recipe/stats.  
G8. Sparse HelperLog.  
G9. On-box Probe.  
G10. Linux boxes do not need a Windows VM.  
G11. Prefix calculator for IPv4 and IPv6, plus IPv4 classful labels A–E.

---

## 4. Workstation inventory

Same fields as v1.2. Linux: NetBIOS-over-TCP is `Unknown`.

---

## 5. Protocol jobs

Unchanged from v1.2.

---

## 5.1 Prefix calculator

Normative text: [`SubnetCalculator_v1.3.md`](SubnetCalculator_v1.3.md).

Façade: `ClassifyAddress`, `DescribePrefix`, `PlanByHosts`, `PlanByNetworks`, `SplitPrefix`, `SplitPrefixByCount`, `PackVlsm`, `Contains`, `Overlaps`, `Summarize`, `NextBlock`.

---

## 6. Campaigns

Unchanged from v1.2.

---

## 7–8. Surface and defaults

v1.2 surface plus §5.1. Count default 4. Grace 15 min. `MaxList` default 1024.

---

## 9. Logging

APPID Network. Sparse. Campaign echoes live in stats JSONL only. Subnet lines are query + summary.

---

## 10. Demo

WPF gallery remains Windows. Add a Subnet tab when Phase 9 ships.

---

## 11. Tests

v1.2 portable suite plus the close gates in the subnet addendum §6.

No public Internet. No live ProgramData / `/var/lib/vestigium` in tests.

---

## 12. Non-goals

Spawn CLI tools on any OS. Install cron/schtasks/systemd units. HTTP client. NetBIOS on Linux. Full NetworkManager / netplan writers. Packet capture. Classful mask inference when prefix and mask are both omitted. Classless in-addr.arpa fabrication. DHCP scope design.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.2 | Windows + Linux first-class |
| v1.3 | Subnet calculator IPv4 + IPv6 + classful labels |
| v1.4 | Json AppendJsonl; richer Linux DHCP |
| v1.5 | HTTP reachability |
| later | macOS as a test gate; pathping-class |

---

## 14. Mapping

| Windows cousin | Linux cousin | Protocol | API |
|---|---|---|---|
| ping.exe | ping(8) | ICMP Echo | IcmpEcho |
| tracert | traceroute | ICMP / UDP TTL | IcmpTrace |
| route print | ip route | forwarding table | GetRoutes |
| route add/delete | ip route add/del | forwarding table | AddRoute / DeleteRoute |
| netstat / ss | ss | TCP/UDP tables | GetConnections |
| arp -a | ip neigh | ARP / ND | GetNeighbors |
| nbtstat | — | NetBIOS | Windows only |
| nslookup | dig / nslookup | DNS | LookupAsync |
| netsh show | ip addr / iw | snapshot | GetSnapshot |
| ipcalc | ipcalc / sipcalc | prefix math | DescribePrefix / Plan* |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

Draft until Accepted. Network stays `net10.0`, one DLL for Windows and Linux. Phase 9 closes the subnet addendum gates.
