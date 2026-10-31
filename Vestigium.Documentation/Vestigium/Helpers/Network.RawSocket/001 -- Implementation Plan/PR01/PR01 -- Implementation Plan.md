# Vestigium.Helpers.Network.RawSocket — PR01 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-RAWSOCKET-PLAN-PR01  
**Status:** Open  
**Date:** 31 October 2026  
**Requirements:** [`PR01 -- Requirements.md`](PR01%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network.RawSocket PR01: <step>`.

One-sentence goal: stand up the elevated package and land a capped SYN scan.

Name is locked: `Vestigium.Helpers.Network.RawSocket`.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | RAW-01 | New project `src/Vestigium.Helpers.Network.RawSocket`. `net10.0`. No plot package. | Done |
| 2 | RAW-02 | Privilege check. Typed denial when admin / `CAP_NET_RAW` is missing. | Open |
| 3 | RAW-03 | `SynScan` job. Hosts supplied. `PortSet`. Cap 1024. Half-open. | Open |
| 4 | RAW-04 | Amend a short README. State that Network stays connect-scan. | Open |
| 5 | RAW-05 | `dotnet test` filter for the new project. | Owner |

## Slice notes

### RAW-01

Landed: `src/Vestigium.Helpers.Network.RawSocket`. `net10.0`. Version 1.0.0. Project reference to Network for `PortSet`. No plot package. In the solution under Library.

### RAW-02

Privilege check. Typed denial when admin / `CAP_NET_RAW` is missing.

### RAW-03

`SynScan` job. Hosts supplied. `PortSet`. Cap 1024. Half-open.

### RAW-04

Amend a short README. State that Network stays connect-scan.

### RAW-05

Owner runs the filter on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Add raw sockets to `Vestigium.Helpers.Network`.
- Expand a CIDR.
- Capture packets.
- Speak TLS or HTTP.
- Spawn a process.

## Next action

RAW-02. Privilege check and typed denial.
