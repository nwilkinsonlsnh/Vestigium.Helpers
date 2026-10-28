# Vestigium.Helpers.Kql — PR01 Design

**Document ID:** VEST-HLP-KQL-DSN-PR01  
**Version:** PR01  
**Status:** Accepted. Documents only. Implementation not started.  
**Date:** 4 October 2026  
**Binding:** `PR01 -- Requirements.md` wins on conflict.

This page records how PR01 fits the pipeline that already shipped. It does not add requirements.

---

## 1. Intent

RouteIQ tabs need a namespace catalog and a richer right-hand side. The pipeline does not change.

```text
text → Lexer → Parser → Binder(session fields) → Evaluator(row)
Create(pack) builds the session lookup: canonical, suffix, aliases, closed values.
```

Kql still does not walk the machine. RouteIQ maps a grid row onto `IKqlRow` and calls Compile.

---

## 2. Why the namespace is the canonical name

The first cut hid the prefix and used `CONN.Status`. The owner rejected that. The sheet's namespace column is what the user types: `route.destination`, `neighbors.macaddress`, `connections.localport`.

Fields are exactly two segments. The lexer already glues `ident '.' ident` into one token, so `route.destination` needs no new token kind. A third segment is a closed value, not a deeper field. That keeps `route.protocol.netmgmt` from becoming a catalog row.

Packs are the namespaces:

| Pack | Namespace | Session |
|---|---|---|
| `Route` | `route` | Routes tab |
| `Neighbor` | `neighbors` | Neighbors tab |
| `Connection` | `connections` | Connections tab |

A session is one pack. Wrong prefix is an unknown field. That is the no-join rule expressed in the binder. Bare suffix is the existing last-segment alias, already implemented for `PID` → `PROC.Pid`.

`InterfaceName` is not one shared field. Two packs each own `interfacename`. Same spelling, different session, no collision.

---

## 3. Catalog

Add the three packs to `KqlPack` and one group flag each (`Route`, `Neighbor`, `Conn`) so the existing pack/group filter still works. Canonical strings are the namespace column, lowercase. Aliases are the grid header and the bare suffix.

Closed values live on the field, not in the parser. `route.protocol` carries `netmgmt` and `local`. `connections.protocol` carries `tcp` and `udp`. The sheet mixed those cells. The catalog does not.

Compare-as is metadata on the field:

| Compare-as | Stored in the row | RHS that binds |
|---|---|---|
| string | string | quoted string, `string(...)`, closed value |
| int | integer | number |
| bool | bool | `true` / `false` |
| ipaddress | string | `ipaddress(...)` |
| macaddress | string | `macaddress(...)` |

Do not add `IpAddress` or `MacAddress` to `KqlType`. The row value stays a string. The constructor produces a compare key. A type mismatch (`route.destination == '172.16.0.15'` without the constructor, or `== ipaddress` against `route.metric`) is a compile diagnostic, same channel as today's `field=PROC.Pid type=Integer op=== rhs=String`.

`neighbors.rtt(ms)` is not a name. The parenthesis is a call. Header keeps the unit.

---

## 4. Lexer and parser

New keywords: `GTE`, `LTE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`. `BEGINS WITH` and `ENDS WITH` are two identifiers. The parser accepts the pair. A lone `BEGINS` or `WITH` is not an operator.

Constructor: `ident '(' argument ')'` where the ident is `ipaddress`, `macaddress`, or `string`. Whitespace between the name and `(` is already skipped.

`ipaddress` argument is a dotted token the current number reader cannot own. `172.16.0.15` is not a number. The constructor reader consumes hex, digits, dots, colons, and hyphens until `)`. It does not go through `ReadNumberOrTimeSpan`. A trailing dot is an empty octet and fails in the binder, not as a wildcard.

`string` argument is an ident, a quoted string, or a number. An ident inside `string(...)` is text. It is not looked up as a field.

