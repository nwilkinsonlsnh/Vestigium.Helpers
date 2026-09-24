# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.6  
**Status:** Locked companion to PR01–PR10.  
**Date:** 24 September 2026  
**Package:** `Vestigium.Helpers.Network` **1.2.0**  
**TFM:** `net10.0` (.NET 10 LTS) — **Windows and Linux are first-class**. Not `net10.0-windows`.  
**Companion:** [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md), [`Design_v1.6.md`](Design_v1.6.md)

If implementation and this file disagree, this file wins **except** where a later PR plan in this folder explicitly amends a lock and lands the amendment here in the same change.

This package is a **library of resources**, not a tool. Hosts subscribe. It does not spawn `ping`, `tracert`, `pathping`, `ip`, `route`, `arp`, or `nslookup`.

This library does not plot and will not grow a plot API. Results are network facts.

---

## 2. Decisions locked in this version

Decisions 1–45 stand from PR01–PR09. Additions:

| # | Decision | Locked as |
|---|---|---|
| 46 | Bound ICMP | When `InterfaceIndex` or `SourceAddress` is set, echo uses a bound ICMP path. Omit both and BCL `Ping` may stay. No `ping(8)`. |
| 47 | PMTU status | `PacketTooBig` / DF fail shrinks. Timeout is unknown and does not lower the ceiling. All unknown → Failed, `LargestPayload` null. |
| 48 | Neighbor resolve | `ProbeNeighbor` is one-address resolve. `GetNeighbors` stays the table dump. |
| 49 | Pathping sample protocol | Phase 2 uses the protocol the walk settled on. |
| 50 | Recipe bind | Campaign recipe writes bind when set. Old recipes open unset. |
| 51 | UdpProbe | One host, one port. Replied, unreachable, or timed out. No port list. |
| 52 | ProbeDns | One name, optional server. Answered, refused, or timed out. Not HTTP. |
| 53 | WatchAdapter | One adapter, one duration. Oper-status and speed samples. Does not bill. Does not plot. |

Decisions 34 and 41 stand: no default-route write, no port sweep.

---

## 5. Protocol jobs

v1.2 surface plus PR09 doors plus PR10 doors: `UdpProbe`, `ProbeDns`, `WatchAdapter`.

---

## 6. Campaigns

Create persists Echo bind when the caller set a non-zero index or a source. Open without those fields leaves `InterfaceIndex = 0` and `SourceAddress` null.

---

## 9. Logging

Named events used through **14560**. New jobs reuse existing subcategories. No packet bytes. No credentials.

---

## 12. Non-goals

Spawn CLI tools. Install cron/schtasks/systemd. HTTP reachability client. Packet capture. Default-route write. Plotting. Port sweep.

---

## 13. Roadmap

| Version | Item |
|---|---|
| PR09 | Package 1.1.0 — **shipped** |
| PR10 | Bound ICMP, PMTU status, neighbor resolve, pathping sample protocol, recipe bind, UdpProbe, ProbeDns, WatchAdapter — **this amendment. Package 1.2.0.** |
| later | Scheduler package; macOS as a test gate; repo Linux CI |

---

## 14. Mapping

| Cousin | Protocol | API |
|---|---|---|
| ping | ICMP Echo | IcmpEcho |
| traceroute | ICMP / UDP / TCP TTL | IcmpTrace |
| pathping | walk + sample | Pathping |
| — | one UDP datagram | UdpProbe |
| nslookup / dig | DNS | LookupAsync / ProbeDns |
| — | NIC oper-status | WatchAdapter |

Cousins are documentation. Never spawned.

---

## 15. Acceptance

PR01–PR09 stand. PR10 accepts decisions 46–53 and package **1.2.0**. `1.1.0` does not contain these doors.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | PR09 doors. Package 1.1.0. |
| 1.6 + PR10-09 | 24 Sep 2026 | PR10 doors. Package 1.2.0. |
