# Vestigium.Helpers.Network — PR10b Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR10B  
**Status:** Open  
**Date:** 24 September 2026  
**Backlog:** [`PR10b -- Backlog.md`](PR10b%20--%20Backlog.md)  
**Binding:** Requirements win. Commit: `Network PR10b: <step>`.

One-sentence goal: make the PR10 doors tell the truth, then run the same Network filter.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR10b-01 | Typed IP Helper neighbor row. IPv6 MAC when present. Linux exact-line stays; no `GetNeighbors()` scan as primary. | Open |
| 2 | PR10b-02 | After bind, assert local endpoint matches source / NIC. Mismatch → Failed. | Open |
| 3 | PR10b-03 | Pathping UDP/TCP sample applies `InterfaceIndex` and `SourceAddress`. | Open |
| 4 | PR10b-04 | Put decisions 1–45 back into `Requirements_v1.6.md`. Keep 46–53. | Open |
| 5 | PR10b-05 | Owner: `FullyQualifiedName~Network` | Owner |

## Slice notes

### PR10b-01

Replace the 128-byte `NeighborResolve` blob with sequential structs that match `ResolveIpNetEntry2` / `GetIpNetEntry2`. Fill `Address` and `InterfaceIndex`. Read `PhysicalAddress` / `PhysicalAddressLength` from the struct, not from guessed offsets. Windows IPv4 may still use `SendARP` as a fast path after the typed resolve misses. Tests: garbage address still throws; loopback probe still returns a result without calling `GetNeighbors()` in `NeighborProbeEngine`.

### PR10b-02

In `BoundIcmpEcho`, after `EgressBind.Apply`, if `SourceAddress` parsed, `LocalEndPoint` address must equal it. If only `InterfaceIndex` is set, local address must live on that adapter (`EgressBind.AddressLivesOn`). Fail the reply as `Failed` with detail `bind-miss` when it does not. Do not throw past the job.

### PR10b-03

`IcmpTraceEngine.UdpProbeAsync` takes `interfaceIndex` and `sourceAddress` and calls `EgressBind.Apply`. Pathping phase 2 passes the pathping options through. TCP sample already does. Add a source test that a negative index still rejects at pathping create (existing bind guard) and that the UDP method signature includes the bind arguments.

### PR10b-04

The PR09-era decisions 1–45 text is the lock. Restore it from git history of `Requirements_v1.6.md` before the PR10-09 condense (`b7d4772` parent). Then append 46–53 as they stand now. Do not invent new locks.

### PR10b-05

Owner runs the filter. This agent does not mark 1.2.0 closed.

## Next action

PR10b-01. Typed neighbor row first.
