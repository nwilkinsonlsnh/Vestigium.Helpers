# Vestigium.Helpers.Network — PR12 Requirements

**Document ID:** VEST-HLP-NETWORK-PR12-REQ  
**Status:** Open  
**Date:** 28 October 2026  
**Package:** `Vestigium.Helpers.Network` next after **1.7.0**. This paper does not set the version number.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the doors.  
**Companion:** [`PR12 -- Implementation Plan.md`](PR12%20--%20Implementation%20Plan.md)

One-sentence goal: add a bounded connect-scan job that takes hosts and ports the caller already chose, runs them with a concurrency cap and progress, and returns a rollup — without becoming a subnet expander, a SYN scanner, or an OS fingerprinter.

This version amends Decision 41 for a **port list / range / well-known set on hosts the caller supplies**. It does not expand a CIDR. It does not SYN-scan. It does not fingerprint an OS.

---

## Decisions locked for PR12

| # | Decision | Locked as |
|---|---|---|
| 65 | Sweep is a job | `NetworkHelper.ScanPorts` takes hosts and ports and returns `NetworkJob<PortScanResult>`. Progress is `IProgress<NetworkProgress>`. |
| 66 | Hosts are supplied | One host, or a small explicit list. No CIDR. No subnet expansion inside the job. The existing calculator stays a separate door. |
| 67 | Ports are supplied | One of: an explicit list, a begin–end range, or a named well-known set. Cap the expanded port count. Reject over the cap. |
| 68 | Concurrency | Caller sets a parallelism cap. Default is small. The job does not open unbounded sockets. |
| 69 | Rate and timing | Per-host delay and an overall timeout. Defaults are polite. Burst is opt-in. |
| 70 | Host discovery | Optional. ICMP (or a short TCP probe of a few ports) before the sweep. Hosts that miss are skipped. Off by default so a single known host still scans. |
| 71 | Rollup | One result: per host, open TCP, open UDP, banners, MSS, catalog name, and optional inference from evidence already collected. |
| 72 | Service confirmation | Optional short protocol probe after a successful TCP connect for a small known set (HTTP request line, no TLS conversation). Off by default. Banner read stays the default speak-first path. |
| 73 | UDP payloads | Optional known payload per a small set of UDP ports. Empty payload remains the default. One datagram. No walk. |
| 74 | SYN scan | Out. No raw sockets. No half-open. Connect scan only. |
| 75 | Decision 41 amended | Single-port `TcpConnect` / `UdpProbe` stay. The new job is the only place a list or range is allowed. |

---

## What the caller can ask after PR12

| Question | Answer | Source |
|---|---|---|
| Which of these ports are open on these hosts? | Open / refused / timeout per port | New sweep job |
| What is the well-known name? | Catalog name | Existing `TryService` |
| Did it speak first? | Optional banner | Existing banner option, used by the sweep |
| What MSS did the handshake offer? | MSS or null | Existing MSS on `TcpConnect` |
| Is the host up before I spend ports on it? | Optional discovery | Existing ICMP, gated by the sweep |
| What does the rollup say? | Host rows + open ports + evidence | New result |
| Can I confirm a known service? | Optional short probe | New, off by default |
| Does a known UDP port answer a known payload? | Replied / silent | New, empty payload default |

---

## Out of this release

| Item | Why not |
|---|---|
| CIDR / subnet expansion inside the scanner | Caller (or a thin planner) uses `DescribePrefix` / `PlanByHosts` and passes hosts. Avoids scanning a /16 by accident. |
| SYN / half-open scan | Raw sockets, elevated rights, different threat model. Connect scan only. |
| OS fingerprinting | Still inference-only from evidence the sweep already collected. |
| Full protocol suites (SMB negotiate, TLS client hello, SNMP walk) | Out. Service confirmation is a short known probe only. |
| Neighbor / MAC resolution from the sweep | Still consume-only if the caller already has a MAC. |
| Plot API, packed IEEE OUI, default-route write, packet capture, process spawn | Unchanged locks. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| Hosts | Caller-supplied. No CIDR. |
| Ports | List, begin–end, or named set. Expanded count capped. |
| Parallelism | Small default. Caller may raise it. |
| Discovery | Off by default. |
| Banner | Off by default, same as PR11. |
| Service confirmation | Off by default. |
| UDP payload | Empty by default. Known payload is opt-in. |
| SYN | Not offered. |
| Commit | `Network PR12: <id short goal>` |

---

## Acceptance

1. A sweep job accepts a host list and a port list, range, or named set.
2. Expanded port count over the cap is rejected.
3. Concurrency and overall timeout are honored.
4. Progress reports as each port finishes.
5. Result is a rollup, not a bag of single-port jobs the caller must stitch.
6. Discovery is optional and off by default.
7. Service confirmation and UDP payloads are optional and off by default.
8. No CIDR expansion. No SYN scan. No OS fingerprint.
9. SRS, Design, Guide, and package README amended in the same change that lands the doors.
