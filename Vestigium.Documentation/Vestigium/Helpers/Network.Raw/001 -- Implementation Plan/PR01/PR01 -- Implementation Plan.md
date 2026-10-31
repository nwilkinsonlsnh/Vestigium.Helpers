# Vestigium.Helpers.Network.Raw — PR01 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-RAW-PLAN-PR01  
**Status:** Proposed  
**Date:** 31 October 2026  
**Requirements:** [`PR01 -- Requirements.md`](PR01%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network.Raw PR01: <step>`.

One-sentence goal: stand up the elevated package and land a capped SYN scan.

This paper is proposed. No project is created until the name is locked.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | RAW-01 | New project `src/Vestigium.Helpers.Network.Raw`. `net10.0`. No plot package. | Proposed |
| 2 | RAW-02 | Privilege check. Typed denial when admin / `CAP_NET_RAW` is missing. | Proposed |
| 3 | RAW-03 | `SynScan` job. Hosts supplied. `PortSet`. Cap 1024. Half-open. | Proposed |
| 4 | RAW-04 | Amend a short README. State that Network stays connect-scan. | Proposed |
| 5 | RAW-05 | `dotnet test` filter for the new project. | Owner |

## What this release does not do

- Add raw sockets to `Vestigium.Helpers.Network`.
- Expand a CIDR.
- Capture packets.
- Speak TLS or HTTP.
- Spawn a process.

## Next action

Lock the name. Then RAW-01.
