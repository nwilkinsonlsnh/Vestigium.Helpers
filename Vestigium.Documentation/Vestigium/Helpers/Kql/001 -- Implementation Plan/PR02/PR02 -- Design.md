# Vestigium.Helpers.Kql — PR02 Design

**Document ID:** VEST-HLP-KQL-DSN-PR02  
**Version:** PR02  
**Status:** Accepted. Documents only. Implementation not started.  
**Date:** 4 October 2026  
**Binding:** `PR02 -- Requirements.md` wins on conflict.

This page records how completion sits on the session that already shipped. It does not add requirements.

---

## 1. Intent

Completion is a read of the session. It does not parse a predicate and it does not evaluate a row.

```text
text + caret + session → slot → ranked rows + replace span
```

The pipeline from PR01 is unchanged. Compile still owns accept and reject.

---

## 2. Why it stays in this package

The session already holds canonical names, bare suffixes, closed values, and compare-as. A new library would take a dependency on those and add a package the host must version. The popup is the only piece that is not catalog. That piece is WPF and stays in RouteIQ.

A shared completion control is a later library, and only when a second host needs the same popup. TraceIQ does not.

---

## 3. Call

`KqlHelper.Complete(text, caret, session, hints)`.

`hints` is an optional list of saved query texts. Null means no MRU boost.

Result:

| Member | Meaning |
|---|---|
| `Slot` | Field, Operator, Value, Join, None |
| `Rows` | Ordered. Index 0 is the default. |
| `ReplaceStart` | Index of the partial token. |
| `ReplaceLength` | Length of the partial token. Zero when the caret is after a delimiter. |

A row:

| Member | Meaning |
|---|---|
| `Insert` | Text that replaces the span. |
| `Display` | Same as insert in this revision. |
| `Kind` | Field, Operator, Value, Constructor, Join |

Caret past the end clamps to the length. Negative caret returns an empty list. No throw.

---

## 4. Slot scan

Walk tokens left of the caret with the existing lexer. Do not invoke the binder.

- No token, or the previous token is a join or `(`: Field.
- Previous token is a field: Operator. The field is resolved with `TryGetField`. Unknown field is an empty operator list.
- Previous token is an operator: Value. Closed values come from that field. Constructor opener comes from compare-as.
- Caret inside a constructor argument: None.
- Previous token closes a comparison: Join.

A partial token at the caret is the filter, not a new slot. `route.prot` is Field, filtered by `route.prot`.

---

## 5. Host shape

Not in this package.

RouteIQ calls Complete on each text change, with the caret and that tab's MRU texts. The popup opens when `Rows` is not empty. Highlight starts at 0. Tab writes `Insert` over `[ReplaceStart, ReplaceLength)` and places the caret at the end of the insert. Escape closes. Enter does not read the list.

The chevron list is unchanged. It applies a whole saved query. It does not call Complete.

---

## 6. Rejected

| Alternative | Why out |
|---|---|
| New completion package | No fact that Kql does not already own. |
| Popup inside Kql | Kql does not reference WPF. |
| Model or frequency service | Closed grammar. MRU boost is the only history. |
| Enter accepts the row | Enter already commits the MRU. One key must not do both. |
| Guess octets or MAC bytes | Open text. Empty list. |
| Offer `IN` | Parses. Not on these bars. |
| Publish as part of this document | Host does not call Complete yet. |

---

## 7. Failure worth a test

A bad caret must not throw. Connections must not offer a route field. Inserting `route.protocol.netmgmt` after `route.protocol ==` must be text PR01 compiles.

`ipaddress(172` returns no rows. Tab is a host no-op when the list is empty.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| PR02 | 4 Oct 2026 | Completion in Kql. Popup in RouteIQ. Docs only. |
