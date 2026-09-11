# Vestigium.Helpers.Kql — Phase Implementation Plan

**Document ID:** VEST-HLP-KQL-PLAN-000  
**Version:** 1.1  
**Status:** Active. Build mode follows this file.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Kql` (product name **KQL**)  
**TFM:** `net10.0`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Counters:** [`Counters_v1.0.md`](Counters_v1.0.md)  
**Logging:** [`Logging_v1.0.md`](Logging_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins after Acceptance. If this file and working code disagree, change the code.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Kql/`.

---

## 0. How build mode uses this file

1. Read SRS §1 locks, [`Counters_v1.0.md`](Counters_v1.0.md), and [`Logging_v1.0.md`](Logging_v1.0.md) before touching code.
2. Implement **one phase**. Do not start the next until that phase's **Close gate** is green.
3. Commit form: `Kql phase N: <short goal>`.
4. Do not invent APIs that are not in SRS §5. Names may move a token; shapes may not.
5. This package does **not** call `Process.GetProcesses`, SCM, PDH, or ETW.
6. This package does **not** parse pipes, `summarize`, `join`, `let`, or regex.
7. Logging is **HelperLog only** (Vestigium.Logging engine). APPID `Kql`. Never `VestigiumLogger.Initialize` from this library. Never log row values or RHS strings.
8. Do not grow `KqlHelper` past Identity + Probe until the catalog phase starts (Phase 1).

Progress is the table in §1. Flip a row to **Done** only when its Close gate is green on `main`.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper + scaffold** | APPID `Kql`, slnx, Identity/Probe, Demo skeleton | **Done** |
| **1 Catalog + session** | Packs, groups, aliases, `KqlHelper.Create`, field lookup | Not started |
| **2 Parser** | Filter grammar, line/col errors | Not started |
| **3 Bind + 3VL** | Enabled-field bind, true/false/unknown evaluate | Not started |
| **4 LIKE + exact warning** | `*` `%` `?` on LIKE only; `==` literals + HelperLog Warning | Not started |
| **5 Processes host** | `Search(query)`, Watch, Campaign accept a Kql string | Not started |
| **6 Demo** | Catalog explorer + query box | Not started |
| **7 Harden** | Guide matches engine, sparse log, full tests | Not started |

---

## 2. Locked decisions (do not debate in build mode)

| # | Lock |
|---|---|
| 1 | Project `Vestigium.Helpers.Kql`. Façade `KqlHelper`. APPID `Kql`. |
| 2 | TFM `net10.0`. Not Windows-only. |
| 3 | Filter dialect only. Not Azure Kusto compatible. |
| 4 | Two axes: entity pack + resource group. |
| 5 | Session enablement: a query cannot see a disabled group. |
| 6 | `CPU.PrivateBytes` aliases `MEM.PrivateBytes`. `RAM` aliases `MEM`. `CPU.IO.*` aliases `IO.*`. |
| 7 | Unknown field → compile error listing enabled fields. |
| 8 | Missing / Denied / Unsupported → unknown. Top-level unknown is not a hit. |
| 9 | Wildcards only on `LIKE` / `!LIKE`. |
| 10 | `==` `!=` `<>` treat `*` `%` `?` as **literals**. Compile still succeeds. |
| 11 | If an exact compare RHS contains `*` `%` `?`, write HelperLog **Warning** (APPID `Kql`, subcategory `Query`): `exact compare treats wildcard chars as literals field=PROC.Name op=== chars=%`. One warning per comparison. Do not log the RHS string. |
| 12 | Keywords and identifiers case-insensitive. String `==` is ordinal ignore case. |
| 13 | Packs are catalog presets, not data providers. |
| 14 | Parser stays `ident ( '.' ident )*`. |
| 15 | Core Helpers only. No project reference to Processes / Services / Network / Charts. |
| 16 | Processes consumes Kql in **Phase 5**. |
| 17 | NET on a Process row is not v1. |
| 18 | GPU clocks / power / temperature are not v1. |
| 19 | Paging Priority 0–7 are not v1 KQL names. |
| 20 | Tests inject nothing into ProgramData. |

---

## 3. Phase 0 — Done

Shipped: `KqlHelper` Identity/Probe, Demo skeleton, slnx, `HelperLog.AppIds.Kql`, tests.

---

## 4. Phase 1 — Catalog and session

Packs, groups, aliases, `Create`. Close gate in v1.0 plan (unchanged).

---

## 5. Phase 2 — Parser

Grammar unchanged. No wildcard semantics here.

---

## 6. Phase 3 — Bind and three-valued evaluate

Unchanged.

---

## 7. Phase 4 — LIKE matcher, exact literals, warning

**Ship**

- LIKE: `*` and `%` = any run, `?` = one character
- `==` / `!=` / `<>` do not honor wildcards
- Compile detects `*` `%` `?` in an exact-compare string and calls HelperLog.Warning as in lock 11
- `KqlFixtureRow` for tests

**Close gate**

- `Name LIKE 'CCleaner%'` matches `CCleaner64.exe`
- `Name == 'CCleaner%'` does **not** match `CCleaner64.exe` (looks for the literal name `CCleaner%`)
- `Name LIKE '%EDGE%'` matches `msedge`
- `Name LIKE 'ms?'` matches `ms1`, not `msedge`
- Compile of `Name == 'CCleaner%'` with a temp `LogDirectory` writes a HelperLog line containing `exact compare treats wildcard chars as literals` and `chars=%`
- That compile result is still Ok (warning, not Failed)

---

## 8–12

Phases 5–7, out-of-plan list, and commands stay as in v1.0.

```
dotnet build src/Vestigium.Helpers.Kql/Vestigium.Helpers.Kql.csproj
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Kql
dotnet run --project src/Vestigium.Helpers.Kql.Demo/Vestigium.Helpers.Kql.Demo.csproj
```
