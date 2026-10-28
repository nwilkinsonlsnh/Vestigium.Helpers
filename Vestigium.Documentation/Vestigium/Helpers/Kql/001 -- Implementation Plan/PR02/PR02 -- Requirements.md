# Vestigium.Helpers.Kql — PR02 Requirements

**Document ID:** VEST-HLP-KQL-SRS-PR02  
**Version:** PR02  
**Status:** Accepted. Documents only. Implementation not started. Do not publish.  
**Date:** 4 October 2026  
**Package:** `Vestigium.Helpers.Kql`  
**Host that caused this:** `Vestigium.Suite.Network.RouteIQ`  
**Binding:** This file wins for PR02 on conflict with chat. `PR01 -- Requirements.md` still wins for the query language. PR02 adds completion. It does not change what compiles.

If implementation and this file disagree, this file wins.

---

## 0. Purpose

The RouteIQ query box offers a list of the next legal token at the caret. Tab accepts the highlighted row. Up and down move the highlight.

The list is a function of the session catalog and the grammar. It is not a second language and not a new package.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | Completion lives in `Vestigium.Helpers.Kql` | Not a new library. A second package would only call this one. |
| 2 | The popup is not in this package | RouteIQ draws the list and owns Tab, Up, Down, Escape. Kql returns rows and the span to replace. |
| 3 | No model | Ranking is prefix, then contains. No prediction service. |
| 4 | Caret, not the whole line | The token left of the caret picks the slot. Text to the right of the caret is not a candidate. |
| 5 | One session, one list | A Routes session does not offer `connections.protocol`. |
| 6 | Canonical is the insert | The row inserts `route.destination`, not the grid header. Bare suffix is a second row only when unique in that session. |
| 7 | Closed values are values | After an operator, `route.protocol.netmgmt` and `route.protocol(netmgmt)` are both offered. They are not fields. |
| 8 | Constructors are values | `ipaddress(`, `macaddress(`, `string(` are offered when the field compares that way. Inside the parenthesis the list is empty. |
| 9 | MRU is a hint | The host may pass saved query texts. A row that appears in them sorts above an equal prefix match. It does not beat a longer prefix. |
| 10 | Tab accepts, Enter does not | Enter still commits the query to the MRU. Tab accepts the highlighted completion. |
| 11 | Default is the first row | The list opens with row 0 highlighted. Up and down move that highlight. |
| 12 | Empty slot is an empty list | Inside a constructor, or when nothing matches, the call returns no rows. It does not throw. |
| 13 | Does not change compile | A completion insert must be text the PR01 parser already accepts. Completion does not add operators. |
| 14 | Logging | Slot name and caret. Never the query text, never a row value. |

---

## 2. Slots

The slot is decided by the token immediately left of the caret.

| Slot | When | Rows |
|---|---|---|
| Field | Start, or after `&&` `\|\|` `AND` `OR` `NOT` `(` | Canonical fields of this session. Bare suffix when unique. |
| Operator | After a field | Operators legal for that field's compare-as. |
| Value | After an operator | Closed values for that field, both spellings. Constructor openers the field accepts. `true` / `false` for bool. No number guess. |
| None | Inside `ipaddress(` `macaddress(` `string(` | Empty list. |
| Join | After a finished comparison | `&&` `\|\|` `AND` `OR` `)` |

Operator rows by compare-as:

| Compare-as | Operators |
|---|---|
| int | `==` `!=` `<` `>` `<=` `>=` `GT` `LT` `GTE` `LTE` `BETWEEN` |
| string | those, plus `CONTAINS` `BEGINS WITH` `ENDS WITH` `LIKE` |
| ipaddress, macaddress | `==` `!=` `CONTAINS` `BEGINS WITH` `ENDS WITH` |
| bool | `==` `!=` |

`IN` is not offered in this revision. It already parses. It is not on the RouteIQ bars.

---

## 3. Ranking

1. Slot match. A field is not offered in the operator slot.
2. Case-insensitive prefix of the partial token.
3. Case-insensitive contains, after every prefix hit.
4. Host MRU boost, only among equal prefix length. The host passes texts. The library does not read RouteIQ settings.

The first row after that sort is the default. Tab inserts that row unless Up or Down has moved the highlight. The highlight is host state. The library does not store it.

---

## 4. Replace span

Each row carries the start and length of the partial token to replace. Tab replaces `route.prot` with `route.protocol`. It does not rewrite the left side of the line.

A constructor row inserts the name and the opening parenthesis: `ipaddress(`. The caret lands after the parenthesis.

A closed dotted row inserts the full tail: `route.protocol.netmgmt`.

---

## 5. Keys

Host rules. Recorded so the library contract has a buyer.

| Key | List open | List closed |
|---|---|---|
| Tab | Accept the highlighted row. Do not move focus. | Move focus. |
| Up / Down | Move the highlight. Do not move the caret. | Caret movement. |
| Escape | Close the list. Leave the text. | No effect. |
| Enter | Commit the query to the MRU. Do not accept a suggestion. | Commit the query to the MRU. |

The chevron remains the saved-query list. This list is the grammar list, under the caret.

---

## 6. Examples

Routes session, caret at the end.

```text
route.prot|
```

Default row: `route.protocol`. Tab replaces the partial token.

```text
route.protocol |
```

Rows include `==` and `CONTAINS`. They do not include `ipaddress(`.

```text
route.protocol == |
```

Rows include `route.protocol.netmgmt`, `route.protocol(netmgmt)`, `route.protocol.local`, `route.protocol(local)`, `string(`.

```text
route.destination BEGINS WITH |
```

Default row: `ipaddress(`.

```text
route.destination BEGINS WITH ipaddress(172|
```

Empty list.

Neighbors session.

```text
neighbors.mac|
```

Default row: `neighbors.macaddress`.

Connections session.

```text
connections.protocol == |
```

Rows include `tcp` and `udp` closed forms. They do not include `netmgmt`.

---

## 7. What this package does not do

- Draw a popup
- Handle Tab, Up, Down, or Escape
- Read the RouteIQ MRU file
- Guess an octet or a MAC byte
- Offer a field from another pack
- Add an operator PR01 does not already parse
- Publish a package as part of accepting this document

---

## 8. Compile gate

Document status is already Accepted. This gate is the exit for implementation.

1. Every example in §6 returns the stated default row, or an empty list where stated.
2. A Connections session does not offer `route.destination` or `netmgmt`.
3. Inserting the default row at each example produces text PR01 compiles, except the open constructor, which is incomplete by design.
4. An empty list does not throw.
5. Process, Service, Thread, System, and Adapter sessions still complete their own fields. PR01 queries still compile.
6. Publish is a later decision, not this gate.
