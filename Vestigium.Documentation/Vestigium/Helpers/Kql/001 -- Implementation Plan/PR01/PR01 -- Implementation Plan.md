# Vestigium.Helpers.Kql — PR01 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR01  
**Version:** PR01  
**Status:** Locked for review. Not started.  
**Date:** 3 October 2026  
**Binding:** `PR01 -- Requirements.md`, then `PR01 -- Design.md`.

No library code until this plan is Accepted. This commit is the plan.

---

## 0. Done for this step

Three documents under `Vestigium.Documentation/Vestigium/Helpers/Kql/001 -- Implementation Plan/PR01/`.

Not done: catalog, lexer, parser, tests, RouteIQ bar, NuGet.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Documents reviewed. Status can move to Accepted. |
| 2 | `KqlEnums` / `KqlCatalog` | Packs `Route`, `Neighbor`, `Connection`. Fields and closed values from the requirements tables. |
| 3 | Lexer / parser | `GTE` `LTE` `BEGINS WITH` `ENDS WITH` `CONTAINS` and the single-word aliases. Constructors `ipaddress` `macaddress` `string`. Closed call form. |
| 4 | Binder | Two-segment field. Third segment or call is a closed value. Wrong namespace is unknown field. Port range. `== ipaddress` requires four octets. `== macaddress` requires 12 hex digits. |
| 5 | Evaluator | Octet list match. Normalized MAC match. String operators lower to literal `LIKE`. |
| 6 | Tests | Every accept example in Requirements §5. Every reject example in §6. Existing process/service queries still compile. |
| 7 | RouteIQ | Project reference. One bar per tab. View filter. Not in this package. |
| 8 | Publish | After step 7 consumes the catalog. Not before. |

Steps 2–6 are this repo. Step 7 is `Vestigium.Suite.Network`. Step 8 waits on step 7.

---

## 2. Catalog notes

Canonical strings match the namespace column, lowercase.

Do not enter `connections.remoeport`. Do not enter `neighbors.rtt(ms)`. Do not copy `netmgmt` onto `connections.protocol`. Do not copy `established` onto `neighbors.state`.

Aliases: grid header, bare suffix. No `CONN.` alias.

---

## 3. Tests that define done

Accept:

```text
route.destination == ipaddress(172.16.0.15)
route.destination BEGINS WITH ipaddress(172)
route.destination CONTAINS ipaddress(16.0)
route.protocol == route.protocol.netmgmt
route.protocol == string(netmgmt)
route.protocol CONTAINS string(mgm)
neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)
neighbors.macaddress CONTAINS macaddress(4c0f)
neighbors.class == neighbors.class(a)
neighbors.isrouter == false
connections.localport LTE 1000
connections.protocol == connections.protocol(udp)
connections.status == added
```

Reject at compile, no throw:

```text
route.protocol == tcp
connections.protocol == netmgmt
route.destination == ipaddress(172)
neighbors.macaddress == macaddress(4c:0f)
neighbors.macaddress CONTAINS macaddress(4c0)
connections.remoeport == 443
neighbors.rtt(ms) >= 50
route.destination == neighbors.address
```

Octet trap: `CONTAINS ipaddress(16.0)` hits `172.16.0.15` and does not hit `172.160.1.1`.

MAC trap: colon, hyphen, and bare hex of the same 12 digits hit one row.

Regression: `PID == 0` still compiles on `KqlPack.Process`.

---

## 4. Out of this plan

LmHosts, NetBios, Settings, CIDR, cross-tab join, chips, a RouteIQ-local dialect, publishing 1.0.0 as part of the doc commit.

---

## 5. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 3 Oct 2026 | Plan written. Implementation not started. |
