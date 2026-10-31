# Vestigium.Helpers.Network — PR14 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR14  
**Status:** Open  
**Date:** 31 October 2026  
**Requirements:** [`PR14 -- Requirements.md`](PR14%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR14: <step>`.

One-sentence goal: fill the lab gaps and the RawSocket parity gaps, and lock the banner-logging rule.

PR14-N is the owner gate on the clone. Test names describe the behavior. They do not use the release id.

## Implementation table

Build order is the Order column.

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR14-01 | Stamp 2.1.0 in README, Guide, and Design. | Open |
| 2 | PR14-02 | Platform attributes. Clear CA1416 on Registry and macOS sites. | Open |
| 3 | PR14-03 | `ResolveMac` on the scan. Opt-in. Miss is null. | Open |
| 4 | PR14-04 | `HostsFromPrefix` returns IPv6 hosts. Cap 1024. Does not scan. | Open |
| 5 | PR14-05 | `ProbeTls`. Subject, issuer, expiry, protocol. No HTTP. | Open |
| 6 | PR14-06 | `PortScanJson.Write`. Pure. Banner is a field, not a log line. | Open |
| 7 | PR14-07 | Library never logs a banner. Document the host rule. | Open |
| 8 | PR14-08 | RawSocket progress. `Sent` expected. `Received` finished. | Open |
| 9 | PR14-09 | `SynScanCsv.Rows` and `Write`. Open ports only. | Open |
| 10 | PR14-10 | SYN parallelism. Default 8. Max 32. | Open |
| 11 | PR14-11 | SYN bind. `InterfaceIndex` or `SourceAddress`. | Open |
| 12 | PR14-12 | Document Windows raw TCP denial. No fallback. | Open |
| 13 | PR14-13 | Amend SRS, Design, Guide, and both READMEs. Decisions 89–100 land. | Open |
| 14 | PR14-14 | `dotnet test` on Network and RawSocket filters. | Owner |

## Slice notes

### PR14-01 through PR14-02

No new behavior. Stamp the version. Mark platform-specific calls.

### PR14-03 through PR14-07

Network doors. `ResolveMac` uses `ProbeNeighbor`. `ProbeTls` is one handshake. JSON is text. Banner never hits the audit log.

### PR14-08 through PR14-12

RawSocket parity. Progress, rows, a lower parallelism cap, a bind pin. Windows raw TCP stays a typed denial.

### PR14-13

Amend in place. Decision 74 (no SYN in Network) and Decision 58 (no CIDR inside the scan) stay.

### PR14-14

Owner runs the filters on the Windows clone. This agent does not mark the release closed.

## What this release does not do

- Put SYN in `Vestigium.Helpers.Network`.
- Expand a CIDR inside `ScanPorts`.
- Capture packets.
- Walk SNMP.
- Add WinDivert or Npcap.
- Fall back from SYN to a connect scan.

## Next action

PR14-01. Stamp 2.1.0.
