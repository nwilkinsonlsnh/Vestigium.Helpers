# Vestigium.Helpers.Network.Raw — PR01 Requirements

**Document ID:** VEST-HLP-NETWORK-RAW-PR01-REQ  
**Status:** Proposed  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network.Raw`  
**Binding:** This package is elevated. `Vestigium.Helpers.Network` stays connect-scan only.  
**Companion:** [`PR01 -- Implementation Plan.md`](PR01%20--%20Implementation%20Plan.md)

One-sentence goal: a separate package for probes that need raw sockets and admin / `CAP_NET_RAW`, starting with a SYN scan, so `Vestigium.Helpers.Network` never takes that privilege.

This paper is proposed. Nothing here is locked until we agree the name and the set.

---

## Why a second package

| Package | Privilege | What it does |
|---|---|---|
| `Vestigium.Helpers.Network` | None | Connect scan, ICMP, DNS, tables, prefix math. Decision 74 stands. |
| `Vestigium.Helpers.Network.Raw` | Admin / `CAP_NET_RAW` | Raw sockets. SYN scan first. |

A host that does not need SYN does not reference this package.

---

## Proposed decisions

| # | Decision | Proposed as |
|---|---|---|
| R1 | Name | `Vestigium.Helpers.Network.Raw`. |
| R2 | Privilege | Documented. Missing rights return a typed denial. Never spawn a helper process. |
| R3 | First door | `SynScan`. One host or a small host list, plus a `PortSet`. Cap 1024 ports. |
| R4 | Result | Open / closed / filtered. Half-open. Does not complete the handshake. |
| R5 | No CIDR | Same rule as Network. Hosts are supplied. |
| R6 | No TLS, no HTTP | This package is the raw path. Confirm stays in Network. |
| R7 | Platforms | Windows and Linux. Same public API. macOS is best-effort. |

---

## Out of PR01

| Item | Why not |
|---|---|
| Packet capture | Different job. Not a scan. |
| OS fingerprint from raw options | Still inference-only, and not in this PR. |
| CIDR expansion | Caller supplies hosts. |
| Moving connect-scan here | Connect scan stays in Network. |

---

## Acceptance

1. A second project exists. Network does not gain a raw socket.
2. `SynScan` is half-open and capped.
3. Missing rights is a typed denial, not a throw from the stack with no name.
4. No process spawn.
