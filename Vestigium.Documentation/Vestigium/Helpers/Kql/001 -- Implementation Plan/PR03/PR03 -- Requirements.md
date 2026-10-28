# Vestigium.Helpers.Kql — PR03 Requirements

**Document ID:** VEST-HLP-KQL-SRS-PR03  
**Version:** PR03  
**Status:** Draft. Documents only. Implementation not started. Do not publish.  
**Date:** 17 October 2026  
**Package:** `Vestigium.Helpers.Kql`  
**Host that caused this:** `Vestigium.Helpers.Kql.AppMgmt`  
**Binding:** This file wins for PR03 on conflict with chat. `PR01 -- Requirements.md` still wins for the query language. `PR02 -- Requirements.md` still wins for completion. PR03 adds a session built from an external field list. It does not change what compiles.

If implementation and this file disagree, this file wins.

---

## 0. Purpose

A domain package must be able to open a `KqlSession` from its own field list. Core KQL stays the dialect. It does not grow a pack for every host.

The buyer is `Vestigium.Helpers.Kql.AppMgmt`. That package owns the Package and Feature catalogs. It cannot bind today because `KqlField` is internal and `KqlHelper.Create` only accepts the built-in packs.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | One door, not a new pack | No `KqlPack.AppMgmt`. No AppMgmt fields in `KqlCatalog`. |
| 2 | Public field factory | `KqlField` stays constructed by a public factory. The constructor stays internal. |
| 3 | `Create(fields)` | A session from an external list. Packs and groups are empty. Lookup is canonical, bare suffix, and aliases. |
| 4 | Existing packs do not move | Process, Service, Thread, System, Adapter, Route, Neighbor, Connection still come from `KqlCatalog.For`. |
| 5 | Same compile | An external session compiles with the PR01 parser and the PR02 completer. No new operator. |
| 6 | Unknown is still unknown | A name not in the external list is an unknown field. It does not fall through to a built-in pack. |
| 7 | Logging | Field count and session kind. Never a field value, never the query text. |

---

## 2. Factory

The factory takes the same facts the internal constructor already stores.

| Argument | Required | Notes |
|---|---|---|
| Canonical | yes | The insert name. Case-insensitive lookup. |
| Type | yes | `KqlType`. |
| Aliases | no | Bare names the session also binds. |
| Closed | no | Legal tokens after an operator. |
| CompareAs | no | Default `Stored`. |
| Minimum, Maximum | no | Port-style bounds. Unused by AppMgmt in this revision. |

Group and pack are not arguments. An external field belongs to no pack.

---

## 3. Session

`KqlHelper.Create(IReadOnlyList<KqlField> fields)` returns a session whose `Fields` is that list.

| Call | Behavior |
|---|---|
| `TryGetField(canonical)` | Hit. |
| `TryGetField(suffix)` | Hit when the suffix is unique in the list. |
| `TryGetField(alias)` | Hit. |
| `TryGetField(other)` | Miss. Does not search `KqlCatalog`. |
| `Complete` | Same slots as PR02, against this list only. |
| `Compile` | Same binder. Unknown name fails the same way a missing pack field fails. |

An empty list is a legal session. Compile of any field name fails. Complete returns no field rows. It does not throw.

---

## 4. What this package does not do

- Add an AppMgmt, Package, or Feature pack
- Map a `PackageRow` or a `FeatureRow`
- Draw a query bar
- Read ProgramData
- Change PR01 operators or PR02 ranking
- Publish a package as part of accepting this document

---

## 5. Compile gate

1. `Create(fields)` with two fields binds both canonical names and their aliases.
2. A name outside the list is unknown. It is not resolved from Process or Route.
3. `PID == 0` still compiles on `KqlPack.Process`.
4. Complete on the external session offers only that session's fields.
5. An empty field list does not throw.
6. Publish is a later decision, not this gate.
