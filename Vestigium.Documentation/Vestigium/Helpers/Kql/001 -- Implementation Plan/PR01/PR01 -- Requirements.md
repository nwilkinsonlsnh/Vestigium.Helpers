# Vestigium.Helpers.Kql — PR01 Requirements

**Document ID:** VEST-HLP-KQL-SRS-PR01  
**Version:** PR01  
**Status:** Locked for review. Not Accepted. No library code in this commit. Do not publish.  
**Date:** 3 October 2026  
**Package:** `Vestigium.Helpers.Kql`  
**Host that caused this:** `Vestigium.Suite.Network.RouteIQ`  
**Binding:** This file wins for PR01 on conflict with chat. `Requirements_v1.0.md` still wins for the shipped process/service dialect. PR01 adds namespaces, constructors, and operators. It does not replace v1.0.

If implementation and this file disagree, this file wins after Acceptance.

---

## 0. Purpose

One filter box per RouteIQ tab. The tab session binds one namespace. The user types the namespace column as the field name.

This revision filters three grids already loaded in RouteIQ: Routes, Neighbors, Connections. It does not query the machine. It does not join tabs.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | No code in the doc commit | Requirements, design, and plan only. |
| 2 | No NuGet publish | `Vestigium.Helpers.Kql` 1.0.0 stays the process/service catalog until a host consumes the new namespaces. RouteIQ takes a project reference until that publish. |
| 3 | Structure stays | Lexer, parser, binder, evaluator, catalog. No second language. No pipe. |
| 4 | Namespace is the field | Canonical is `route.destination`, not `CONN.Status` and not a hidden prefix. Lookup is case-insensitive. |
| 5 | One session, one namespace | Connections binds `connections.*` only. `route.destination` on that bar is an unknown field. |
| 6 | Bare suffix | Accepted when unique in that session. `destination` on the Routes bar binds `route.destination`. Document and suggest the namespace form. |
| 7 | Third segment is not a field | Fields are exactly `namespace.field`. A third segment is a closed value. |
| 8 | Shared words are not shared fields | `route.interfacename` and `neighbors.interfacename` are different fields. Same for `interfaceindex`, `protocol`, `state`. |
| 9 | Protocol split | `route.protocol` is `netmgmt`, `local`. `connections.protocol` is `tcp`, `udp`. |
| 10 | State split | `neighbors.state` is `invalid`, `static`, `reachable`. `connections.state` is `listen`, `established`, `timewait`. |
| 11 | Sheet typos are not canonical | `connections.remoteport`, not `remoeport`. `neighbors.rtt`, not `neighbors.rtt(ms)`. |
| 12 | Typed RHS | `ipaddress(...)`, `macaddress(...)`, `string(...)`, closed values, `true`, `false`, numbers. |
| 13 | No cross-tab join | One expression does not read two grids. |
| 14 | No CIDR | Octet-boundary prefix is the IP rule. A subnet type is out. |
| 15 | Filter is a view | Watch mutates the source collection. Empty text shows all. Parse or compile error keeps the last good rows and surfaces line/column. |
| 16 | Host maps rows | Kql does not call `GetSnapshot` and does not reference Suite.Network. |
| 17 | Logging | Field names, line, column, operator. Never RHS text, never row values. |
| 18 | Out of this revision | LmHosts, NetBios, Settings, chips-as-grammar, visual builder. |

---

## 2. Namespaces and fields

Stored types stay the existing Kql types plus two compare-time types. The row still holds a string for an address. `ipaddress` and `macaddress` are constructors on the right-hand side, not new stored field types.

### route

| Canonical | Grid column | Compare as | Closed values |
|---|---|---|---|
| `route.destination` | Destination | ipaddress | |
| `route.prefixlength` | PrefixLength | int | |
| `route.subnetmask` | SubnetMask | ipaddress | |
| `route.gateway` | Gateway | ipaddress | |
| `route.interfacename` | InterfaceName | string | |
| `route.interfaceindex` | InterfaceIndex | int | |
| `route.metric` | Metric | int | |
| `route.protocol` | Protocol | string | `netmgmt`, `local` |

### neighbors

