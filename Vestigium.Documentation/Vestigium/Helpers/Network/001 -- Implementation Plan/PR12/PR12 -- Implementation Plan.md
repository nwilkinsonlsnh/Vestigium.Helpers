# Vestigium.Helpers.Network — PR12 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR12  
**Status:** Open  
**Date:** 28 October 2026  
**Requirements:** [`PR12 -- Requirements.md`](PR12%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR12: <step>`.

One-sentence goal: add a bounded connect-scan job over hosts and ports the caller already chose, with concurrency, progress, and a rollup.

This version amends Decision 41 for that job only. It is not a subnet expander and not a SYN scanner.

PR12-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR12-01 | Port set types. Explicit list, begin–end range, and a named well-known set. Expand with a hard cap. Reject over the cap. No I/O. | Done |
| 2 | PR12-02 | Sweep job. Hosts the caller supplies. Ports from PR12-01. Concurrency cap, per-host delay, overall timeout. Progress per finished port. | Open |
| 3 | PR12-03 | Rollup. Per host: open TCP, open UDP, catalog name, MSS, banner. Optional inference from evidence the sweep already collected. | Open |
| 4 | PR12-04 | Optional host discovery. ICMP (or a short TCP probe) before the sweep. Off by default. Missed hosts are skipped and recorded. | Open |
| 5 | PR12-05 | Optional service confirmation. Short known probe after a successful TCP connect for a small set. Off by default. No TLS conversation. | Open |
| 6 | PR12-06 | Optional UDP payloads. Known payload for a small set of UDP ports. Empty payload remains the default. One datagram. | Open |
| 7 | PR12-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 65–75 land. Decision 74 (no SYN) is explicit. | Open |
| 8 | PR12-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR12-01

Pure. `PortSet` expands to a distinct, ordered list of ports. Sources: `FromList`, `FromRange(begin, end)`, `WellKnown`. Cap is a constant (propose 1024). Over the cap throws. No network call.

Landed: `PortSet` (`MaxPorts` 1024), `FromList`, `FromRange`, `WellKnown("common" | "catalog")`. Doors: `NetworkHelper.PortsFromList`, `PortsFromRange`, `PortsWellKnown`. Tests in `PortSetTests`.

### PR12-02

`NetworkHelper.ScanPorts(hosts, ports, options)`. Uses existing `TcpConnect` and `UdpProbe`. Does not reimplement connect. Parallelism default is small (propose 16). Overall timeout cancels the job. Progress phase names the host and port.

### PR12-03

`PortScanResult` holds host rows. Each row holds the ports that answered and the evidence collected. Inference is optional and uses the existing pure scorer. The sweep does not call `ProbeNeighbor` or `IcmpTrace`.

### PR12-04

Flag on the options. When set, run `IcmpEcho` (or one TCP probe) per host first. Hosts that miss are listed as skipped. When unset, every supplied host is scanned.

### PR12-05

Flag on the options. When set, after a successful TCP connect to a known port, send one short request (for example an HTTP request line on 80/8080) and read a capped reply. Default remains the PR11 banner read, which sends nothing.

### PR12-06

Flag on the options. When set, UDP probes for a small known set use a fixed payload (for example DNS on 53, NTP on 123). Other UDP ports stay empty. No second datagram.

### PR12-07

Amend `Requirements_v1.6.md`, `Design_v1.6.md`, `DevelopersGuide_v1.6.md`, and the package README in place. Add decisions 65–75. State that Decision 41 is amended only for `ScanPorts`. Leave SYN out.

### PR12-08

Owner runs the filter on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Expand a CIDR or call the subnet calculator from the sweep.
- SYN or half-open scan.
- OS fingerprinting.
- SNMP walk, set, or OIDs other than the existing `sysDescr` door.
- Neighbor or MAC resolution from the sweep.
- Plot API, packed IEEE OUI, default-route write, packet capture, or process spawn.

## Next action

PR12-02. Sweep job over hosts and a `PortSet`.
