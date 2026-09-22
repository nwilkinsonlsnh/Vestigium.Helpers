# Vestigium.Helpers.Kql — Logging

**Document ID:** VEST-HLP-KQL-LOG-000  
**Date:** 10 September 2026  
**Engine:** sibling `Vestigium.Logging` via `HelperLog` only.

Kql never calls `VestigiumLogger.Initialize`. The gallery or test host does. Writes are no-ops until that happens.

## Identity

| | |
|---|---|
| Category | `Helpers` |
| APPID | `Kql` |
| Folder | `%ProgramData%\Vestigium\Logs\Kql\` when the Kql Demo is the host |

## Subcategories

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | existing suite |
| Query | Parse, Compile, wildcard-on-exact warning |
| Session | `Create` pack/group list (names only) |

## Exact-equals wildcard warning (locked)

`==`, `!=`, and `<>` never treat `*`, `%`, or `?` as wildcards. Those characters are literals.

If the right-hand string of an exact compare contains `*`, `%`, or `?`, Compile writes **one Warning per comparison** and still succeeds:

```text
APPID=Kql  CATEGORY=Helpers  SUBCATEGORY=Query  STATUS=Warning
exact compare treats wildcard chars as literals field=PROC.Name op=== chars=%
```

- Log the **canonical field name** and the op.
- Log which of `*` `%` `?` appeared (`chars=%*`).
- Do **not** log the RHS string (it may be `PROC.CommandLine`).
- Do **not** fail the compile. The predicate is exact match of the literal, including the `%`.
- LIKE / `!LIKE` do not emit this warning.

Example:

```text
Name == 'CCleaner%'     → no match for CCleaner64.exe; Warning as above
Name LIKE 'CCleaner%'   → match; no Warning
```

## Never log

Row values, command lines, passwords, full query text after Phase 0 (query text may appear in Debug enter lines as length only: `chars=41`).
