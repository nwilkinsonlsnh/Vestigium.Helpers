# Vestigium.Helpers.Network — PR11 Requirements

**Document ID:** VEST-HLP-NETWORK-PR11-REQ  
**Status:** Open  
**Date:** 28 October 2026  
**Package:** `Vestigium.Helpers.Network` next after **1.2.0**. This paper does not set the version number.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the doors.  
**Companion:** [`PR11 -- Implementation Plan.md`](PR11%20--%20Implementation%20Plan.md)

One-sentence goal: let a diagnostic host ask useful questions of a target for lab support, connectivity checking, and security analysis on a system — path size and TTL, plus a coarse inferred device class and vendor — without becoming a port scanner or an OS detector.

This version is not: a port sweep, a banner-grab suite, an SNMP manager, CDP/LLDP, OS fingerprinting, a hard claim that any host is Windows, Linux, macOS, Android, or Cisco, an actual hop walk, or a neighbor/MAC resolution.

---

## Decisions locked for PR11

| # | Decision | Locked as |
|---|---|---|
| 54 | Inference only | Results are `Inferred*`. Never a definitive OS or device claim. Always return the evidence list. |
| 55 | Device class enum | `InferredDeviceClass`: `Unknown`, `WindowsHost`, `UnixLikeHost`, `NetworkDevice`, `MobileOrEmbedded`, `PrinterOrAppliance`. |
| 56 | Vendor enum | `InferredVendor`: `Unknown`, `Cisco`, `Juniper`, `Arista`, `PaloAlto`, `Fortinet`, `Other`. |
| 57 | Scoring is pure | A function that consumes evidence (open ports, OUI match, optional banner snippet, optional SNMP `sysDescr` fragment) and returns class, vendor, confidence, and the evidence used. No I/O inside the scorer. |
| 58 | No port sweep | Decision 41 stands. No range, no CIDR expansion, no caller-supplied port list as a library job. The caller may fire existing single-port jobs. |
| 59 | Banner read | Optional short read of the first bytes after a successful TCP connect. Cap the length. Cap the time. Do not speak a protocol conversation. |
| 60 | TCP MSS | On a successful `TcpConnect`, capture the MSS option from the SYN-ACK when the stack exposes it. Miss stays null. |
| 61 | TTL summary | Thin helper over existing ICMP replies: min/max/mode observed TTL and an **estimated** hop count. Does not probe. Does not call `IcmpTrace` / `Trace`. Actual hops stay on the existing trace job. |
| 62 | SNMP sysDescr | Optional one-shot UDP Get of `1.3.6.1.2.1.1.1.0`. Community string is a caller parameter. Result is the string or a typed miss (timeout, no response, error). No walk. No set. No other OIDs in this PR. |
| 63 | OUI reuse | Scoring may consume an existing OUI lookup result when the caller already has a MAC. This path does not call `ProbeNeighbor` or `GetNeighbors` and does not obtain a MAC. This PR does not pack the IEEE registry. |
| 64 | Local adapter MTU | If `NetworkAdapter` does not already expose the interface MTU, surface it. Remote frame size is path MTU (existing `PathMtu`). |

---

## What the caller can ask after PR11

| Question | Answer | Source |
|---|---|---|
| What is the path size to this target? | Largest DF payload that passed, plus hit-ceiling | Existing `PathMtu` |
| What TTL did the target return? | Observed TTL on each reply; optional min/max/mode and **estimated** hops | Existing ICMP + new summary |
| What is the local interface MTU? | NIC MTU | Inventory (surface if missing) |
| What MSS did the TCP handshake offer? | MSS or null | New on `TcpConnect` |
| What well-known service is this port number? | Catalog name | Existing `TryPort` / `TryService` |
| Is this port open, and does it speak first? | Connect result + optional short banner | Existing `TcpConnect` + new banner option |
| What kind of device is this, and which vendor, inferred? | Class, vendor, confidence, evidence | New scorer |
| Does managed gear answer SNMP sysDescr? | String fragment or typed miss | New one-shot probe |

---

## Out of this release

| Item | Why not |
|---|---|
| Port sweep / port set / staggered multi-port job | Decision 41. Caller orchestrates single-port jobs if needed. |
| Full banner-grab or protocol conversation (EHLO, SSH negotiation past version line, HTTP client) | Decision 31 and scope. Banner is a short read only. |
| SNMP walk, set, or other OIDs | One Get of `sysDescr` only. |
| CDP / LLDP | Layer-2. Not a TCP/UDP port probe. |
| OS fingerprinting from TTL + TCP options + window | Fragile. Not required for lab support. |
| Hard OS detection (Windows / Linux / macOS / Android as fact) | Inference only. |
| Actual hop walk (`IcmpTrace` / `Trace`) | Estimated hops from TTL are enough for this path. Actual hops stay on the existing trace job. |
| Neighbor / MAC resolution (`ProbeNeighbor`, `GetNeighbors`) | This path does not obtain a MAC. OUI may be consumed only if the caller already has one. |
| Plot API, packed IEEE OUI, default-route write, packet capture, process spawn | Unchanged locks. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| Inference | Always evidence + confidence. Never a bare enum as the only result. |
| Banner | Optional. Length and time capped. Off by default. |
| SNMP | Optional. One OID. Caller supplies community. Off unless asked. |
| TCP MSS | Captured when available. Null on miss. Does not change `TcpConnect` status. |
| Hop count | Estimated from TTL only. No trace job from this path. |
| MAC | Not obtained here. OUI is consume-only when the caller already has a MAC. |
| Port loop | Still forbidden inside the library. |
| Commit | `Network PR11: <id short goal>` |

---

## Acceptance

1. Enums and pure scorer exist and return evidence with every inference.
2. Banner read is optional, capped, and does not speak a protocol.
3. TCP MSS is captured or null on successful connect.
4. TTL summary is a pure function over existing replies and produces an estimated hop count only.
5. SNMP is one Get of `sysDescr` or a typed miss.
6. No port sweep job. Decision 41 remains locked.
7. No call to `IcmpTrace` / `Trace` and no call to `ProbeNeighbor` / `GetNeighbors` from this path.
8. SRS, Design, Guide, and package README amended in the same change that lands the doors.
