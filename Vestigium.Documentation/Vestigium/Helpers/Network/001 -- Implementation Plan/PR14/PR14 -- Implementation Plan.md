# Vestigium.Helpers.Network — PR14 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR14  
**Status:** Closed  
**Date:** 31 October 2026  
**Requirements:** [`PR14 -- Requirements.md`](PR14%20--%20Requirements.md)  
**Binding:** Requirements win. Commit: `Network PR14: <step>`.

One-sentence goal: fill the lab gaps and lock the banner-logging rule.

SYN parity is RawSocket PR02. This paper does not touch that package.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR14-01 | Stamp 2.1.0 in README, Guide, and Design. | Done |
| 2 | PR14-02 | Platform attributes. Clear CA1416 on Registry and macOS sites. | Done |
| 3 | PR14-03 | `ResolveMac` on the scan. Opt-in. Miss is null. | Done |
| 4 | PR14-04 | `HostsFromPrefix` returns IPv6 hosts. Cap 1024. Does not scan. | Done |
| 5 | PR14-05 | `ProbeTls`. Subject, issuer, expiry, protocol. No HTTP. | Done |
| 6 | PR14-06 | `PortScanJson.Write`. Pure. Banner is a field, not a log line. | Done |
| 7 | PR14-07 | Library never logs a banner. Document the host rule. | Done |
| 8 | PR14-08 | Amend SRS, Design, Guide, and the package README. Decisions 89–95 land. | Done |
| 9 | PR14-09 | Owner gate. Package 2.1.1 listed. | Done |

Shipped as `Vestigium.Helpers.Network` **2.1.1**.
