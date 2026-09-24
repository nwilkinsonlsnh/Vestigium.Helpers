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
| 11 | route write | **Print both OS, both families.** Write is Option C (19 Sep 2026): Windows IPv4 IP Helper + HKLM persist; Windows IPv6 `CreateIpForwardEntry2`; Linux IPv4+IPv6 netlink. Missing admin / `CAP_NET_ADMIN` → `NetworkRouteDenied`. Never spawn `route` / `ip` / `netsh`. |
| 11b | IPv6 routes | Print required both OS. Write allowed under 11. Default `::/0` is not offered (§34). |
| 12 | Snapshot | Read-only interface snapshot. Same object both OS. |
| 13 | Windows-only | NetBIOS (nbtstat-class). Not emulated with Samba/`nmblookup`. |
| 14 | Logging | HelperLog audit. APPID Network. |
| 15 | Two JSONL channels | Audit under Vestigium.Logging data dir. Stats under Network campaign dir. |
| 16 | IPv4 and IPv6 | First-class both OS. Includes prefix calculator and Option C route write. |
| 17 | Probe | On-box only. |
| 18 | Linux ICMP | BCL `Ping` / unprivileged ICMP DGRAM when the kernel allows it. No `ping(8)`. |
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
| 29 | Route interface | No guessed IfIndex `1`. Caller may pass `InterfaceIndex >= 1`. Omit or `0` leaves the stack to choose. Never rewrite `0` to `1`. Linux write requires `InterfaceIndex`. Windows IPv4 with no up IPv4 NIC and no caller index → Reject + `ArgumentException`. Windows IPv6 uses the caller index or the first up IPv6 NIC (`GetIPv6Properties().Index`). Never reuse an IPv4 table index on an IPv6 write. |
| 30 | Campaign paths | Recipe and results must resolve under the campaign root after `Path.GetFullPath`. `..` escape → Reject + `ArgumentException`. |
| 31 | HTTP | Never this library except the constrained OUI GET. |
| 32 | Share campaigns | **Shipped (PR03).** `PlanShareProbe` / `CreateShareCampaign` / `OpenShareCampaign`. Probe I/O is FileIo. No password field. No `FileStream` in Network. |
| 33 | Plotting | Never this library. No plot type and no plot method. Results are addresses, RTTs, tables, and prefixes. That is not a deferred feature. |
| 34 | Default route | `0.0.0.0/0` and `::/0` write → `NetworkRouteDenied`. Default route is not offered. |
| 35 | OUI | The IEEE registry is not packed. It changes. Completeness is `LookupOuiAsync`: the caller points at an HTTPS URL and this library fetches it on that request. Allowlist, no redirect, body cap. The embedded snapshot is a tiny stub and is not grown. A host file via `LoadOuiRegistry` is optional and is not a substitute for packing MA-L into this DLL. |
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

Hosts catch `NetworkRouteDenied` for cap / ACL / default-route denies.

---

## 2.1 Platform matrix

| Capability | Windows | Linux | Notes |
|---|---|---|---|
| Adapter inventory | Yes | Yes | `NetworkInterface` |
| Default gateway, DNS servers | Yes | Yes | Linux: adapter + `/etc/resolv.conf` / resolved |
| DHCP enabled | Yes | Best-effort | |
| DHCP server + lease | Yes | Best-effort / null | Never invent timestamps |
| NetBIOS-over-TCP + nbtstat reads | Yes | **No** | |
| ICMP Echo / Trace / Pathping | Yes | Yes | Duration / interval floors §2.4 |
| DNS explicit server | Yes | Yes | UDP/TCP 53. Peer-bound. |
| Connections / statistics | Yes | Yes | Linux PID best-effort |
| Neighbors / ProbeNeighbor | Yes | Yes | |
| Routes print (IPv4 + IPv6) | Yes | Yes | |
| Routes mutate IPv4 | Yes | Yes (netlink) | Cap / admin or `NetworkRouteDenied`. Not `ip`. |
| Routes mutate IPv6 | Yes (`CreateIpForwardEntry2`) | Yes (netlink) | Same deny rules. Default `::/0` refused. |
| Interface snapshot | Yes | Yes | |
| Campaign recipe + JSONL | Yes | Yes | Paths confined to campaign root |
| Share campaigns | Yes | Yes | FileIo probes |
| Prefix describe / plan / VLSM / classify | Yes | Yes | Pure math |
| MAC / EUI / OUI / bandwidth / P95 bill | Yes | Yes | OUI completeness is an HTTPS URL on request. The embedded snapshot is a stub and is not grown. |
| Charts / WPF Demo | **Never** | **Never** | Not a Network surface |

