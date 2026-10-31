# Vestigium.Helpers.Network — PR13a Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR13A  
**Status:** Closed  
**Date:** 31 October 2026  
**Requirements:** [`PR13a -- Requirements.md`](PR13a%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR13a: <step>`.

One-sentence goal: add the flat open-port row. CSV becomes a writer over that list.

Shipped as package **2.1.0**.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR13A-01 | `PortScanOpenRow` and `PortScanCsv.Rows`. Pure. No I/O. | Done |
| 2 | PR13A-02 | `Write` uses `Rows`. Same CSV. Banner quoted. | Done |
| 3 | PR13A-03 | Amend SRS, Design, Guide, and the package README in place. Decisions 84–88 land. | Done |
| 4 | PR13A-04 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Done |

## Slice notes

### PR13A-04

Owner ran the filter on the Windows clone. All tests passing. Package **2.1.0** is listed.

## What this release does not do

- Write a file.
- Replace nested host rows.
- Add closed or timed-out rows.
- Expand a CIDR inside `ScanPorts`.
- SYN or half-open scan.

## Next action

Closed. Package 2.1.0.
