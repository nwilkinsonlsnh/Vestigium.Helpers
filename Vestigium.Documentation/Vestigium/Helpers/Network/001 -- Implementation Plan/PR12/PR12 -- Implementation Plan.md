# Vestigium.Helpers.Network — PR12 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR12  
**Status:** Closed  
**Date:** 31 October 2026  
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
| 2 | PR12-02 | Sweep job. Hosts the caller supplies. Ports from PR12-01. Concurrency cap, per-host delay, overall timeout. Progress per finished port. | Done |
| 3 | PR12-03 | Rollup. Per host: open TCP, open UDP, catalog name, MSS, banner. Optional inference from evidence the sweep already collected. | Done |
| 4 | PR12-04 | Optional host discovery. ICMP (or a short TCP probe) before the sweep. Off by default. Missed hosts are skipped and recorded. | Done |
| 5 | PR12-05 | Optional service confirmation. Short known probe after a successful TCP connect for a small set. Off by default. No TLS conversation. | Done |
| 6 | PR12-06 | Optional UDP payloads. Known payload for a small set of UDP ports. Empty payload remains the default. One datagram. | Done |
| 7 | PR12-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 65–75 land. Decision 74 (no SYN) is explicit. | Done |
| 8 | PR12-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Done |

## Slice notes

### PR12-01

Landed: `PortSet` (`MaxPorts` 1024), `FromList`, `FromRange`, `WellKnown("common" | "catalog")`. Doors: `NetworkHelper.PortsFromList`, `PortsFromRange`, `PortsWellKnown`. Tests in `PortSetTests`.

### PR12-02

Landed: `ScanPorts`, `PortScanOptions` (parallelism 16, max 64, connect timeout, overall timeout, per-host delay), `PortScanHit`, `PortScanResult`. TCP on by default. UDP off by default. Progress phase is `host:port/transport`. Tests in `PortScanTests`.

### PR12-03

Landed: `PortScanHostRow`, `PortScanOpenPort`, `PortScanRollup`. Catalog name on each hit. `PortScanOptions.Infer` default false. Inference uses open ports and banners only. Tests in `PortScanRollupTests`.

### PR12-04

Landed: `PortScanOptions.Discover` default false. `DiscoverPort` switches the probe to TCP (connected or refused counts as up). Misses land on `PortScanResult.Skipped` and are not scanned. Tests in `PortScanDiscoverTests`.

### PR12-05

Landed: `PortScanOptions.Confirm` default false. `TcpConnectOptions.ConfirmService`. `ServiceConfirm.ProbeFor` sends `GET / HTTP/1.0` on 80/8000/8008/8080/8081/8888 only. Reply lands in `Banner`. No TLS. Tests in `ServiceConfirmTests`.

### PR12-06

Landed: `PortScanOptions.KnownUdp` default false. `UdpProbeOptions.Payload`. `UdpPayloads.For` is a DNS query on 53 and an NTP client request on 123. Other ports stay empty. One datagram. Tests in `UdpPayloadTests`.

### PR12-07

Landed: decisions 65–75 in the SRS. Decision 41 amended only for `ScanPorts`. Decision 74 (no SYN) is explicit. Design, Guide, and package README amended in place.

### PR12-08

Owner ran the filter on the Windows clone. All tests passed. Package `Vestigium.Helpers.Network` **2.0.0** pushed to NuGet on 31 October 2026.

## What this release does not do

- Expand a CIDR or call the subnet calculator from the sweep.
- SYN or half-open scan.
- OS fingerprinting.
- SNMP walk, set, or OIDs other than the existing `sysDescr` door.
- Neighbor or MAC resolution from the sweep.
- Plot API, packed IEEE OUI, default-route write, packet capture, or process spawn.

## Closed

PR12 is closed. Package 2.0.0 is the line.
