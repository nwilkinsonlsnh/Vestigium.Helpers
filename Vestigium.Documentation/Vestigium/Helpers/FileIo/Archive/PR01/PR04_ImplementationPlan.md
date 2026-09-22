# Vestigium.Helpers.FileIo — PR04 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR04  
**Version:** 1.2  
**Status:** Closed.  
**Date:** 19 September 2026  
**Priority:** P1  
**Depends on:** PR03 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR04.001** | P0 | Persist dest jsonl. | **Done** |
| **PR04.002** | P0 | `CleanIndex` deletes that jsonl. | **Done** 19 Sep 2026. `FileIoPR04Tests` writes then `CleanIndex` removes. |
| **PR04.003** | P0 | Production `%ProgramData%\Vestigium\FileIo\Indexes\`. Tests use override. | **Done**. Fixture asserts `IndexRoot()` when override is null. |
| **PR04.004** | P1 | `FileIoMask.Matches`. Analyze uses it. | **Done**. `*` / `?`, case-insensitive, whole name. Analyze no longer `Contains(Trim('*'))`. Job `Masked` is the same regex rule. |
| **PR04.005** | P1 | Tests. | **Done**. Persist + CleanIndex + Audit writes no index + `*.tmp` vs `notatmp.txt` + Analyze count. |
| **PR04.006** | P2 | Do not encrypt the index. | **Done**. Plain jsonl. Digest hex only. |

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 19 Sep 2026 | PR04 closed. |
