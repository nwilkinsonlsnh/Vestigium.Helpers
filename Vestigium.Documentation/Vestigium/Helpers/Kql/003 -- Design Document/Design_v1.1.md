# Vestigium.Helpers.Kql — Design

**Document ID:** VEST-HLP-KQL-DSN-000  
**Version:** 1.1  
**Status:** Locked companion to SRS v1.0 + v1.1 addendum (as built)  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records *why* Kql is shaped this way. It does not add requirements.

---

## 1. Intent

One filter dialect and one field catalog that hosts can share. Kql does not walk the machine. Processes / Services / Network bind rows and call Compile.

```
text → Lexer → Parser → Binder(session fields) → Evaluator(row)
Create(pack) builds the session lookup: canonical, suffix, aliases.
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| No process / service enumeration here | Keeps the dialect reusable. Dependency arrow is host → Kql. |
| Pack + group catalog | A session only binds what the host asked for. Unknown field names the enabled pack. |
| Parse vs Compile | Parse is syntax. Compile binds names. A parse-ok string can still fail compile. |
| Three-valued logic | Missing, denied, unsupported, blank string → unknown. `unknown && false` = false. Top-level unknown is not a hit. |
| No pipe | `A \| where B` is Kusto, not this dialect. |
| Never log RHS | Queries can contain names, paths, secrets. Log line/col and field names only. |
| Field catalog ≠ event catalog | `KqlCatalog` is fields. `KqlLoggingCatalog` is EVENTIDs. |
| Never `Initialize` | Folder follows the host APPID. |

---

## 3. Shape

| File | Role |
|---|---|
| `KqlHelper.cs` | Identity, Probe, Create, Parse, Compile |
| `KqlSession.cs` | Pack/group + case-insensitive field lookup |
| `KqlCatalog.cs` | Field definitions per pack |
| `KqlLexer.cs` / `KqlParser.cs` | Tokens and AST |
| `KqlBinder.cs` | Names → fields, type checks |
| `KqlEvaluator.cs` | Three-valued eval on `IKqlRow` |
| `KqlLike.cs` | LIKE wildcards |
| `KqlField` / `KqlValue` / `KqlRow` | Types |
| `KqlLoggingCatalog` / `KqlEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Session lookup

Canonical first, then last-segment suffix if unique, then aliases. Case-insensitive.

Examples: `PID` → `PROC.Pid`. `CPU.PrivateBytes` / `RAM.PrivateBytes` → `MEM.PrivateBytes`.

`Groups = None` means pack defaults (Process → Proc, Cpu, Mem, Io, Gpu).

---

## 5. Exception / result policy

Compile and Parse return result objects (`Ok` / `Error` / `Diagnostics`). They do not throw on a bad query. `ArgumentNullException` on a null session or options.

Type mismatch is a compile diagnostic:

```text
field=PROC.Pid type=Integer op=== rhs=String
```

---

## 6. Still out

Kusto pipe, joins, `summarize`, time charts, walking processes, logging RHS text.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.1 | (prior) | One-paragraph as-built note. |
| 1.1 | 21 Sep 2026 | Expanded to standalone Design. As-built pipeline unchanged. |