---

## 2.2 Paths

| Use | Windows | Linux |
|---|---|---|
| Campaign default root | `%ProgramData%\\Vestigium\\Network\\Campaigns\\` | `/var/lib/vestigium/network/campaigns/` |
| IPv4 persistent routes | `HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\PersistentRoutes` | n/a |
| Audit logs | Vestigium.Logging host path | Vestigium.Logging host path |
| Tests | `NetworkTestHooks.CampaignRoot` temp | same |

Prefix math writes no files. Live OUI writes no files. The embedded snapshot is not the IEEE registry. A host-supplied OUI file stays capped (8 MiB / 200_000 rows).

---

## 2.3 Privileges

The library never calls `sudo`, never prompts UAC, never `setcap`s itself. Prefix / MAC / bandwidth math need no privilege. Route mutate needs administrator on Windows and `CAP_NET_ADMIN` on Linux. Persistent-route registry writes need administrator on Windows.

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
G5. Route **print** both OS; **write** Option C; default route not offered.  
G6. DNS to a specified server; accept only that peer.  
G7. Json for recipe/stats.  
G8. Sparse HelperLog.  
G9. On-box Probe.  
G10. Linux boxes do not need a Windows VM.  
G11. Prefix calculator for IPv4 and IPv6, plus IPv4 classful labels A–E.  
G12. Fail closed on attacker-controlled strings (OUI URL, campaign path, ICMP flood, DNS spoof, guessed IfIndex).  
G13. Share campaigns without credentials or `FileStream` in this DLL.  
G14. No plot API. Never.

---

## 4. Workstation inventory

Same fields as v1.2. Linux: NetBIOS-over-TCP is `Unknown`.

---

## 5. Protocol jobs

Unchanged from v1.2 except §2.4, §28, §29, decisions 11 / 11b / 32–45, and the PR09 doors: `Pathping`, `TcpConnect`, `SampleCounters`, `PathMtu`, `ProbeNeighbor`.

### 5.1 Prefix calculator

Normative text: subnet addendum v1.3.

Façade: `ClassifyAddress`, `DescribePrefix`, `PlanByHosts`, `PlanByNetworks`, `SplitPrefix`, `SplitPrefixByCount`, `PackVlsm`, `Contains`, `Overlaps`, `Summarize`, `NextBlock`.

### 5.2 MAC / bandwidth

Normative text: MAC/bandwidth addendum v1.4, plus decision 25 for live OUI and 35 for packed OUI.

Façade: `ParseMac`, `MacFromInteger`, `FormatMac`, `ToModifiedEui64`, `ToEui48`, `ToLinkLocal`, `LookupOuiAsync`, `LoadOuiRegistry`, `LookupOuiFile`, `LoadPackedOuiRegistry`, `LookupOuiPacked`, `Bandwidth`, `ConvertBandwidth`, `TransferTime`, `RequiredRate`, `Transferred`, `VolumeFromRate`, `RateFromVolume`, `EstimateWebsite`, `BandwidthSeconds`, `BillP95`, `BillPercentile`.

### 5.3 Share campaigns

Normative text: share addendum v1.5.

Façade: `PlanShareProbe`, `CreateShareCampaign`, `OpenShareCampaign`.

---

## 6. Campaigns

Clock windows + date range + timezone + 15 min grace. Recipe `.json`, results `.jsonl`. Create persists Echo options that Create already accepted (`timeoutMs`, `intervalMs`, `bufferSize`, `ttl`, `dontFragment`, `maxDurationMs`, `allowBurst`). Open reads them. A recipe with no `echo` object keeps current `IcmpEchoOptions` defaults. Window `Count` stays on the window.

`ResultsPath` and `RecipePath` must stay under the campaign root (§2.2, decision 30). Tests inject the temp root only.

Window shape is `EchoWindow(LocalTime, Count, Duration?)`. Recipe writes `durationMs` only when Duration is set. Open of an old recipe leaves duration null and runs the count. Both set: stop at count or duration, whichever first. Neither: reject.

Share-transfer campaigns are shipped (PR03).

---

## 7–8. Surface and defaults

v1.2 surface plus §5.1–5.3 plus PR09 doors. Count default 4. Grace 15 min. `MaxList` default 1024. Continuous default duration 60 s. Long continuous interval floor 1 s.

---

## 9. Logging

APPID Network. Sparse. Campaign echoes live in stats JSONL only. Subnet / MAC / bandwidth / share lines are query + summary. No packet bytes. No credentials. Absolute-path prefixes are stripped from Network log / Reject lines (PR01.009). Named events used through **14560**: RouteDenied 14530, IcmpForbidden 14535, CampaignWindowMissed 14540, CampaignPathEscape 14545, DnsPeerMismatch 14550, OuiLookupRejected 14555, BindRejected 14560. Taxonomy includes Share, Stats, Progress. `BillPercentile(NumericSeries, double)` is on the façade.

