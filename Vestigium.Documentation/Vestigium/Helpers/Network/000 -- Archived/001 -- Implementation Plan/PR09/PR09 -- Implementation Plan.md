# Vestigium.Helpers.Network — PR09 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR09  
**Status:** Open  
**Date:** 24 September 2026  
**Backlog:** [`PR09 -- Backlog.md`](PR09%20--%20Backlog.md)  
**Binding:** Requirements win. Commit: `Network PR09: <step>`.

One-sentence goal: ship the ten diagnostic jobs 1.0.1 does not have, as package **1.1.0**.

PR09-12 is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column. The ID is the backlog number.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR09-02 | Egress bind. Optional interface index and source address on echo, trace, DNS, and the pathping options that follow. Omit means the stack chooses. Reject a guessed index of `1` when the caller passed `0`. | Open |
| 2 | PR09-09 | Address-family pin on trace. Unset keeps today's resolve. Pathping takes the same pin. | Open |
| 3 | PR09-01 | `NetworkHelper.Pathping`. Phase 1 is the trace walk. Phase 2 samples every hop for a window. Each hop reports RTT, loss at the hop, and loss on the link to the next hop. | Open |
| 4 | PR09-07 | PTR on each trace and pathping hop. Miss leaves the name empty. The hop still counts. | Open |
| 5 | PR09-04 | TCP as a trace probe, with a destination port, beside ICMP and UDP. | Open |
| 6 | PR09-03 | `TcpConnect`. One host, one port. Result is connected, refused, or timed out, plus elapsed time. No port loop. | Open |
| 7 | PR09-05 | Counter sample on one adapter for a duration. Bytes in, bytes out, errors, discards. Returns samples. Does not call `BillP95` and does not plot. | Open |
| 8 | PR09-06 | Echo window duration. Persisted on the recipe. Count stays. Both set means stop at whichever comes first. A recipe with no duration opens as count-only. | Open |
| 9 | PR09-08 | PMTU walk. Largest don't-fragment echo that passes. | Open |
| 10 | PR09-10 | One-target neighbor probe. One address. MAC or none. `GetNeighbors` stays the full table. | Open |
| 11 | PR09-11 | Amend SRS, Design, Guide, and the package README. csproj and README version **1.1.0**. | Open |
| 12 | PR09-12 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR09-02

`IcmpEchoOptions`, `IcmpTraceOptions`, and `DnsLookupOptions` gain optional `InterfaceIndex` and `SourceAddress`. Pathping options, added in PR09-01, carry the same pair and pass them into the walk and the sample. `InterfaceIndex` of `0` or omitted does not become `1`. A caller value `>= 1` is used. Source address, when set, must belong to that interface or the job rejects.

### PR09-09

`IcmpTraceOptions` gains an optional address family. Unset is today's resolver. Set to IPv4 or IPv6, the walk uses only that family. Pathping reads the same field.

### PR09-01

`NetworkHelper.Pathping(target, options)` returns a job. Phase 1 reuses the trace walk, including bind, family, and the probe protocol in force after PR09-04. Phase 2 sends a fixed sample to each discovered hop. The result is not "trace with more probes." Each hop has sample count, loss at that hop, and loss on the link to the next hop. The link figure is the difference between this hop's loss and the next hop's loss, never a negative number reported as gain.

### PR09-07

Trace and pathping hop records gain an optional name. Fill it with one PTR lookup. Timeout or NXDOMAIN stores null. The hop status stays the probe status.

### PR09-04

`ProbeProtocol` gains `Tcp`. Trace options gain a destination port, used only for the TCP probe. ICMP and UDP keep their current defaults. A TCP probe that completes the handshake, or receives a reset, has reached that hop. No data is written past the handshake.

### PR09-03

`NetworkHelper.TcpConnect(host, port, options)` is one connect. Options are timeout, and the egress bind from PR09-02. The result status is connected, refused, or timed out, with elapsed milliseconds. The library does not loop ports and does not speak HTTP.

### PR09-05

One adapter, one duration. Read counters at the start and at the end. Return bytes in, bytes out, errors, and discards as a delta. Also return the per-interval samples when the caller set an interval. Those samples are numbers for the host to pass to `BillPercentile` if it wants a bill. This job does not bill and does not plot.

### PR09-06

`EchoWindow` gains an optional duration. The recipe writes `durationMs` only when the caller set one. Open of an old recipe leaves duration null and runs the count, as today. A window with both stops when the count is met or the duration elapses, whichever is first. A window with neither is a reject.

### PR09-08

A job that echoes the target with don't-fragment set, raising the payload until the path rejects the size, then returns the largest size that passed. It uses the echo door and the egress bind. It is not a trace.

### PR09-10

`NetworkHelper.ProbeNeighbor(address)` asks ARP or ND for that address only. Result is the MAC, or none. It does not dump the table and it does not scan the subnet.

### PR09-11

Amend `Requirements_v1.6.md`, `Design_v1.6.md`, `DevelopersGuide_v1.6.md`, and the package README in place. Do not open a second requirements file. csproj `<Version>` and the README consume line become `1.1.0`. The version rule says 1.0.1 does not contain these ten doors.

### PR09-12

Owner runs the filter on the clone. This agent does not mark the release closed.

## What this release does not do

- Plot API.
- Packing the IEEE OUI registry.
- Default-route write.
- Scheduler install.
- Packet capture.
- A port sweep.
- An HTTP client.
- Process spawn of `ping`, `tracert`, `pathping`, or `route`.

## Next action

PR09-02. Egress bind first, so the jobs after it take the same options instead of growing a second way to pick a NIC.
