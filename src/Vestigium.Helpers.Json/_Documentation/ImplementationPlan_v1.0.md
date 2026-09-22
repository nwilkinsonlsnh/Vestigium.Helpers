# Vestigium.Helpers.Json — Phase Implementation Plan

**Document ID:** VEST-HLP-JSON-PLAN-000  
**Version:** 1.0  
**Status:** Accepted.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Json`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins.

Working tree of record: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Json/`. Clone `Vestigium.Logging` as `../Vestigium.Logging`.

---

## 0. How build mode uses this file

1. Read SRS §2 locks before touching code.
2. Implement **one phase**. Close gate green before the next.
3. Commit form: `Json phase N: <short goal>`.
4. Do not invent APIs that are not in SRS §7.
5. Do not write `%ProgramData%\\Vestigium\\Logs\\` from Json. Do not UniqueName. Do not use Newtonsoft.
6. Do not grow `JsonHelper` past Identity + Probe until Phase 0 has flipped Status to Accepted.

---

## 1. Phases

| Phase | Goal | Close gate |
|---|---|---|
| **0 Paper** | Status Accepted, this plan on disk, §8 taxonomy registered, umbrella HLP-JSON row updated | Docs Accepted; `Json_subcategories_are_registered`; façade still Identity + Probe |
| **1 Pure pieces** | Options, path parser (Pointer + dotted), RFC 8259 read/write of strings, `ToJson`/`FromJson`, `Parse` | §10 tests for dialect + paths; no file session yet |
| **2 Session** | `JsonSession` for `.json`: Snapshot, Set, Diff (6902), Commit, Revert, Cancel | In-memory lifecycle tests; no Desktop |
| **3 Files** | Open/Create/Save/SaveAs, 64 KiB streams, atomic replace, collision Fail/Overwrite, export folder hook | File tests on injected temp root |
| **4 JSONL + demo** | OpenJsonl, Record, AppendRecord, full rewrite Save; shipped gallery tabs | JSONL tests; gallery no longer SkeletonWindow |
| **5 Harden** | Sparse HelperLog, no bodies, Probe `%TEMP%` only, Developers Guide matches engine, full §10 | Suite filter `FullyQualifiedName~Json` green |

---

## 2. Phase 0 edit list (when you say Accept)

- Flip SRS/Guide/Plan **Status: Accepted.**
- `HelperLog.Subcategories`: add `Document`, `Query`, `Snapshot`, `Diff`, `Commit`, `Save`, `Jsonl` (reuse `Session`, `Probe`, `Identity`, `Guard`).
- Register them in `CreateTaxonomy()`.
- Test `Json_subcategories_are_registered`.
- Umbrella `_Documentation/Requirements_v1.0.md` HLP-JSON row: SRS v1.0 accepted.
- Do not implement engine in that commit.

---

## 3. Out of this plan

JSON Schema, JSONPath, Merge Patch, mid-file JSONL splice, source-gen, FileIo verbs, Logging writer, Csv/Excel, Newtonsoft.

---

## 4. Commands (after Phase 0)

```
dotnet build src/Vestigium.Helpers.Json/Vestigium.Helpers.Json.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json
dotnet run --project src/Vestigium.Helpers.Json.Demo/Vestigium.Helpers.Json.Demo.csproj
```
