# Vestigium.Helpers.Kql — Phase Implementation Plan

**Document ID:** VEST-HLP-KQL-PLAN-000  
**Version:** 1.3  
**Status:** v1 phases complete.  
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

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper + scaffold** | APPID `Kql`, slnx, Identity/Probe, Demo skeleton | **Done** |
| **1 Catalog + session** | Packs, groups, aliases, `KqlHelper.Create`, field lookup | **Done** |
| **2 Parser** | Filter grammar, line/col errors | **Done** |
| **3 Bind + 3VL** | Enabled-field bind, true/false/unknown evaluate | **Done** |
| **4 LIKE + exact warning** | `*` `%` `?` on LIKE only; `==` literals + HelperLog Warning | **Done** |
| **5 Processes host** | `Search(query)`, Watch, Campaign accept a Kql string | **Done** |
| **6 Demo** | Catalog explorer + query box | **Done** |
| **7 Harden** | Guide matches engine, sparse log, full tests | **Done** |

Companion: `VestigiumStatus.Warning` on `Vestigium.Logging`.

---

## 2. Phase 7 — Done

- DevelopersGuide v1.1 matches `Parse` / `Compile` / packs / LIKE vs `==`
- `KqlHardenTests` covers project isolation, no-RHS-in-log, 3VL OR, LIKE type check, double quotes, guide sample
- `Vestigium.Helpers.Kql.csproj` references Helpers only
- Demo uses `HelperWpfHost.Start` only

```
dotnet test src/Vestigium.Helpers.Tests --filter "FullyQualifiedName~Kql|FullyQualifiedName~ProcessKql"
```
