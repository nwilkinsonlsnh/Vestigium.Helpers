# Vestigium.Helpers.Network.RawSocket — PR02 Requirements

**Document ID:** VEST-HLP-RAWSOCKET-PR02-REQ  
**Status:** Open  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network.RawSocket` next after **1.0.0**. This paper does not set the version number.  
**Companion:** [`PR02 -- Implementation Plan.md`](PR02%20--%20Implementation%20Plan.md)

One-sentence goal: give `SynScan` the same host-facing doors as connect scan — progress, rows, a parallelism cap, and a bind pin — without leaving this package.

SYN stays here. `Vestigium.Helpers.Network` stays connect-scan only.

---

## Decisions locked for PR02

| # | Decision | Locked as |
|---|---|---|
| R2 | SYN progress | `Sent` is the expected probe count. `Received` is finished. |
| R3 | SYN rows | `SynScanCsv.Rows` returns one row per open port. `Write` formats those rows. No file. |
| R4 | Parallelism | Caller sets a cap. Default 8. Max 32. Lower than connect scan. |
| R5 | Bind pin | Optional `InterfaceIndex` or `SourceAddress`. Same shape as Network. |
| R6 | Windows raw TCP | Often denied even for admin. Typed denial. No WinDivert. No Npcap. No connect-scan fallback. |
| R7 | Caps stay | 64 hosts. 1024 ports. IPv4. Hosts are supplied. No CIDR. |

---

## What the caller can ask after PR02

| Question | Answer | Source |
|---|---|---|
| How far is the SYN scan? | Sent / received | Progress |
| Bind a grid of open ports | Flat rows | `SynScanCsv.Rows` |
| Paste a table | CSV text | `SynScanCsv.Write` |
| Pin a NIC | Bind set | `InterfaceIndex` or `SourceAddress` |
| Why did Windows deny it? | Typed denial | `raw-tcp-unavailable` |

---

## Out of this release

| Item | Why not |
|---|---|
| SYN in `Vestigium.Helpers.Network` | Decision 74. |
| CIDR expansion | Hosts are supplied. |
| Packet capture | Different package. |
| WinDivert / Npcap | Out. |
| Connect-scan fallback | A missing raw socket is a denial, not a silent downgrade. |
| IPv6 SYN | Later. |
| TLS or HTTP | Out. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| Parallelism | 8. Max 32. |
| Host cap | 64. |
| Port cap | 1024. |
| Family | IPv4. |
| Commit | `RawSocket PR02: <id short goal>` |

---

## Acceptance

1. Progress reports expected probes and finished probes.
2. `Rows` is one open port per row. `Write` uses those rows.
3. Parallelism defaults to 8 and rejects over 32.
4. Bind pin is optional and unset by default.
5. Windows raw TCP denial is documented. No fallback.
6. README amended in the same change that lands the doors.
