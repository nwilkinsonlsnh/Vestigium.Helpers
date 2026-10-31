# Vestigium.Helpers.Network — PR14 Requirements

**Document ID:** VEST-HLP-NETWORK-PR14-REQ  
**Status:** Open  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` next after **2.1.0**. RawSocket work lands in `Vestigium.Helpers.Network.RawSocket`. This paper does not set the version number.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the door.  
**Companion:** [`PR14 -- Implementation Plan.md`](PR14%20--%20Implementation%20Plan.md)

One-sentence goal: close the lab gaps — MAC, IPv6 host list, TLS certificate, JSON — and the RawSocket parity gaps, and lock the banner-logging rule.

---

## Decisions locked for PR14

| # | Decision | Locked as |
|---|---|---|
| 89 | Stamp 2.1.0 | README, Guide, and Design say **2.1.0**. The next bump is this PR, not a rewrite of 2.1.0. |
| 90 | Platform attributes | Windows-only Registry and macOS-unsupported calls are marked. Pack has no CA1416 on those sites. |
| 91 | Opt-in MAC | `PortScanOptions.ResolveMac` asks `ProbeNeighbor` per live host. Default false. Miss stays null. |
| 92 | IPv6 host list | `HostsFromPrefix` returns usable IPv6 hosts. Same 1024 cap. Over the cap throws. It does not scan. |
| 93 | TLS certificate probe | `ProbeTls` returns subject, issuer, expiry, and protocol. One handshake. No HTTP. Off the scan by default. |
| 94 | JSON of a scan | `PortScanJson.Write` returns text. Pure. No file. Banner is a field, not a log line. |
| 95 | Banner is not logged | The library never writes a banner to the audit log. A host must not log it raw. |
| 96 | SYN progress | `SynScan` reports `Sent` as expected probes and `Received` as finished. |
| 97 | SYN rows | `SynScanCsv.Rows` and `Write`. Open ports only. No file. |
| 98 | SYN parallelism | Caller sets a cap. Default 8. Max 32. Lower than connect scan. |
| 99 | SYN bind | Optional `InterfaceIndex` or `SourceAddress`. Same pin as Network. |
| 100 | Windows raw TCP | Documented. Often denied even for admin. No WinDivert. No Npcap. No connect-scan fallback. |

---

## What the caller can ask after PR14

| Question | Answer | Source |
|---|---|---|
| What is the MAC? | Address or null | `ResolveMac`, opt-in |
| What hosts are in this IPv6 prefix? | Capped list | `HostsFromPrefix` |
| What certificate is on 443? | Subject, issuer, expiry, protocol | `ProbeTls` |
| Give me JSON | Text | `PortScanJson.Write` |
| How far is the SYN scan? | Sent / received | RawSocket progress |
| Bind a SYN grid | Flat rows | `SynScanCsv.Rows` |

---

## Out of this release

| Item | Why not |
|---|---|
| SYN in the main package | Decision 74. |
| CIDR inside `ScanPorts` | Decision 58. |
| Packet capture | Different package. |
| SNMP walk | `sysDescr` stays the one Get. |
| OS fingerprinting | Still inference-only. |
| WinDivert / Npcap | Out. Windows raw TCP stays a typed denial. |
| Linux CI | Roadmap. Not this PR. |
| Plot API, default-route write, process spawn | Unchanged locks. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| `ResolveMac` | Off. |
| `ProbeTls` on the scan | Off. Standalone door is the ask. |
| Host list cap | 1024, both families. |
| SYN parallelism | 8. Max 32. |
| Banner in the audit log | Never. |
| Commit | `Network PR14: <id short goal>` |

---

## Acceptance

1. Contract text says 2.1.0 where it still said 2.0.0.
2. CA1416 is gone on the marked sites.
3. `ResolveMac` is opt-in and misses as null.
4. `HostsFromPrefix` returns IPv6 hosts under the cap.
5. `ProbeTls` returns a certificate summary and does not speak HTTP.
6. `PortScanJson.Write` is pure text.
7. The library does not log a banner.
8. RawSocket has progress, rows, a parallelism cap, and a bind pin.
9. Windows raw TCP denial is documented. No fallback.
10. SRS, Design, Guide, and both READMEs amended in the same change that lands the doors.
