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
| 5 | PR13-05 | `HostsFromPrefix`. Capped host list from an existing prefix. Does not scan. | Done |
| 6 | PR13-06 | Pure CSV of a `PortScanResult`. No file write. | Done |
| 7 | PR13-07 | Amend SRS, Design, Guide, and the package README in place. Decisions 76–83 land. | Done |
| 8 | PR13-08 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR13-07

Amend in place. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay.

Landed: SRS keeps 1–75 and appends 76–83. Design and Guide keep prior passes and add PR13. Package README updated. Version stays 2.0.0 until the owner bumps it.

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

PR13-08. Owner runs `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network`.
