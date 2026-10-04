# Vestigium.Helpers.Kql — PR01 Requirements

**Document ID:** VEST-HLP-KQL-SRS-PR01  
**Version:** PR01  
**Status:** Accepted. Documents only. Implementation not started. Do not publish.  
**Date:** 4 October 2026  
**Package:** `Vestigium.Helpers.Kql`  
**Host that caused this:** `Vestigium.Suite.Network.RouteIQ`  
**Binding:** This file wins for PR01 on conflict with chat. `Requirements_v1.0.md` still wins for the shipped process/service dialect. PR01 adds namespaces, constructors, and operators. It does not replace v1.0.

If implementation and this file disagree, this file wins.

Step 1 closed the document gate. The compile gate in §8 is the exit for steps 2–6, not a reason to reopen this file.

---

## 0. Purpose

One filter box per RouteIQ tab. The tab session binds one namespace. The user types the namespace column as the field name.

This revision filters three grids already loaded in RouteIQ: Routes, Neighbors, Connections. It does not query the machine. It does not join tabs.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | Document gate is not the compile gate | This file is Accepted. Library code starts at step 2. Publish waits on a host. |
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
| 19 | Grouping | `AND` / `&&` and `OR` / `\|\|` combine comparisons. Parentheses group. `AND` binds tighter than `OR` unless parentheses say otherwise. `NOT` before a group. `!` is only `!LIKE`. |

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

Already shipped, still required: `==` `!=` `<>` `<` `>` `<=` `>=` `GT` `LT` `GE` `LE` `LIKE` `!LIKE` `IN` `BETWEEN` `AND` `OR` `NOT` `&&` `\|\|` `(` `)`.

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

### 3.1 Logical and, logical or, parentheses

Both spellings are required. Same operator.

| Word | Symbol |
|---|---|
| `AND` | `&&` |
| `OR` | `\|\|` |
| `NOT` | none. `!` is only `!LIKE` |

Precedence, already in the parser: `NOT`, then `AND`, then `OR`. Parentheses call the whole expression again, so a group of two comparisons joined by `OR` can itself be joined by `AND` to another group.

A bare `&` is a parse error. A bare `|` is the pipe error. Those are not aliases. The accepted spellings are `&&` and `\|\|`.

An empty octet is a compile error. `ipaddress(10.)` is rejected. The prefix form is `ipaddress(10)`.

Accepted:

```text
route.protocol == route.protocol.netmgmt && route.prefixlength >= 16
route.protocol == route.protocol.netmgmt AND route.prefixlength >= 16

(route.protocol == route.protocol.netmgmt || route.protocol == route.protocol.local) && route.prefixlength >= 16
(route.protocol == route.protocol.netmgmt OR route.protocol == route.protocol.local) AND route.prefixlength >= 16

(connections.protocol == tcp && connections.localport == 443) || (connections.protocol == udp && connections.localport == 53)
(connections.protocol == connections.protocol.tcp && connections.localport == 443) || (connections.protocol == connections.protocol.udp && connections.remoteport == 53)

(neighbors.state == reachable || neighbors.state == static) && neighbors.isrouter == true
neighbors.isrouter == false || (neighbors.class == a && neighbors.rtt LTE 50)

connections.status == added && (connections.remote BEGINS WITH ipaddress(10) || connections.remote BEGINS WITH ipaddress(172.16))
NOT (connections.state == listen) && connections.protocol == tcp
```

`A && B || C` is `(A && B) || C`. It is not `A && (B || C)`. Use parentheses when the second reading is the one you want.

Rejected, with the accepted spelling above each:

