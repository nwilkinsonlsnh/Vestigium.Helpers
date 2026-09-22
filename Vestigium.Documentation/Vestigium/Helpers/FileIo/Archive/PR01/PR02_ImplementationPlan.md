# Vestigium.Helpers.FileIo — PR02 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR02  
**Version:** 1.7  
**Status:** Closed.  
**Date:** 19 September 2026  
**Priority:** P0  
**Depends on:** PR01 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR02 finished Vestigium.Logging inside FileIo. Catalog block 12500–12610. Job lines carry `correlationId = JobId`. `HelperCompat.cs` is gone.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR02.001** | P0 | Constants 12535–12610 on `FileIoEvents`. | **Done** |
| **PR02.002** | P0 | Catalog + `fileio.json` twins. | **Done** |
| **PR02.003** | P0 | `Write` takes correlationId and properties. | **Done** |
| **PR02.004** | P0 | Named helpers. Job uses them. | **Done** |
| **PR02.005** | P0 | Delete `HelperCompat`. | **Done** |
| **PR02.006** | P1 | Job JSONL `correlationId` = `JobId`. | **Done** |
| **PR02.007** | P1 | Logging tests (`FileIoPR02Tests`). | **Done** |
| **PR02.008** | P2 | Developers Guide: `FileIoCatalog.Register(cfg)`. Strike HelperLog / HelperWpfHost. | **Done** 19 Sep 2026. Guide v1.3. |

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.7 | 19 Sep 2026 | PR02 closed. Guide v1.3. |
