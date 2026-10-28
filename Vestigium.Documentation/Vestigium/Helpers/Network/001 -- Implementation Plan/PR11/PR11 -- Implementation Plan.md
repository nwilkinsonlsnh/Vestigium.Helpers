# Vestigium.Helpers.Network — PR11 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR11  
**Status:** Open  
**Date:** 28 October 2026  
**Requirements:** [`PR11 -- Requirements.md`](PR11%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR11: <step>`.

One-sentence goal: add coarse device/vendor inference and a small set of path and identity probes (TTL summary, TCP MSS, optional banner, optional SNMP `sysDescr`) on top of the existing single-port and ICMP doors.

This version is not a port scanner, an OS detector, an SNMP manager, an actual hop walk, or a neighbor/MAC resolution.

PR11-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR11-01 | `InferredDeviceClass` and `InferredVendor` enums. Pure scorer that takes evidence and returns class, vendor, confidence, and the evidence list. No I/O. | Open |
| 2 | PR11-02 | TTL summary helper. Pure function over existing `IcmpEchoReply` list: min/max/mode TTL and **estimated** hop count. Does not call `IcmpTrace` / `Trace`. | Open |
| 3 | PR11-03 | TCP MSS on successful `TcpConnect`. Capture when the stack exposes the option. Miss is null. Status unchanged. | Open |
| 4 | PR11-04 | Optional banner read on TCP connect. Short, capped length and time. Off by default. No protocol conversation. | Open |
| 5 | PR11-05 | Optional one-shot SNMP Get of `sysDescr` (`1.3.6.1.2.1.1.1.0`). Caller supplies community. Typed miss on timeout or error. No walk. No set. | Open |
| 6 | PR11-06 | Surface local adapter MTU on `NetworkAdapter` if not already present. Document that remote frame size is existing `PathMtu`. | Open |
| 7 | PR11-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 54–64 land. Decision 41 does not move. | Open |
| 8 | PR11-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR11-01

Enums are the locked set. The scorer is a pure function. Inputs are a list of evidence items (port open, OUI match when the caller already has a MAC, banner snippet, SNMP fragment). Output always includes the evidence that contributed. Confidence is derived from independent signals, not from a single port. No network call inside the scorer. No neighbor or MAC lookup.

### PR11-02

No new probe. Consumes replies the caller already has from `IcmpEcho`. Estimated hops use the conventional initial TTL (128 for many Windows, 64 for many Unix-like) only as a label, not as proof. Does not call `IcmpTrace` / `Trace`. Actual hops stay on the existing trace job.

### PR11-03

Extend the successful `TcpConnect` path. If the platform socket or the SYN-ACK exposes MSS, store it on the result. If not, null. Do not fail the connect because MSS is missing.

### PR11-04

New option on the TCP connect options (or a sibling flag). When set, after a successful connect, read up to a fixed byte cap for a fixed time, then return the bytes as text when they are printable. Default remains off. Do not send a protocol-specific request in this slice.

### PR11-05

One UDP datagram. One OID. Community is required and is never logged. Result is the `sysDescr` string or a typed status (answered, timed out, error). No GetNext. No bulk. No other OIDs.

### PR11-06

Check `NetworkAdapter`. If MTU is already there, document it and do nothing. If not, add the field from the existing inventory path. Remote sizing stays `PathMtu`.

### PR11-07

Amend `Requirements_v1.6.md`, `Design_v1.6.md`, `DevelopersGuide_v1.6.md`, and the package README in place. Add decisions 54–64. Leave Decision 41 locked. Do not open a second requirements file.

### PR11-08

Owner runs the filter on the clone. This agent does not mark the release closed.

## What this release does not do

- Port sweep or a library job that takes a port list.
- Protocol conversation beyond a short banner read.
- SNMP walk, set, or OIDs other than `sysDescr`.
- CDP or LLDP.
- OS fingerprinting.
- A definitive OS or vendor claim.
- An actual hop walk (`IcmpTrace` / `Trace`). Estimated hops from TTL only.
- Neighbor or MAC resolution (`ProbeNeighbor`, `GetNeighbors`). OUI is consume-only when the caller already has a MAC.
- Plot API, packed IEEE OUI, default-route write, packet capture, or process spawn.

## Next action

PR11-01. Enums and the pure scorer first, so every later probe has a place to feed evidence.
