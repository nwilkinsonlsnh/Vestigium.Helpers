# Vestigium.Helpers.Network.RawSocket — PR02 Implementation Plan

**Document ID:** VEST-HLP-RAWSOCKET-PLAN-PR02  
**Status:** Open  
**Date:** 31 October 2026  
**Requirements:** [`PR02 -- Requirements.md`](PR02%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `RawSocket PR02: <step>`.

One-sentence goal: progress, rows, a parallelism cap, and a bind pin on `SynScan`.

PR02-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | RAW02-01 | Progress. `Sent` expected. `Received` finished. | Done |
| 2 | RAW02-02 | `SynScanCsv.Rows` and `Write`. Open ports only. | Done |
| 3 | RAW02-03 | Parallelism. Default 8. Max 32. | Done |
| 4 | RAW02-04 | Bind pin. `InterfaceIndex` or `SourceAddress`. | Done |
| 5 | RAW02-05 | Document Windows raw TCP denial. No fallback. Amend README. | Open |
| 6 | RAW02-06 | `dotnet test` on the RawSocket filter. | Owner |

## Slice notes

### RAW02-04

Optional pin. Unset keeps today's local-address pick.

Landed: `InterfaceIndex` and `SourceAddress` on `SynScanOptions`. Same validation as Network. Index 0 is not rewritten to 1. Applied on the raw socket and on the local-address probe. Tests in `SynScanBindTests`.

### RAW02-05

README states the Windows limit. A denied raw TCP socket stays `Denied`.

### RAW02-06

Owner runs the filter on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Move SYN into `Vestigium.Helpers.Network`.
- Expand a CIDR.
- Capture packets.
- Add WinDivert or Npcap.
- Fall back to a connect scan.

## Next action

RAW02-05. Document the Windows denial.
