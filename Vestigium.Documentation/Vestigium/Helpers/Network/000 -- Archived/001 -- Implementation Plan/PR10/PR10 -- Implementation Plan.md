# Vestigium.Helpers.Network — PR10 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR10  
**Status:** Open  
**Date:** 24 September 2026  
**Backlog:** [`PR10 -- Backlog.md`](PR10%20--%20Backlog.md)  
**Binding:** Requirements win. Commit: `Network PR10: <step>`.

One-sentence goal: make the 1.1.0 doors true, then add UdpProbe, ProbeDns, and WatchAdapter, as package **1.2.0**.

PR10-10 is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column. The ID is the backlog number. Lane A is 1–5. Lane B is 6–8.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR10-01 | Bound ICMP echo. Bind set → bound ICMP path. Bind omitted → BCL `Ping` may stay. | Open |
| 2 | PR10-02 | PMTU: shrink only on sized reject (`PacketTooBig` / DF fail). Timeout is unknown. | Open |
| 3 | PR10-03 | `ProbeNeighbor` is one-address resolve, not a table filter. | Open |
| 4 | PR10-04 | Pathping phase 2 uses the walk's settled `ProbeProtocol`. | Open |
| 5 | PR10-05 | Campaign recipe persists bind when set. Old recipes open unset. | Open |
| 6 | PR10-06 | `NetworkHelper.UdpProbe(host, port, options)`. One datagram. | Open |
| 7 | PR10-07 | `NetworkHelper.ProbeDns(name, options)` with optional server. Answered / refused / timed out. | Open |
| 8 | PR10-08 | `NetworkHelper.WatchAdapter(nameOrId, options)`. Status samples over a duration. | Open |
| 9 | PR10-09 | Amend SRS, Design, Guide, README. Version **1.2.0**. | Open |
| 10 | PR10-10 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR10-01

`IcmpEchoOptions` already has `InterfaceIndex` and `SourceAddress`. Today `Validate` runs and BCL `Ping` ignores them. After this slice, a set index or source sends the echo on a socket that `EgressBind.Apply` owns. Omit both and the job may keep `Ping`. Linux unprivileged ICMP DGRAM is allowed when the kernel allows it. No `ping(8)`.

### PR10-02

`PathMtuTry` gains enough status to tell **passed**, **too big**, and **unknown**. Binary search only lowers `hi` on too-big. Unknown does not move the bounds; the walk records the try and continues on the other half only when a sized reject or a pass exists. If every try is unknown, `LargestPayload` stays null and the job is Failed. HitCeiling still means a pass at `MaxPayload`.

### PR10-03

`ProbeNeighbor(address)` asks the stack for that address. Windows: `SendARP` may remain the IPv4 fast path; IPv6 and the miss path use `ResolveIpNetEntry2` (or the existing IP Helper neighbor resolve). Linux: one-address netlink, not "read `/proc/net/arp` and pick a line" as the primary path. Result is still `NeighborProbeResult`. Garbage address is still a reject. `GetNeighbors()` does not change.

### PR10-04

Pathping already reuses the trace walk. Phase 2 today calls `IcmpEcho`. After this slice it sends the same `ProbeProtocol` the last named hop used, including TCP port when that protocol is TCP. Bind and family stay on the sample. Link-loss math does not change.

### PR10-05

`CampaignEchoDto` gains `interfaceIndex` and `sourceAddress`. Write them only when the caller set a non-zero index or a source. Open without those fields leaves `InterfaceIndex = 0` and `SourceAddress = null`. Guard stays `EgressBind.Validate`.

### PR10-06

`UdpProbe` is one datagram to one host and one port. Options: timeout, bind, family, optional payload ≤ echo max. Status: `Replied`, `Unreachable`, `TimedOut`. Elapsed ms always. No list of ports. No stagger. No in-flight cap — there is one probe.

### PR10-07

`ProbeDns` is "does this name return from this server." Options reuse `DnsLookupOptions` (server, bind, family, timeout). Status: `Answered`, `Refused`, `TimedOut`. Records from a successful lookup may be attached. This is not HTTP and not a recursive scanner.

### PR10-08

`WatchAdapter` is one adapter, one duration (10 ms–1 hour), optional interval. Each sample stores time, `OperationalStatus`, and `SpeedBitsPerSecond`. No byte counters (that is `SampleCounters`). No `BillP95`. Missing adapter is a reject.

### PR10-09

Amend `Requirements_v1.6.md`, `Design_v1.6.md`, `DevelopersGuide_v1.6.md`, and the package README in place. Do not open a second requirements file. csproj `<Version>` and the README consume line become `1.2.0`. Decisions 34 and 41 do not move.

### PR10-10

Owner runs the filter on the clone. This agent does not mark the release closed.

## What this release does not do

- Plot API.
- Packing the IEEE OUI registry.
- Default-route write.
- Scheduler install.
- Packet capture.
- A port sweep or a caller-supplied port list.
- An HTTP client.
- Process spawn of `ping`, `tracert`, `pathping`, `arp`, or `route`.

## Next action

PR10-01. Bound ICMP first, so PMTU, pathping samples, and campaigns that persist bind send on the NIC the caller named.
