# Vestigium.Helpers.FileIo — PR03 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR03  
**Version:** 1.4  
**Status:** Closed.  
**Date:** 19 September 2026  
**Priority:** P1  
**Depends on:** PR02 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

Engine contract tests live in `FileIoPR03Tests.cs`. Old `FileIoSessionTests` / `FileIoCoverageTests` stay `<Compile Remove>`.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR03.001** | P0 | Inventory. | **Done** |
| **PR03.002** | P0 | Extract `FileIoPR03Tests.cs`. | **Done** |
| **PR03.003** | P0 | Contract fixtures. | **Done** |
| **PR03.004** | P1 | Retry / InUse. | **Done** |
| **PR03.005** | P1 | `%TEMP%` only + `IndexRootOverride`. | **Done** 19 Sep 2026. Trees under `%TEMP%\VestigiumFileIoPR03`. Index fixture injects `%TEMP%\VestigiumFileIoPR03Index` and asserts `IndexRoot()` stays under TEMP. |
| **PR03.006** | P2 | `[Collection("Logger")]` for Initialize. | **Done** 19 Sep 2026. `FileIoPR03Tests` does not Initialize — no collection. `FileIoLoggingTests` and `FileIoPR02Tests` already use `[Collection("Logger")]`. |

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.4 | 19 Sep 2026 | PR03 closed. |