Closed call `neighbors.class(a)` is the same production as a constructor, dispatched because the name is a field with a closed set rather than `string` / `ipaddress` / `macaddress`.

Closed dotted `route.protocol.netmgmt` is one ident token. Binder splits on the last dot only when the left part is a field and the right part is in its closed set. Otherwise unknown field.

Grouping is already parsed. `ParseOr` calls `ParseAnd`, `ParseAnd` calls `ParseNot`, parentheses in `ParsePrimary` call `ParseOr` again. `AND` and `&&` are one token kind. `OR` and `||` are one token kind. Do not add a second precedence. `NOT` before a parenthesis already works. `!` is only the `!LIKE` lex. It is not a group operator.

Pipe stays a lex error. A bare `&` stays a lex error.

---

## 5. Compare rules

String `BEGINS WITH` / `ENDS WITH` / `CONTAINS` lower to `LIKE` with escaped literals. The user argument is not a wildcard pattern.

`ipaddress` builds an octet list. Empty octet is a compile error. More than four octets is a compile error. `==` requires four. Prefix operators require one to four. Match is list equality for `==`, list-prefix for `BEGINS WITH`, list-suffix for `ENDS WITH`, contiguous sublist for `CONTAINS`. Text compare is not used, so `172.16.0.15` does not contain the characters `16.0` by accident in the wrong place; it contains the octets 16, 0.

`macaddress` strips `:`, `-`, and spaces, then requires hex. `==` requires 12 digits. Fragment operators require an even count, minimum 2. Compare is on the normalized hex string. Separator form on the row does not matter.

Port fields reject an `==` or `BETWEEN` bound outside 1–65535 at compile time. `LTE 1000` is in range and binds.

Missing, blank, and denied stay unknown. Top-level unknown is not a hit. Unchanged from v1.0.

---

## 6. Host shape

Not in this package. Recorded so the library contract has a buyer.

RouteIQ holds one `KqlSession` per tab, created with that pack. The bar compiles on debounce. Evaluate against the collection already bound. Watch writes the source. The bar filters a view. Empty text clears the predicate. A failed compile leaves the last good predicate and shows the diagnostic.

Row mapping is a host function: grid column to canonical name. Kql does not reference RouteIQ.

---

## 7. Rejected

| Alternative | Why out |
|---|---|
| `CONN.` prefix as the typed form | Owner rejected it. Namespace column is the name. |
| One shared `protocol` field | Route values and connection values are different sets. |
| `ipaddress` as a stored `KqlType` | Rows are strings. Constructor is compare-time. |
| CIDR | No buyer in the examples. Octet prefix covers `BEGINS WITH ipaddress(172)`. |
| Cross-namespace join | Different row kinds. A later host action can copy an address to another tab. |
| Publish 1.0.0 with this catalog | Freezes a package the host does not consume yet. |
| Chips as grammar | A chip may emit `connections.protocol == tcp`. The grammar does not grow a category axis. |
| Bare `&` or bare `|` | Pipe collision, and `&` is already a lex error. `&&` and `\|\|` are the symbols. |
| `!` as group NOT | Lexer only accepts `!LIKE`. Group negation is `NOT`. |

---

## 8. Failure worth a test

A bad keystroke must not blank the grid. Compile failure returns a result object. It does not throw. The host keeps the last good predicate.

`CONTAINS ipaddress(16.0)` must not become a character search. `16.0` inside `172.160.1.1` is not an octet hit. `172.16.0.15` is.

`macaddress(00:e0:4c:0f:31:b4)` and `macaddress(00e04c0f31b4)` must hit the same row.

`route.protocol == tcp` must fail compile, not return zero rows.

`(A || B) && (C || D)` must not match a row that satisfies only one group.

---

## 9. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 3 Oct 2026 | Namespace catalog, constructors, closed values, string operators. Docs only. |
| PR01 | 4 Oct 2026 | Step 1. Grouping recorded as already parsed. Empty octet and `!` group form rejected. Accepted. |
