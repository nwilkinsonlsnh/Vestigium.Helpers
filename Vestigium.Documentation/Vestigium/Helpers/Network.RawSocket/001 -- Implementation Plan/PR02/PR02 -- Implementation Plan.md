# Vestigium.Helpers.Network.RawSocket — PR02 Implementation Plan

**Document ID:** VEST-HLP-RAWSOCKET-PLAN-PR02  
**Status:** Closed  
**Date:** 31 October 2026  
**Requirements:** [`PR02 -- Requirements.md`](PR02%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `RawSocket PR02: <step>`.

One-sentence goal: progress, rows, a parallelism cap, and a bind pin on `SynScan`.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | RAW02-01 | Progress. `Sent` expected. `Received` finished. | Done |
| 2 | RAW02-02 | `SynScanCsv.Rows` and `Write`. Open ports only. | Done |
| 3 | RAW02-03 | Parallelism. Default 8. Max 32. | Done |
| 4 | RAW02-04 | Bind pin. `InterfaceIndex` or `SourceAddress`. | Done |
| 5 | RAW02-05 | Document Windows raw TCP denial. No fallback. Amend README. | Done |
| 6 | RAW02-06 | Owner gate. Package 1.1.1 listed. | Done |

Shipped as `Vestigium.Helpers.Network.RawSocket` **1.1.1**. README states the Windows denial. No connect-scan fallback.
