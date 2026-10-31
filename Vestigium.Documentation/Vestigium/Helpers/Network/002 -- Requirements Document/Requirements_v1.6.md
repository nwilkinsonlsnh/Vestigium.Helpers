# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.6  
**Status:** Locked companion to PR01–PR14.  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` **2.1.0**  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md), [`Design_v1.6.md`](Design_v1.6.md)

If implementation and this file disagree, this file wins **except** where a later PR plan in this folder explicitly amends a lock and lands the amendment here in the same change.

This package is a **library of resources**, not a tool. Hosts subscribe. It does not spawn `ping`, `tracert`, `pathping`, `ip`, `route`, `arp`, or `nslookup`.

This library does not plot and will not grow a plot API.

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
| 11 | route write | **Print both OS, both families.** Write is Option C. Missing admin / `CAP_NET_ADMIN` → `NetworkRouteDenied`. Never spawn `route` / `ip` / `netsh`. |
| 11b | IPv6 routes | Print required both OS. Write allowed under 11. Default `::/0` is not offered (§34). |
| 12 | Snapshot | Read-only interface snapshot. Same object both OS. |
| 13 | Windows-only | NetBIOS. Not emulated with Samba/`nmblookup`. |
| 14 | Logging | HelperLog audit. APPID Network. |
| 15 | Two JSONL channels | Audit under Vestigium.Logging data dir. Stats under Network campaign dir. |
| 16 | IPv4 and IPv6 | First-class both OS. |
| 17 | Probe | On-box only. |
| 18 | Linux ICMP | BCL `Ping` / unprivileged ICMP DGRAM when the kernel allows it. No `ping(8)`. |
| 19 | Linux payload | Custom Echo buffer rejected → empty retry + `PayloadRestricted`. |
| 20 | Subnet calculator | IPv4 **and** IPv6. Shipped. |
| 21 | Classful | A/B/C/D/E is a label from the first IPv4 octet. It never selects the mask. |
| 22 | IPv6 class | `TraditionalClass = None`. |
| 23 | Usable hosts | IPv4 excludes network+broadcast except `/31` (2) and `/32` (1). IPv6 usable = prefix size. |
| 24 | Fit or throw | Parent too small → Reject + throw. List cap `MaxList` default 1024. |
| 25 | OUI live lookup | HTTPS only. Allowlist. No auto-redirect. Body cap 4 KiB. |
| 26 | Test hooks | `NetworkTestHooks` is **internal**. |
| 27 | ICMP duration / interval | See §2.4. |
| 28 | DNS wire peer | UDP accepted only from the queried server IP + port. |
| 29 | Route interface | No guessed IfIndex `1`. Omit or `0` leaves the stack to choose. Never rewrite `0` to `1`. |
| 30 | Campaign paths | Recipe and results must resolve under the campaign root. |
| 31 | HTTP | Never this library except the constrained OUI GET, the short `Confirm` probe, and `ProbeTls` (certificate only, no HTTP). |
| 32 | Share campaigns | Shipped. No password field. |
| 33 | Plotting | Never this library. |
| 34 | Default route | `0.0.0.0/0` and `::/0` write → `NetworkRouteDenied`. |
| 35 | OUI | IEEE registry is not packed. Completeness is `LookupOuiAsync`. |
| 36 | Egress bind | Optional `InterfaceIndex` and `SourceAddress` on echo, trace, DNS, pathping, TcpConnect, UdpProbe, and PMTU. |
| 37 | Trace family | Optional `RouteFamily` on trace and pathping. |
| 38 | Pathping | Walk then sample. Link loss is never a negative gain. |
| 39 | PTR | Optional name on each hop. Miss stays empty. |
| 40 | TCP probe | Third trace probe. Refused connect names the hop. |
| 41 | TcpConnect | One host, one port. No port loop on this door. `ScanPorts` is the only list or range job (decision 75). |
| 42 | Counter sample | One adapter, one duration. Does not bill. Does not plot. |
| 43 | Window duration | `EchoWindow` may carry `Duration`. First limit wins. |
| 44 | PMTU | Largest don't-fragment echo that passed. Timeout is unknown, not smaller. |
| 45 | Neighbor probe | `ProbeNeighbor(address)` is one ARP/ND ask. `GetNeighbors` stays the table. |
| 46 | Bound ICMP | Bind set → bound ICMP path. Bind omitted → BCL `Ping` may stay. Local endpoint must match the pin. |
| 47 | PMTU status | Too-big shrinks. Timeout does not lower the ceiling. |
| 48 | Neighbor resolve | Typed IP Helper row / one ARP line. Not a table filter as the primary path. |
| 49 | Pathping sample protocol | Phase 2 uses the walk protocol and the same bind. |
| 50 | Recipe bind | Recipe writes bind when set. Old recipes open unset. |
| 51 | UdpProbe | One host, one port. Replied, unreachable, or timed out. |
| 52 | ProbeDns | One name, optional server. Answered, refused, or timed out. Not HTTP. |
| 53 | WatchAdapter | One adapter, one duration. Oper-status and speed. No bill. No plot. |
| 54 | Inference only | Results are `Inferred*`. Never a definitive OS or device claim. Always return the evidence list. |
| 55 | Device class enum | `InferredDeviceClass`: `Unknown`, `WindowsHost`, `UnixLikeHost`, `NetworkDevice`, `MobileOrEmbedded`, `PrinterOrAppliance`. |
| 56 | Vendor enum | `InferredVendor`: `Unknown`, `Cisco`, `Juniper`, `Arista`, `PaloAlto`, `Fortinet`, `Other`. |
| 57 | Scoring is pure | A function that consumes evidence and returns class, vendor, confidence, and the evidence used. No I/O inside the scorer. |
| 58 | No CIDR sweep | `ScanPorts` is the only sweep. It does not expand a CIDR. The calculator stays a separate door. |
| 59 | Banner read | Optional short read after a successful TCP connect. Cap the length. Cap the time. Do not speak a protocol conversation. Off by default. The library never logs the text (decision 95). |
| 60 | TCP MSS | On a successful `TcpConnect`, capture MSS when the stack exposes it. Miss stays null. Status unchanged. |
| 61 | TTL summary | Thin helper over existing ICMP replies: min/max/mode observed TTL and an **estimated** hop count. Does not probe. Does not call `IcmpTrace` / `Trace`. |
| 62 | SNMP sysDescr | Optional one-shot UDP Get of `1.3.6.1.2.1.1.1.0`. Community is a caller parameter and is never logged. No walk. No set. No other OIDs. |
| 63 | OUI reuse | Scoring may consume an existing OUI lookup result when the caller already has a MAC. This path does not call `ProbeNeighbor` or `GetNeighbors` and does not obtain a MAC. |
| 64 | Local adapter MTU | `NetworkAdapter.Mtu` is the local interface MTU. Remote frame size is `PathMtu`. |
| 65 | Sweep is a job | `NetworkHelper.ScanPorts` takes hosts and a `PortSet` and returns `NetworkJob<PortScanResult>`. Progress is `IProgress<NetworkProgress>`. |
| 66 | Hosts are supplied | One host, or a small explicit list. No CIDR. No subnet expansion inside the job. |
| 67 | Ports are supplied | Explicit list, begin–end range, or a named well-known set. Cap is 1024. Over the cap throws. |
| 68 | Concurrency | Caller sets a parallelism cap. Default is 16. Max is 64. |
| 69 | Rate and timing | Per-host delay and an overall timeout. Defaults are polite. |
| 70 | Host discovery | Optional. ICMP, or a TCP probe when `DiscoverPort` is set. Off by default. Misses are skipped and recorded. |
| 71 | Rollup | One result: per host, open TCP, open UDP, catalog name, MSS, banner, optional inference, optional MAC. |
| 72 | Service confirmation | Optional short HTTP probe on known clear-text ports. Off by default. No TLS conversation. |
| 73 | UDP payloads | Optional known payload for DNS (53) and NTP (123). Empty remains the default. One datagram. |
| 74 | SYN scan | Out. No raw sockets. No half-open. Connect scan only. Half-open lives in `Vestigium.Helpers.Network.RawSocket`. |
| 75 | Decision 41 amended | Single-port `TcpConnect` / `UdpProbe` stay. `ScanPorts` is the only place a list or range is allowed. |
| 76 | Progress has a total | `NetworkProgress.Sent` is the expected probe count after discovery. `Received` is finished. |
| 77 | Open-only hits | `PortScanOptions.OpenOnly` drops closed, timed-out, and error hits. Default false. |
| 78 | Banner on the scan | `PortScanOptions.ReadBanner` passes through to `TcpConnect`. Off by default. `Confirm` stays a separate flag. |
| 79 | Lab port sets | `WellKnown` gains `windows`, `printers`, and `databases`. Cap 1024. |
| 80 | Host list is a separate door | `NetworkHelper.HostsFromPrefix` returns a capped host list. IPv4 usable hosts. IPv6 hosts. Cap 1024. It does not scan. `ScanPorts` still does not expand a CIDR. |
| 81 | Rollup text | `PortScanCsv.Write` turns a `PortScanResult` into CSV text. No file write. |
| 82 | SYN still out | Decision 74 stands. |
| 83 | Decision 41 stands | `TcpConnect` and `UdpProbe` stay one host, one port. |
| 84 | Flat open row | `PortScanOpenRow` is host, port, transport, service, MSS, banner, elapsed. |
| 85 | Rows is the door | `PortScanCsv.Rows` returns `IReadOnlyList<PortScanOpenRow>`. Pure. No I/O. |
| 86 | CSV stays | `PortScanCsv.Write` writes the same rows. Banner stays quoted. No file write. |
| 87 | Nested rows stay | `PortScanHostRow` answers what is open on this box. Optional `Mac` when `ResolveMac` is set. |
| 88 | Open ports only | Rows come from open TCP and open UDP. Closed and timed-out are not in the list. |
| 89 | Stamp 2.1.0 | README, Guide, and Design say **2.1.0**. |
| 90 | Platform attributes | Windows-only Registry and macOS-unsupported calls are marked. |
| 91 | Opt-in MAC | `PortScanOptions.ResolveMac` asks `ProbeNeighbor` per live host. Default false. Miss stays null. Inference still does not obtain a MAC. |
| 92 | IPv6 host list | Decision 80 is both families. Same cap. Over the cap throws. No scan. |
| 93 | TLS certificate probe | `ProbeTls` returns subject, issuer, expiry, and protocol. One handshake. No HTTP. Not on the scan by default. |
| 94 | JSON of a scan | `PortScanJson.Write` returns text. Pure. No file. Banner is a field, not a log line. |
| 95 | Banner is not logged | The library never writes a banner to the audit log. The result still carries it. The host may show or save it. |

---

## 2.4 ICMP duration and interval

| Job | Duration | Interval | `AllowBurst` |
|---|---|---|---|
| `Count >= 1` | Ends on count | `0` allowed | Not required |
| `Count == 0`, duration ≤ 1 min | Required or default 60 s | ≥ 200 ms | May go faster |
| `Count == 0`, duration > 1 min up to 24 h | Required | ≥ 1 s | Rejected |

---

## 12. Non-goals

Spawn CLI tools. Install cron/schtasks/systemd. HTTP reachability client beyond the short confirm probe. Packet capture. Default-route write. Plotting. CIDR expansion inside the scanner. SYN or half-open scan in this package. Actual hop walk from the inference path. Neighbor or MAC resolution from the inference path. SNMP walk or set. OS fingerprinting. Hard OS or vendor detection.

---

## 13. Roadmap

| Version | Item |
|---|---|
| PR09 | Package 1.1.0 — **shipped** |
| PR10 / PR10b | Package 1.2.0 — **shipped** |
| PR11 | Package 1.7.0 — **shipped** |
| PR12 | Package 2.0.0 — **shipped**. Sweep, rollup, discovery, confirm, known UDP. |
| PR13 | Lab doors on 2.0.0. Progress total, `OpenOnly`, `ReadBanner`, lab sets, `HostsFromPrefix`, CSV. |
| PR13a | Flat open row. `Rows` is the grid door. `Write` uses those rows. Package 2.1.0. |
| PR14 | Stamp 2.1.0. Platform attributes. Opt-in MAC. IPv6 host list. `ProbeTls`. JSON. Banner never logged. |
| later | Scheduler package; macOS as a test gate; repo Linux CI |

---

## 15. Acceptance

PR01–PR13 stand. PR13a accepts decisions 84–88. PR14 accepts decisions 89–95. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Package 1.1.0. |
| 1.6 + PR10-09 | 24 Sep 2026 | PR10 doors. Package 1.2.0. |
| 1.6 + PR10b-04 | 24 Sep 2026 | Restored decisions 1–45. Kept 46–53. |
| 1.6 + PR11-07 | 28 Oct 2026 | PR11 doors. Decisions 54–64. Decision 41 locked. Package 1.7.0. |
| 1.6 + PR12-07 | 31 Oct 2026 | PR12 doors. Decisions 65–75. Decision 41 amended only for `ScanPorts`. No SYN. Package 2.0.0. |
| 1.6 + PR13-07 | 31 Oct 2026 | PR13 doors. Decisions 76–83. |
| 1.6 + PR13A-03 | 31 Oct 2026 | PR13a doors. Decisions 84–88. Flat open row. |
| 1.6 + PR14-08 | 31 Oct 2026 | PR14 doors. Decisions 89–95. Stamp 2.1.0. Opt-in MAC. IPv6 hosts. `ProbeTls`. JSON. Banner never logged. |
