# Vestigium.Helpers.Network — PR04 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR04  
**Version:** 1.2  
**Status:** Open. PR04.001 closed.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network` only  
**Default route goal:** Option A — v1 write stays Windows IPv4. Linux / IPv6 mutate stay denied.

PR01–PR03 closed security, contract lock, and share campaigns. PR04 is leftover **Network package** work. Not a gallery. Not Charts. Not a scheduler. Not HttpIQ.

---

## 0. What this package is not

| Item | Why it is out |
|---|---|
| Charts / ScottPlot / WPF plots | Host wires numbers to Charts. Network returns `BandwidthAmount`, `PercentileBill`, `ShareCampaignResult`. |
| Demo / Gallery tabs | Skipped in PR03.007–009. |
| Portable test project / ubuntu workflow | Solution CI. Not Network's to fix. |
| HTTP reachability | HttpIQ. |
| Packet capture, cousin CLIs, cron/schtasks, `net use`, passwords | Never |

---

## 1. Route write — Option A unless this table is edited with a date

| Option | Meaning |
|---|---|
| **A. Stop (default)** | Windows IPv4 IP Helper + HKLM. Linux and IPv6 write → `NetworkRouteDenied`. |
| B / C | Netlink / IPv6 mutate. Out until written here with a date. |

---

## 2. Work table

| ID | Item | Status |
|---|---|---|
| PR04.001 | Docs lock: no Charts reference | **Closed** |
| PR04.002 | Optional packed OUI snapshot | Optional / open |
| PR04.003 | Option A deny confirmation | Open |
| PR04.004 | Linux IPv4 netlink | Out (Option B) |
| PR04.005 | IPv6 mutate | Out (Option C) |
| PR04.006 | Tests + close | Open |

---

## 3. PR04.001 result

- `Vestigium.Helpers.Network.csproj` references Json, Analytics, FileIo. Not Charts.
- Fixture `PR04_001_network_has_no_charts_reference`.
- Design + Developers Guide state: hosts plot `ShareCampaignResult` / P95 if they want a picture.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_001
```

---

## 4. Permanently out

Charts, Demo, portable CI TFM, HttpIQ, scheduler install, `net use`, packet capture, cousin CLIs.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. CI mixed into Network. |
| 1.1 | 19 Sep 2026 | Charts / Demo / CI removed from Network scope. |
| 1.2 | 19 Sep 2026 | 001 closed. |
