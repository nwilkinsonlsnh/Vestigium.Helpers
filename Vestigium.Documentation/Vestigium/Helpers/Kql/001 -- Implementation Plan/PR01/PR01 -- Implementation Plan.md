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
| 3 | Lexer / parser | `GTE` `LTE` `BEGINS WITH` `ENDS WITH` `CONTAINS` and the single-word aliases. Constructors `ipaddress` `macaddress` `string`. Closed call form. Do not reimplement `AND` `OR` `&&` `\|\|` or parentheses. Those already parse. |
| 4 | Binder | Two-segment field. Third segment or call is a closed value. Wrong namespace is unknown field. Port range. `== ipaddress` requires four octets. `== macaddress` requires 12 hex digits. |
| 5 | Evaluator | Octet list match. Normalized MAC match. String operators lower to literal `LIKE`. Grouped `AND` / `OR` already evaluate. |
| 6 | Tests | Every accept line in Requirements §3.1, §5, and §6. Every reject line in §6. Existing process/service queries still compile. |
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

Each reject is listed under the accept spelling that replaces it. Both sides are required.

Grouping is required. `AND` binds tighter than `OR`. Parentheses override. Word and symbol are the same operator.

```text
(route.protocol == route.protocol.netmgmt || route.protocol == route.protocol.local) && route.prefixlength >= 16
(route.protocol == route.protocol.netmgmt OR route.protocol == route.protocol.local) AND route.prefixlength >= 16
(connections.protocol == tcp && connections.localport == 443) || (connections.protocol == udp && connections.localport == 53)
(neighbors.state == reachable || neighbors.state == static) && neighbors.isrouter == true
connections.status == added && (connections.remote BEGINS WITH ipaddress(10.) || connections.remote BEGINS WITH ipaddress(172.16))

route.protocol == netmgmt && route.prefixlength >= 16
route.protocol == netmgmt & route.prefixlength >= 16

route.protocol == netmgmt || route.protocol == local
route.protocol == netmgmt | route.protocol == local

(connections.protocol == tcp && connections.localport == 443)
(connections.protocol == tcp && connections.localport == 443
```

A row that satisfies only the first parenthesized group must not match `(A || B) && (C || D)`.

Field rejects, accept spelling above each:

```text
route.protocol == route.protocol.netmgmt
route.protocol == tcp

connections.protocol == connections.protocol.tcp
connections.protocol == netmgmt

connections.protocol == connections.protocol.tcp
route.protocol == route.protocol.netmgmt
connections.protocol == route.protocol.netmgmt

neighbors.state == neighbors.state.reachable
connections.state == connections.state.established
neighbors.state == established

connections.state == connections.state.established
neighbors.state == neighbors.state.reachable
connections.state == reachable

route.destination == ipaddress(172.16.0.15)
route.destination BEGINS WITH ipaddress(172)
route.destination == ipaddress(172)

neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress CONTAINS macaddress(4c:0f)
neighbors.macaddress == macaddress(4c:0f)

neighbors.macaddress CONTAINS macaddress(4c0f)
neighbors.macaddress CONTAINS macaddress(4c0)

route.destination == ipaddress(172.16.0.15)
neighbors.address == ipaddress(172.16.0.15)
route.destination == neighbors.address

connections.remoteport == 443
connections.remoeport == 443

neighbors.rtt >= 50
neighbors.rtt(ms) >= 50
```

Octet trap: `CONTAINS ipaddress(16.0)` hits `172.16.0.15` and does not hit `172.160.1.1`.

MAC trap: colon, hyphen, and bare hex of the same 12 digits hit one row.

Regression: `PID == 0` still compiles on `KqlPack.Process`. `(PID == 0 || Name LIKE '%edge%') && GPU.Usage GT 20` still compiles.

---

## 4. Out of this plan

LmHosts, NetBios, Settings, CIDR, cross-tab join, chips, a RouteIQ-local dialect, a single `&` or a single `|` as a logical operator, publishing 1.0.0 as part of the doc commit.

---

## 5. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 3 Oct 2026 | Plan written. Implementation not started. |
| PR01 | 3 Oct 2026 | Each reject paired with the accepted spelling. |
| PR01 | 3 Oct 2026 | Grouped AND/OR is a required test, not new parser work. |