| Canonical | Grid column | Compare as | Closed values |
|---|---|---|---|
| `neighbors.address` | Address | ipaddress | |
| `neighbors.class` | Class | string | `a`, `b`, `c`, `d`, `e` |
| `neighbors.macaddress` | MacAddress | macaddress | |
| `neighbors.interfacename` | InterfaceName | string | |
| `neighbors.state` | State | string | `invalid`, `static`, `reachable` |
| `neighbors.ismulticast` | IsMulticast | bool | |
| `neighbors.isbroadcast` | IsBroadcast | bool | |
| `neighbors.vendor` | Vendor | string | |
| `neighbors.rtt` | RTT (ms) | int | |
| `neighbors.isrouter` | IsRouter | bool | |
| `neighbors.interfaceindex` | InterfaceIndex | int | |

InterfaceName is one Neighbors field. The sheet listed it twice. That duplicate is not a second field.

### connections

| Canonical | Grid column | Compare as | Closed values |
|---|---|---|---|
| `connections.status` | Status | string | `open`, `added`, `dropped`, `reopened` |
| `connections.local` | Local | ipaddress | |
| `connections.localport` | LocalPort | int | range 1–65535 is a value rule, not a closed set |
| `connections.remote` | Remote | ipaddress | |
| `connections.remoteport` | RemotePort | int | range 1–65535 |
| `connections.process` | Process | string | |
| `connections.time` | Time | int | seconds |
| `connections.protocol` | Protocol | string | `tcp`, `udp` |
| `connections.state` | State | string | `listen`, `established`, `timewait` |

`netmgmt` and `local` are not connections protocol values. `invalid`, `static`, and `reachable` are not connections state values. The sheet mixed those cells. This table is the split.

---

## 3. Operators

Already shipped, still required: `==` `!=` `<>` `<` `>` `<=` `>=` `GT` `LT` `GE` `LE` `LIKE` `!LIKE` `IN` `BETWEEN` `AND` `OR` `NOT` `&&` `||`.

PR01 adds:

| Token | Means |
|---|---|
| `GTE` | alias of `GE` / `>=` |
| `LTE` | alias of `LE` / `<=` |
| `BEGINS WITH` | string prefix, unless RHS is `ipaddress` or `macaddress` |
| `ENDS WITH` | string suffix, same exception |
| `CONTAINS` | string substring, same exception |
| `STARTSWITH` | single-word alias of `BEGINS WITH` |
| `ENDSWITH` | single-word alias of `ENDS WITH` |

`BEGINS WITH` / `ENDS WITH` / `CONTAINS` on a plain string or `string(...)` are `LIKE` sugar: prefix is `LIKE 'x%'`, suffix is `LIKE '%x'`, substring is `LIKE '%x%'`. Wildcards in the argument are literal when the operator is one of these three. `LIKE` keeps `*` `%` `?`.

Space before a constructor parenthesis is ignored.

---

## 4. Right-hand side

### 4.1 Closed value

Two spellings, same value, compile error if the tail is not in that field's closed set:

```text
route.protocol == route.protocol.netmgmt
route.protocol == route.protocol(netmgmt)
neighbors.class == neighbors.class.a
neighbors.class == neighbors.class(a)
```

The dotted form has three segments. The call form is `namespace.field(token)`. Token is an ident or a number. Case-insensitive.

A closed value is not a field. It cannot appear on the left.

### 4.2 string(...)

Explicit string. Does not check the closed set. This is the open form.

```text
route.protocol == string(netmgmt)
route.protocol CONTAINS string(mgm)
```

`CONTAINS string(mgm)` is a substring match against the stored text. It is not a closed-value test. `string('netmgmt')` and `string(netmgmt)` are the same. Inside the call, an ident is taken as text, not as a field.

### 4.3 ipaddress(...)

Normalizes a dotted prefix. Comparison is octet-based, not raw text.

| Operator | Rule |
|---|---|
| `==` | Full quad required. `ipaddress(172.16.0.15)` equals that address. `ipaddress(172)` is a compile error on `==`. |
| `BEGINS WITH` | Leading octets. `ipaddress(172)` matches `172.16.0.15`. It does not match `1720.1.1.1`. |
| `ENDS WITH` | Trailing octets. `ipaddress(0.15)` matches `172.16.0.15`. |
| `CONTAINS` | Consecutive octets. `ipaddress(16.0)` matches `172.16.0.15` and `10.16.0.5`. |

