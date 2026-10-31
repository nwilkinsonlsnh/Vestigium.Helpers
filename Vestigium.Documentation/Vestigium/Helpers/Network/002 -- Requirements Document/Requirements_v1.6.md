# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.6  
**Status:** Locked companion to PR01–PR13.  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` **2.0.0**  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md), [`Design_v1.6.md`](Design_v1.6.md)

If implementation and this file disagree, this file wins **except** where a later PR plan in this folder explicitly amends a lock and lands the amendment here in the same change.

This package is a **library of resources**, not a tool. Hosts subscribe. It does not spawn `ping`, `tracert`, `pathping`, `ip`, `route`, `arp`, or `nslookup`.

This library does not plot and will not grow a plot API.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 76 | Progress has a total | `NetworkProgress.Sent` is the expected probe count after discovery. `Received` is finished. |
| 77 | Open-only hits | `PortScanOptions.OpenOnly` drops closed, timed-out, and error hits. Default false. |
| 78 | Banner on the scan | `PortScanOptions.ReadBanner` passes through to `TcpConnect`. Off by default. `Confirm` stays a separate flag. |
| 79 | Lab port sets | `WellKnown` gains `windows`, `printers`, and `databases`. Cap 1024. |
| 80 | Host list is a separate door | `NetworkHelper.HostsFromPrefix` returns a capped IPv4 host list. It does not scan. `ScanPorts` still does not expand a CIDR. |
| 81 | Rollup text | `PortScanCsv.Write` turns a `PortScanResult` into CSV text. No file write. |
| 82 | SYN still out | Decision 74 stands. Half-open lives in `Vestigium.Helpers.Network.RawSocket`. |
| 83 | Decision 41 stands | `TcpConnect` and `UdpProbe` stay one host, one port. |

Decisions 1–75 stand in this file as locked by PR01–PR12. Decision 41 is amended only for `ScanPorts`. Decision 58 stands: the scanner does not expand a CIDR. Decision 74 stands: no SYN in this package.

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
| later | Scheduler package; macOS as a test gate; repo Linux CI |

---

## 15. Acceptance

PR01–PR12 stand. PR13 accepts decisions 76–83. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay. Owner gate is PR13-08.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Package 1.1.0. |
| 1.6 + PR10-09 | 24 Sep 2026 | PR10 doors. Package 1.2.0. |
| 1.6 + PR10b-04 | 24 Sep 2026 | Restored decisions 1–45. Kept 46–53. |
| 1.6 + PR11-07 | 28 Oct 2026 | PR11 doors. Decisions 54–64. Decision 41 locked. Package 1.7.0. |
| 1.6 + PR12-07 | 31 Oct 2026 | PR12 doors. Decisions 65–75. Decision 41 amended only for `ScanPorts`. No SYN. Package 2.0.0. |
| 1.6 + PR13-07 | 31 Oct 2026 | PR13 doors. Decisions 76–83. Progress total, `OpenOnly`, `ReadBanner`, lab sets, `HostsFromPrefix`, CSV. |
