# Vestigium.Helpers.Network — PR04 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR04  
**Version:** 1.3  
**Status:** Closed. Option C landed 19 September 2026.  
**Package:** `Vestigium.Helpers.Network` only

---

## 0. Out of this package

Charts, Demo, portable CI TFM, HttpIQ, scheduler install, `net use`, packet capture, cousin CLIs.

Hosts plot `BandwidthAmount` / `PercentileBill` / `ShareCampaignResult` if they want a picture.

---

## 1. Route write — Option C (dated 19 September 2026)

| Family | Windows | Linux |
|---|---|---|
| IPv4 | IP Helper `CreateIpForwardEntry` + optional HKLM persistent | Netlink `RTM_NEWROUTE` / `RTM_DELROUTE` |
| IPv6 | `CreateIpForwardEntry2` | Netlink `AF_INET6` |
| `0.0.0.0/0` and `::/0` | `NetworkRouteDenied` — default route is not offered | same |
| Missing admin / `CAP_NET_ADMIN` | `NetworkRouteDenied` | `NetworkRouteDenied` |

No `ip`, `route`, `netsh`, `sudo`, or `setcap` from this DLL.

---

## 2. Work table

| ID | Item | Status |
|---|---|---|
| PR04.001 | No Charts reference | Closed |
| PR04.002 | Packed OUI snapshot | Closed |
| PR04.003 | Deny / print confirmation | Closed |
| PR04.004 | Linux IPv4 netlink (Option B) | Closed 19 Sep 2026 |
| PR04.005 | IPv6 mutate both OS (Option C) | Closed 19 Sep 2026 |
| PR04.006 | Tests + close | Closed |

---

## 3. Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_
```

`PR04_006_required_fixtures_exist` lists the named fixtures.

Publish order is unchanged: FileIo / Json / Analytics / Hashing before Network.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. |
| 1.1 | 19 Sep 2026 | Charts / Demo / CI out of Network. Option A default. |
| 1.2 | 19 Sep 2026 | 001 closed. |
| 1.3 | 19 Sep 2026 | Option C. 001–006 closed. |
