# Vestigium.Helpers.Kql — PR01 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR01  
**Version:** PR01  
**Status:** Accepted. Step 2 done. Lexer not started.  
**Date:** 4 October 2026  
**Binding:** `PR01 -- Requirements.md`, then `PR01 -- Design.md`.

Publish still waits on a host.

---

## 0. Done

Step 1 closed 4 October 2026. Documents Accepted.

Step 2 closed 4 October 2026. `KqlPack.Route`, `KqlPack.Neighbor`, `KqlPack.Connection`. Groups `Route`, `Neighbor`, `Conn`. Canonical names are the namespace column. Closed values and compare-as sit on `KqlField`. Port fields carry 1–65535. No `remoeport`. No `rtt(ms)`. `netmgmt` is not on `connections.protocol`.

Not done: lexer, parser, binder, evaluator, tests, RouteIQ bar, NuGet.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Done. Documents Accepted. |
| 2 | `KqlEnums` / `KqlCatalog` | Done. Three packs, fields, closed values, compare-as, port bounds. |
| 3 | Lexer / parser | `GTE` `LTE` `BEGINS WITH` `ENDS WITH` `CONTAINS` and the single-word aliases. Constructors `ipaddress` `macaddress` `string`. Closed call form. Do not reimplement `AND` `OR` `&&` `\|\|` or parentheses. Those already parse. |
| 4 | Binder | Two-segment field. Third segment or call is a closed value. Wrong namespace is unknown field. Port range. `== ipaddress` requires four octets. `== macaddress` requires 12 hex digits. Empty octet fails. |
| 5 | Evaluator | Octet list match. Normalized MAC match. String operators lower to literal `LIKE`. Grouped `AND` / `OR` already evaluate. |
| 6 | Tests | Every accept line in Requirements §3.1, §5, and §6. Every reject line in §6. Existing process/service queries still compile. |
| 7 | RouteIQ | Project reference. One bar per tab. View filter. Not in this package. |
| 8 | Publish | After step 7 consumes the catalog. Not before. |

Steps 3–6 are this repo. Step 7 is `Vestigium.Suite.Network`. Step 8 waits on step 7.

---

## 2. Catalog notes

Canonical strings match the namespace column, lowercase. Session lookup already registers the bare suffix.

Do not enter `connections.remoeport`. Do not enter `neighbors.rtt(ms)`. Do not copy `netmgmt` onto `connections.protocol`. Do not copy `established` onto `neighbors.state`.

Aliases: grid header. No `CONN.` alias. `neighbors.rtt` aliases `RTT` because the header is not the suffix.

---

## 3. Tests that define done

Each reject is listed under the accept spelling that replaces it. Both sides are required. These tests are step 6. The catalog does not parse them yet.

Grouping is required. `AND` binds tighter than `OR`. Parentheses override. Word and symbol are the same operator. `NOT` negates a group. `!` does not.

```text
(route.protocol == route.protocol.netmgmt || route.protocol == route.protocol.local) && route.prefixlength >= 16
(route.protocol == route.protocol.netmgmt OR route.protocol == route.protocol.local) AND route.prefixlength >= 16
(connections.protocol == tcp && connections.localport == 443) || (connections.protocol == udp && connections.localport == 53)
(neighbors.state == reachable || neighbors.state == static) && neighbors.isrouter == true
connections.status == added && (connections.remote BEGINS WITH ipaddress(10) || connections.remote BEGINS WITH ipaddress(172.16))
NOT (connections.state == listen) && connections.protocol == tcp

route.protocol == netmgmt && route.prefixlength >= 16
route.protocol == netmgmt & route.prefixlength >= 16

route.protocol == netmgmt || route.protocol == local
route.protocol == netmgmt | route.protocol == local

(connections.protocol == tcp && connections.localport == 443)
(connections.protocol == tcp && connections.localport == 443

connections.remote BEGINS WITH ipaddress(10)
connections.remote BEGINS WITH ipaddress(10.)

NOT (connections.state == listen)
!(connections.state == listen)
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

LmHosts, NetBios, Settings, CIDR, cross-tab join, chips, a RouteIQ-local dialect, a single `&` or a single `|` as a logical operator, `!` as group NOT, publishing 1.0.0 as part of this step.

---

## 5. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 3 Oct 2026 | Plan written. Implementation not started. |
| PR01 | 3 Oct 2026 | Each reject paired with the accepted spelling. |
| PR01 | 3 Oct 2026 | Grouped AND/OR is a required test, not new parser work. |
| PR01 | 4 Oct 2026 | Step 1 Accepted. Empty octet and `!` group form added to the reject list. |
| PR01 | 4 Oct 2026 | Step 2. Catalog packs and closed values. |
