# Vestigium.Helpers.Network — PR13 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR13  
**Status:** Open  
**Date:** 31 October 2026  
**Requirements:** [`PR13 -- Requirements.md`](PR13%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR13: <step>`.

One-sentence goal: make the 2.0.0 scanner usable for lab support without a second scanner.

PR13-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR13-01 | Progress total. `Sent` is expected probes. `Received` is finished. | Done |
| 2 | PR13-02 | `OpenOnly`. Drop closed, timed-out, and error hits. Default false. | Done |
| 3 | PR13-03 | `ReadBanner` on the scan. Pass through to `TcpConnect`. Off by default. | Done |
| 4 | PR13-04 | Well-known sets `windows`, `printers`, `databases`. | Done |
| 5 | PR13-05 | `HostsFromPrefix`. Capped host list from an existing prefix. Does not scan. | Open |
| 6 | PR13-06 | Pure CSV of a `PortScanResult`. No file write. | Open |
| 7 | PR13-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 76–83 land. | Open |
| 8 | PR13-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR13-01

Expected count is hosts × ports × transports, after discovery drops skipped hosts. Report `Sent` once the live set is known.

Landed: `Sent` is the expected probe count after discovery. `Received` is finished. Tests in `PortScanProgressTests`.

### PR13-02

Filter in `Finish`. Host rows are unchanged. Default false.

Landed: `PortScanOptions.OpenOnly` default false. When set, `Hits` keeps only open ports. Host rows still come from the full set. Tests in `PortScanOpenOnlyTests`.

### PR13-03

`PortScanOptions.ReadBanner` sets `TcpConnectOptions.ReadBanner`. Confirm stays independent.

Landed: `ReadBanner` default false. Passed through on the TCP probe. `Confirm` is a separate flag. Tests in `PortScanBannerTests`.

### PR13-04

`windows`: 135, 139, 445, 3389, 5985, 5986. `printers`: 515, 631, 9100. `databases`: 1433, 1521, 3306, 5432, 27017. Names stay under the cap.

Landed: `WellKnown` gains those three names. `common` and `catalog` stay. Tests in `PortSetLabTests`.

### PR13-05

Uses the existing prefix math. Returns usable hosts as strings. Cap 1024. Over the cap throws. No call to `ScanPorts`.

### PR13-06

Header plus one row per open port: host, port, transport, service, mss, banner. Banner is quoted. No file I/O.

### PR13-07

Amend in place. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay.

### PR13-08

Owner runs the filter on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Expand a CIDR inside `ScanPorts`.
- SYN or half-open scan.
- OS fingerprinting.
- SNMP walk or extra OIDs.
- TLS conversation.
- Plot API, packed IEEE OUI, default-route write, packet capture, or process spawn.

## Next action

PR13-05. `HostsFromPrefix`.
