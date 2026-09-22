# Vestigium.Helpers.Network — PR05 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR05  
**Version:** 1.1  
**Status:** Closed. Hygiene only. No new protocol surface.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`

PR01–PR04 shipped the library. PR05 caught docs and persist-key drift. Live Linux / Windows-admin route checks stay in §4. They are **not** a Network publish gate.

---

## 1. Work table

| ID | Item | Status |
|---|---|---|
| PR05.001 | Persist write uses `NetworkRouteKeys.PersistentRoutes` | Closed |
| PR05.002 | Requirements match shipped PR03/PR04 | Closed |
| PR05.003 | Design + Guide match 002 | Closed |
| PR05.004 | Status index | Closed |
| PR05.005 | Root README: no `Network.Demo` | Closed |
| PR05.006 | IPv6 write tests renamed cap-or-cleanup | Closed |
| PR05.007 | Tests + close | Closed |

---

## 2. Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~NetworkPR05
```

`PR05_007_required_fixtures_exist` lists 001 + 006. No default-route write. No Ubuntu job.

---

## 3. Parked until Ubuntu / admin Windows (not this close)

| Check | Pass |
|---|---|
| Linux, no `CAP_NET_ADMIN`, IPv4 + IPv6 Add | `NetworkRouteDenied`, message mentions `CAP_NET_ADMIN`, no `ip` process |
| Linux, with cap, `192.0.2.88/32` Add then Remove | Route appears then disappears. Never `0.0.0.0/0`. |
| Windows admin, IPv6 `2001:db8:1::/64` Add then Remove | `CreateIpForwardEntry2` succeeds or typed deny — no hang, no `netsh`. |

Record results here as a dated note when those boxes exist.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. |
| 1.1 | 19 Sep 2026 | 001–007 closed. Live Linux still parked. |
