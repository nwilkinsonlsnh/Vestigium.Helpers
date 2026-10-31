# Vestigium.Helpers.Network — PR13 Requirements

**Document ID:** VEST-HLP-NETWORK-PR13-REQ  
**Status:** Closed  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` **2.1.0**.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the doors.  
**Companion:** [`PR13 -- Implementation Plan.md`](PR13%20--%20Implementation%20Plan.md)

One-sentence goal: make the 2.0.0 scanner usable for lab support — percent progress, open-only results, speak-first banners, lab port sets, and a separate host list from a prefix — without becoming a subnet expander, a SYN scanner, or an OS fingerprinter.

Closed. Shipped as package **2.1.0**. Owner gate passed.

---

## Decisions locked

| # | Decision | Locked as |
|---|---|---|
| 76 | Progress has a total | `NetworkProgress.Sent` is the expected probe count. `Received` is finished. A host can show percent. |
| 77 | Open-only hits | `PortScanOptions.OpenOnly` drops closed, timed-out, and error hits from `Hits`. Host rows already hold open ports. Default false so existing callers stay the same. |
| 78 | Banner on the scan | `PortScanOptions.ReadBanner` passes through to `TcpConnect`. Off by default. Confirm stays a separate flag. |
| 79 | Lab port sets | `WellKnown` gains `windows`, `printers`, and `databases`. Still under the 1024 cap. `common` and `catalog` stay. |
| 80 | Host list is a separate door | `NetworkHelper.HostsFromPrefix` returns a capped host list from an existing prefix. It does not scan. `ScanPorts` still does not expand a CIDR. |
| 81 | Rollup text | A pure function turns a `PortScanResult` into CSV text. No file write. |
| 82 | SYN still out | Decision 74 stands. |
| 83 | Decision 41 stands on the single-port doors | `TcpConnect` and `UdpProbe` stay one host, one port. |

---

## What the caller can ask after PR13

| Question | Answer | Source |
|---|---|---|
| How far is the scan? | Sent / received | Progress total |
| Show me only the open ports | Filtered hits | `OpenOnly` |
| Did SSH / FTP / SMTP speak first? | Banner | `ReadBanner` on the scan |
| Scan the Windows admin set | Named set | `WellKnown("windows")` |
| What hosts are in this prefix? | Capped list | `HostsFromPrefix` — not a scan |
| Give me a table | CSV text | Pure rollup |

---

## Out of this release

| Item | Why not |
|---|---|
| CIDR expansion inside `ScanPorts` | Decision 58. The host list is a separate door. |
| SYN / half-open scan | Decision 74. |
| OS fingerprinting | Still inference-only. |
| SNMP walk or more OIDs | `sysDescr` stays the one Get. |
| TLS conversation | Confirm stays clear-text HTTP. |
| Plot API, packed IEEE OUI, default-route write, packet capture, process spawn | Unchanged locks. |

---

## Acceptance

Owner gate passed. Package **2.1.0** listed. Decisions 76–83 are in the SRS.
