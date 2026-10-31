# Vestigium.Helpers.Network — PR14 Requirements

**Document ID:** VEST-HLP-NETWORK-PR14-REQ  
**Status:** Open  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` next after **2.1.0**. This paper does not set the version number.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the door.  
**Companion:** [`PR14 -- Implementation Plan.md`](PR14%20--%20Implementation%20Plan.md)

One-sentence goal: close the lab gaps — MAC, IPv6 host list, TLS certificate, JSON — and lock the banner-logging rule.

SYN stays in `Vestigium.Helpers.Network.RawSocket`. Parity for that door is RawSocket PR02.

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

---

## What the caller can ask after PR14

| Question | Answer | Source |
|---|---|---|
| What is the MAC? | Address or null | `ResolveMac`, opt-in |
| What hosts are in this IPv6 prefix? | Capped list | `HostsFromPrefix` |
| What certificate is on 443? | Subject, issuer, expiry, protocol | `ProbeTls` |
| Give me JSON | Text | `PortScanJson.Write` |

---

## Out of this release

| Item | Why not |
|---|---|
| SYN in this package | Decision 74. RawSocket PR02 owns SYN parity. |
| CIDR inside `ScanPorts` | Decision 58. |
| Packet capture | Different package. |
| SNMP walk | `sysDescr` stays the one Get. |
| OS fingerprinting | Still inference-only. |
| Linux CI | Roadmap. Not this PR. |
| Plot API, default-route write, process spawn | Unchanged locks. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| `ResolveMac` | Off. |
| `ProbeTls` on the scan | Off. Standalone door is the ask. |
| Host list cap | 1024, both families. |
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
8. SRS, Design, Guide, and the package README amended in the same change that lands the doors.