---

## 10. Demo

Not a library gate. No `Vestigium.Helpers.Network.Demo` project in this package.

---

## 11. Tests

Named fixtures `PR01_` … `PR05_` plus later tests named for the behavior. No public Internet. No live ProgramData / `/var/lib/vestigium` in tests. OUI tests inject `OuiLookupOptions.Handler` or use the packed snapshot.

Linux CI for the umbrella test project is **repo** work (test TFM is `net10.0-windows`). It is not a Network feature. Windows `FullyQualifiedName~Network` is the Network gate until a later repo workflow lands. Live Ubuntu route checks are parked on [`PR05_ImplementationPlan.md`](PR05_ImplementationPlan.md) §4.

---

## 12. Non-goals

Spawn CLI tools on any OS. Install cron/schtasks/systemd units. HTTP reachability client. NetBIOS on Linux. Full NetworkManager / netplan writers. Packet capture. Classful mask inference when prefix and mask are both omitted. Classless in-addr.arpa fabrication. DHCP scope design. Guessing IfIndex. Following OUI HTTP redirects. Accepting DNS answers from a foreign UDP source. Default-route write. Plotting / a charting surface. Demo gallery. `net use` / stored share passwords. Port sweep.

---

## 13. Roadmap

| Version | Item |
|---|---|
| v1.2 | Windows + Linux first-class |
| v1.3 | Subnet calculator — **shipped** |
| v1.4 | MAC / EUI / OUI / bandwidth / P95 — **shipped** |
| v1.5 | Share-transfer campaigns — **shipped PR03** |
| v1.6 | PR01 security locks — **shipped** |
| PR02 | Contract lock — **shipped** |
| PR04 | Packed OUI + Option C route write — **shipped** |
| PR05 | Hygiene (persist key, docs) — **shipped** |
| PR07 | Post-1.0.0 truth pass (1.0.1) — **shipped** |
| PR08 | Fail IDs through 14555, series percentile, paper — **shipped** |
| PR09 | Bind, Pathping, TCP, counters, PMTU, neighbor probe — **this amendment. Package 1.1.0.** |
| later | Scheduler package; macOS as a test gate; repo Linux CI |

HTTP reachability stays out of this package.

---

## 14. Mapping

| Windows cousin | Linux cousin | Protocol | API |
|---|---|---|---|
| ping.exe | ping(8) | ICMP Echo | IcmpEcho |
| tracert | traceroute | ICMP / UDP / TCP TTL | IcmpTrace |
| pathping | — | walk + sample | Pathping |
| route print | ip route | forwarding table | GetRoutes |
| route add/delete | ip route add/del | forwarding table | AddRoute / DeleteRoute — Option C. Cousin never spawned. Default route not offered. |
| netstat / ss | ss | TCP/UDP tables | GetConnections |
| arp -a | ip neigh | ARP / ND | GetNeighbors / ProbeNeighbor |
| nbtstat | — | NetBIOS | Windows only |
| nslookup | dig / nslookup | DNS | LookupAsync |
| netsh show | ip addr / iw | snapshot | GetSnapshot |
| ipcalc | ipcalc / sipcalc | prefix math | DescribePrefix / Plan* |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

PR01–PR08 stand. PR09 accepts decisions 36–45 and package **1.1.0**. `1.0.1` does not contain these doors. Network stays `net10.0`, one DLL for Windows and Linux. Route write stays Option C.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 10 Sep 2026 | Windows + Linux first-class. |
| 1.3 | 10 Sep 2026 | Subnet calculator. |
| 1.4 | Sep 2026 | MAC / bandwidth addendum. |
| 1.5 | Sep 2026 | Share campaign addendum. |
| 1.6 | 19 Sep 2026 | PR01 locks. |
| 1.6 + PR02.001 | 19 Sep 2026 | Then: Windows mutate, Linux print-only. |
| 1.6 + PR05.002 | 19 Sep 2026 | Share shipped. Option C. Default route refused. Packed OUI. Plotting is not a Network surface. |
| 1.6 + PR07.009 | 24 Sep 2026 | IPv6 IfIndex. Echo recipe persist. Events through 14540. Package 1.0.1. Plot API never offered. |
| 1.6 + PR08.005 | 24 Sep 2026 | Option C stands. OUI is a URL fetched on request. IEEE registry is not packed. Events through 14555. |
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Events through 14560. Package 1.1.0. |
