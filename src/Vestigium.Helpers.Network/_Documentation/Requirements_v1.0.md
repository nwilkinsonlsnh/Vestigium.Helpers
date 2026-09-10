# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.2  
**Status:** Draft. Supersedes v1.1. Awaiting acceptance before the public API grows past Identity + Probe.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost on Windows or Linux

If implementation and this file disagree, this file wins.

This package is a **library of resources**, not a tool. Hosts subscribe. It speaks **protocols** (ICMP Echo, ICMP Time Exceeded, DNS, ARP/ND, TCP/UDP tables, the OS routing table). It does not spawn `ping`, `ping.exe`, `traceroute`, `tracert.exe`, `ip`, `ss`, `netstat`, `route`, `nbtstat`, `arp`, `netsh`, or `nslookup`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Library | `net10.0` / .NET 10 LTS. Tools subscribe. No CLI host in this project. |
| 1b | Platforms | **Windows + Linux required.** Same public API. macOS is best-effort BCL (not a v1 test gate). |
| 2 | Protocols, not shells | ICMP / DNS / ARP-ND / stack tables / routing APIs. No process spawn on either OS. |
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
| 16 | IPv4 and IPv6 | First-class both OS. |
| 17 | Probe | On-box only. |
| 18 | Linux ICMP | Prefer unprivileged ICMP DGRAM (`IPPROTO_ICMP`) when the kernel allows it (`ping_group_range`). Do not require root for a default 4-echo loopback job. |
| 19 | Linux payload | If a custom Echo buffer is rejected (unprivileged + custom payload), retry once with empty payload, set `PayloadRestricted = true`, log Warning once. Do not crash the campaign. |

---

## 2.1 Platform matrix

| Capability | Windows | Linux | Notes |
|---|---|---|---|
| Adapter inventory (name, MAC, IPv4/IPv6, CIDR, mask, status, speed) | Yes | Yes | `NetworkInterface` |
| Default gateway, DNS servers | Yes | Yes | Linux: adapter + `/etc/resolv.conf` / resolved |
| DHCP enabled | Yes | Best-effort | Linux null if NetworkManager/systemd-networkd lease is not readable |
| DHCP server + lease obtained/expires | Yes | Best-effort / null | Never invent timestamps |
| NetBIOS-over-TCP flag + nbtstat reads | Yes | **No** | `PlatformNotSupportedException` after HelperGuard |
| ICMP Echo | Yes | Yes | Linux: ICMP DGRAM first; raw + `CAP_NET_RAW` if needed |
| ICMP Trace | Yes | Yes | Same; UDP TTL fallback if ICMP send forbidden; record `ProbeProtocol` |
| DNS explicit server | Yes | Yes | UDP/TCP 53 |
| Connections / statistics (ss/netstat-class) | Yes | Yes | Linux PID from `/proc` when readable |
| Neighbors (arp / ip neigh) | Yes | Yes | `/proc/net/arp` + ND cache |
| Routes print (ip route / route print) | Yes | Yes | |
| Routes add/change/delete | Yes (admin) | Yes (`CAP_NET_ADMIN`) | Typed access-denied |
| Interface snapshot | netsh-class | `ip`/`iw`-class | One `InterfaceSnapshot` type |
| WLAN SSID / signal | Yes | Yes when nl80211 readable | Names only, no secrets |
| Campaign scheduler + JSONL | Yes | Yes | |
| WPF Demo gallery | Yes | **No** | Demo stays `net10.0-windows`. Library itself is not WPF. |

Linux behavior references (documentation only, never spawned): `ping`, `traceroute`, `ip addr`, `ip route`, `ip neigh`, `ss`, `resolvectl`.

---

## 2.2 Paths

| Use | Windows | Linux |
|---|---|---|
| Campaign default root | `%ProgramData%\Vestigium\Network\Campaigns\` | `/var/lib/vestigium/network/campaigns/` |
| Audit logs | Vestigium.Logging host path (`%ProgramData%\Vestigium\Logs\Network\`) | Vestigium.Logging host path (typically `/var/log/vestigium/Network/` or whatever the host passed to `Initialize`) |
| Tests | Injected temp root | Injected temp root |

If `/var/lib/vestigium/...` is not writable, HelperGuard Failed then throw — do not silently write `$HOME`. Hosts that run as a user pass `ResultsPath` explicitly.

---

## 2.3 Privileges

The library never calls `sudo`, never prompts UAC, never `setcap`s itself.

| Action | If denied |
|---|---|
| ICMP Echo default (loopback / empty-or-default payload) | Typed `TimedOut` / `ProtocolForbidden`. Job does not throw. |
| ICMP custom payload on unprivileged Linux | Retry empty payload + `PayloadRestricted`. |
| GetConnections process name | `ProcessName` null |
| GetRoutes | Still return what the OS allows (often full read) |
| AddRoute / DeleteRoute | Failed + `UnauthorizedAccessException` after HelperGuard |
| NetBIOS on Linux | `PlatformNotSupportedException` |

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
G10. Linux boxes do not need a Windows VM to run PingIQ / TraceIQ / DnsIQ against this library.

---

## 4. Workstation inventory

Same fields as v1.1. Linux: NetBIOS-over-TCP is `Unknown`. DHCP lease fields null when the lease file is not readable. Do not parse `dhclient.leases` as a hard requirement in v1; if a readable lease source exists, fill it.

---

## 5. Protocol jobs

Unchanged from v1.1 except:

- ICMP implementation on Linux uses unprivileged ICMP sockets when `net.ipv4.ping_group_range` includes the host GID.
- Trace records `ProbeProtocol = Icmp | Udp`.
- `GetRoutes` / neighbors / connections have Linux readers (BCL first, then `/proc` / netlink). Still no `ip`/`ss` spawn.
- NetBIOS methods: Windows only.

---

## 6. Campaigns

Same clock rules, line kinds, and Json append rules as v1.1. Default `ResultsPath` directory follows §2.2. Time zone default remains `TimeZoneInfo.Local` (IANA on Linux, Windows zone on Windows).

No crontab write, no systemd unit install from this library. ProbeHost-on-Linux is the long-running host.

---

## 7–8. Surface and defaults

Same public surface as v1.1. Count default 4. Grace 15 min.

---

## 9. Logging

APPID Network. Sparse. Campaign echoes live in stats JSONL only.

---

## 10. Demo

WPF gallery remains Windows. Library tests and ProbeHost-on-Linux exercise the same DLL.

---

## 11. Tests

Default suite must pass on **Windows and Linux** agents for: Identity, Probe, GetWorkstation (at least loopback), IcmpEcho 127.0.0.1 default count 4 (or typed protocol-forbidden, not crash), Count override, Count=0 cancel, campaign JSONL append + OpenJsonl, GetRoutes list, GetConnections list, NetBIOS throws on Linux and runs on Windows.

No public Internet. No live ProgramData / `/var/lib/vestigium` in tests (inject temp root).

---

## 12. Non-goals

Spawn CLI tools on any OS. Install cron/schtasks/systemd units. HTTP client. NetBIOS on Linux. Full NetworkManager / netplan writers. Packet capture.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.2 | This document: Windows + Linux first-class |
| v1.3 | Json AppendJsonl; richer Linux DHCP from NetworkManager when present |
| v1.4 | HTTP reachability |
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
| netsh show | ip addr / iw | snapshot | GetInterfaceSnapshot |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

Draft until Accepted. Then Network stays `net10.0`, references Json, ships one DLL for Windows and Linux, and CI covers both OS for the portable tests in §11.