`ipaddress(16.0)` is not a valid address. It is a legal fragment for `BEGINS WITH`, `ENDS WITH`, and `CONTAINS`.

### 4.4 macaddress(...)

Strips `:`, `-`, and spaces. Compares hex. These three are equal:

```text
neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)
neighbors.macaddress == macaddress(00e04c0f31b4)
```

`==` requires 12 hex digits after normalize. `CONTAINS` / `BEGINS WITH` / `ENDS WITH` require an even number of hex digits, at least 2. `macaddress(4c:0f)`, `macaddress(4c-0f)`, and `macaddress(4c0f)` are the same two bytes. A one-byte fragment is allowed and matches many NICs. Odd length is a compile error.

The row mapper may store any of the three separator forms. The constructor normalizes both sides at compare time. The host does not have to pre-normalize for `==` to hit.

### 4.5 Bool and number

```text
neighbors.isrouter == true
neighbors.isrouter == false
connections.localport LTE 1000
neighbors.rtt >= 50
connections.localport BETWEEN 1 AND 1023
```

`true` and `false` are the existing keywords. `LTE` is `<=`. Port range outside 1–65535 on `==` to a port field is a compile error. `BETWEEN` uses the same range check on a port field.

---

## 5. Examples this revision must accept

```text
route.destination == ipaddress(172.16.0.15)
route.destination BEGINS WITH ipaddress(172)
route.destination CONTAINS ipaddress(16.0)
route.gateway == ipaddress(172.16.0.1) && route.prefixlength >= 16
route.protocol == netmgmt
route.protocol == route.protocol.netmgmt
route.protocol == route.protocol.local
route.protocol == route.protocol(local)
route.protocol == string(netmgmt)
route.protocol CONTAINS string(mgm)
route.interfacename CONTAINS 'ethernet'

neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)
neighbors.macaddress == macaddress(00e04c0f31b4)
neighbors.macaddress CONTAINS macaddress(4c:0f)
neighbors.macaddress CONTAINS macaddress(4c-0f)
neighbors.macaddress CONTAINS macaddress(4c0f)
neighbors.class == a
neighbors.class == neighbors.class(a)
neighbors.class == neighbors.class.a
neighbors.isrouter == true
neighbors.isrouter == false
neighbors.rtt >= 50
neighbors.interfacename == 'Ethernet'

connections.protocol == tcp && connections.state == established
connections.protocol == connections.protocol.tcp
connections.protocol == connections.protocol(udp)
connections.localport == 443
connections.localport LTE 1000
connections.remote BEGINS WITH ipaddress(10.1)
connections.process CONTAINS 'chrome'
connections.process CONTAINS string(chrome)
connections.status == added
connections.status == connections.status.added
connections.time BETWEEN 1 AND 30
```

Bare suffix on the owning tab is also required: `protocol == tcp` on Connections, `destination BEGINS WITH ipaddress(172)` on Routes.

---

## 6. Examples this revision must reject

```text
route.protocol == tcp
connections.protocol == netmgmt
connections.protocol == route.protocol.netmgmt
neighbors.state == established
connections.state == reachable
route.destination == ipaddress(172)
neighbors.macaddress == macaddress(4c:0f)
neighbors.macaddress CONTAINS macaddress(4c0)
route.destination == neighbors.address
connections.remoeport == 443
neighbors.rtt(ms) >= 50
```

`route.destination == neighbors.address` is a cross-namespace field reference. Out. Unknown field, not a join.

`connections.protocol == route.protocol.netmgmt` names a value from the wrong namespace. Compile error.

---

## 7. What this package does not do

- Enumerate routes, neighbors, or connections
- Join a route row to a neighbor row
- Publish a new package as part of accepting this document
- Grow a pipe, `summarize`, or `let`
- Treat `Protocol.Udp` as a category token

---

## 8. Acceptance gate

1. This file, the design, and the plan sit under `001 -- Implementation Plan/PR01/`.
2. Every example in §5 compiles against the owning namespace and evaluates as specified.
3. Every example in §6 fails compile, with line/column, and does not throw.
4. Process, Service, Thread, System, and Adapter packs still compile their existing queries.
5. Status moves to Accepted only after that. Publish is a later decision, not this gate.
