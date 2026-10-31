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
| 80 | Host list is a separate door | `NetworkHelper.HostsFromPrefix` returns a capped host list. IPv4 usable hosts. IPv6 hosts. Cap 1024. It does not scan. `ScanPorts` still does not expand a CIDR. |
| 89 | Stamp 2.1.0 | README, Guide, and Design say **2.1.0**. |
| 90 | Platform attributes | Windows-only Registry and macOS-unsupported calls are marked. |
| 91 | Opt-in MAC | `PortScanOptions.ResolveMac` asks `ProbeNeighbor` per live host. Default false. Miss stays null. Inference still does not obtain a MAC. |
| 92 | IPv6 host list | Decision 80 is both families. Same cap. Over the cap throws. No scan. |
| 93 | TLS certificate probe | `ProbeTls` returns subject, issuer, expiry, and protocol. One handshake. No HTTP. Not on the scan by default. |
| 94 | JSON of a scan | `PortScanJson.Write` returns text. Pure. No file. Banner is a field, not a log line. |
| 95 | Banner is not logged | The library never writes a banner to the audit log. The result still carries it. The host may show or save it. |

Decisions 1–88 stand. Decision 31 (no TLS conversation) is not a certificate probe. `ProbeTls` is one handshake and no HTTP. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay.

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