```text
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

The missing `)` is a parse failure. The grid keeps the last good predicate.

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

`ipaddress(16.0)` is not a valid address. It is a legal fragment for `BEGINS WITH`, `ENDS WITH`, and `CONTAINS`. A trailing dot is an empty octet and is rejected.

### 4.4 macaddress(...)

Strips `:`, `-`, and spaces. Compares hex. These three are equal:

```text
neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)
neighbors.macaddress == macaddress(00e04c0f31b4)
```

`==` requires 12 hex digits after normalize. `CONTAINS` / `BEGINS WITH` / `ENDS WITH` require an even number of hex digits, at least 2. `macaddress(4c:0f)`, `macaddress(4c-0f)`, and `macaddress(4c0f)` are the same two bytes. A one-byte fragment (`4c`) is allowed and matches many NICs. Odd length (`4c0`) is a compile error.

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
(route.protocol == route.protocol.netmgmt || route.protocol == route.protocol.local) && route.prefixlength >= 16
(route.protocol == route.protocol.netmgmt OR route.protocol == route.protocol.local) AND route.prefixlength >= 16

neighbors.address == ipaddress(172.16.0.15)
neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)
neighbors.macaddress == macaddress(00e04c0f31b4)
neighbors.macaddress CONTAINS macaddress(4c:0f)
neighbors.macaddress CONTAINS macaddress(4c-0f)
neighbors.macaddress CONTAINS macaddress(4c0f)
neighbors.class == a
neighbors.class == neighbors.class(a)
neighbors.class == neighbors.class.a
neighbors.state == reachable
neighbors.state == neighbors.state.reachable
neighbors.isrouter == true
neighbors.isrouter == false
neighbors.rtt >= 50
neighbors.interfacename == 'Ethernet'
(neighbors.state == reachable || neighbors.state == static) && neighbors.isrouter == true

connections.protocol == tcp
connections.protocol == udp
connections.protocol == connections.protocol.tcp
connections.protocol == connections.protocol(udp)
connections.state == established
connections.state == connections.state.established
connections.localport == 443
connections.localport LTE 1000
connections.remoteport == 443
connections.remote BEGINS WITH ipaddress(10.1)
connections.process CONTAINS 'chrome'
connections.process CONTAINS string(chrome)
connections.status == added
connections.status == connections.status.added
connections.time BETWEEN 1 AND 30
(connections.protocol == tcp && connections.localport == 443) || (connections.protocol == udp && connections.localport == 53)
connections.status == added && (connections.remote BEGINS WITH ipaddress(10) || connections.remote BEGINS WITH ipaddress(172.16))
NOT (connections.state == listen) && connections.protocol == tcp
```

Bare suffix on the owning tab is also required: `protocol == tcp` on Connections, `destination BEGINS WITH ipaddress(172)` on Routes.

The accept column in §6 is part of this set. A reject with no accept spelling above it is an incomplete requirement.

---

## 6. Rejected spellings, with the accepted spelling above each

Each pair is the same intent. The first line compiles. The second fails compile, with line/column, and does not throw.

Wrong closed set. `tcp` is not a route protocol.

```text
route.protocol == route.protocol.netmgmt
route.protocol == tcp
```

Wrong closed set. `netmgmt` is not a connection protocol.

```text
connections.protocol == connections.protocol.tcp
connections.protocol == netmgmt
```

Closed value from the wrong namespace.

```text
connections.protocol == connections.protocol.tcp
route.protocol == route.protocol.netmgmt
connections.protocol == route.protocol.netmgmt
```

Wrong state set. `established` is a connection state.

```text
neighbors.state == neighbors.state.reachable
connections.state == connections.state.established
neighbors.state == established
```

Wrong state set. `reachable` is a neighbor state.

```text
connections.state == connections.state.established
neighbors.state == neighbors.state.reachable
connections.state == reachable
```

`==` on an address requires a full quad. A prefix uses `BEGINS WITH`.

```text
route.destination == ipaddress(172.16.0.15)
route.destination BEGINS WITH ipaddress(172)
route.destination == ipaddress(172)
```

`==` on a MAC requires 12 hex digits. A fragment uses `CONTAINS`.

```text
neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)
neighbors.macaddress CONTAINS macaddress(4c:0f)
neighbors.macaddress == macaddress(4c:0f)
```

MAC fragment length must be even.

```text
neighbors.macaddress CONTAINS macaddress(4c0f)
neighbors.macaddress CONTAINS macaddress(4c0)
```

No cross-namespace compare. Same address is two queries, one per tab.

```text
route.destination == ipaddress(172.16.0.15)
neighbors.address == ipaddress(172.16.0.15)
route.destination == neighbors.address
```

Typo is not a field.

```text
connections.remoteport == 443
connections.remoeport == 443
```

Unit stays in the header. It is not part of the name.

```text
neighbors.rtt >= 50
neighbors.rtt(ms) >= 50
```

Bare `&` is not AND. Bare `|` is the pipe error, not OR. Missing `)` does not compile. Empty octet does not compile. `!` does not negate a group.

```text
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

---

## 7. What this package does not do

- Enumerate routes, neighbors, or connections
- Join a route row to a neighbor row
- Publish a new package as part of accepting this document
- Grow a pipe, `summarize`, or `let`
- Treat `Protocol.Udp` as a category token
- Treat a single `&` or a single `|` as a logical operator
- Treat `!` as `NOT` in front of a parenthesis

---

## 8. Compile gate

Document status is already Accepted. This gate is the exit for implementation.

1. Every accept line in §3.1, §5, and §6 compiles against the owning namespace and evaluates as specified.
2. Every reject line in §6 fails compile, with line/column, and does not throw.
3. `(A || B) && (C || D)` matches a row that satisfies one of A/B and one of C/D. It does not match a row that satisfies only one group.
4. Process, Service, Thread, System, and Adapter packs still compile their existing queries.
5. Publish is a later decision, not this gate.
