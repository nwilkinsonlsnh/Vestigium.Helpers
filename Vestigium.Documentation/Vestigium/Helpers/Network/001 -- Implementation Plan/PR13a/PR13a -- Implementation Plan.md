# Vestigium.Helpers.Network — PR13a Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR13A  
**Status:** Open  
**Date:** 31 October 2026  
**Requirements:** [`PR13a -- Requirements.md`](PR13a%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR13a: <step>`.

One-sentence goal: add the flat open-port row. CSV becomes a writer over that list.

PR13a-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR13A-01 | `PortScanOpenRow` and `PortScanCsv.Rows`. Pure. No I/O. | Open |
| 2 | PR13A-02 | `Write` uses `Rows`. Same CSV. Banner quoted. | Open |
| 3 | PR13A-03 | Amend SRS, Design, Guide, and the package README in place. Decisions 84–88 land. | Open |
| 4 | PR13A-04 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR13A-01

`PortScanOpenRow(Host, Port, Transport, Service, Mss, Banner, ElapsedMs)`. `Rows` walks `Hosts`, open TCP then open UDP.

### PR13A-02

`Write` calls `Rows` and formats the same six columns. No file I/O.

### PR13A-03

Amend in place. Decision 74 (no SYN) and Decision 58 (no CIDR inside the scan) stay.

### PR13A-04

Owner runs the filter on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Write a file.
- Replace nested host rows.
- Add closed or timed-out rows.
- Expand a CIDR inside `ScanPorts`.
- SYN or half-open scan.

## Next action

PR13A-01. Flat row and `Rows`.
