# Vestigium.Helpers.Network — PR13 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR13  
**Status:** Closed  
**Date:** 31 October 2026  
**Requirements:** [`PR13 -- Requirements.md`](PR13%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR13: <step>`.

One-sentence goal: make the 2.0.0 scanner usable for lab support without a second scanner.

Shipped as package **2.1.0**.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR13-01 | Progress total. `Sent` is expected probes. `Received` is finished. | Done |
| 2 | PR13-02 | `OpenOnly`. Drop closed, timed-out, and error hits. Default false. | Done |
| 3 | PR13-03 | `ReadBanner` on the scan. Pass through to `TcpConnect`. Off by default. | Done |
| 4 | PR13-04 | Well-known sets `windows`, `printers`, `databases`. | Done |
| 5 | PR13-05 | `HostsFromPrefix`. Capped host list from an existing prefix. Does not scan. | Done |
| 6 | PR13-06 | Pure CSV of a `PortScanResult`. No file write. | Done |
| 7 | PR13-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 76–83 land. | Done |
| 8 | PR13-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Done |

## Slice notes

### PR13-08

Owner ran the filter on the Windows clone. All tests passing. Package **2.1.0** is listed.

## What this release does not do

- Expand a CIDR inside `ScanPorts`.
- SYN or half-open scan.
- OS fingerprinting.
- SNMP walk or extra OIDs.
- TLS conversation.
- Plot API, packed IEEE OUI, default-route write, packet capture, or process spawn.

## Next action

Closed. Package 2.1.0.
