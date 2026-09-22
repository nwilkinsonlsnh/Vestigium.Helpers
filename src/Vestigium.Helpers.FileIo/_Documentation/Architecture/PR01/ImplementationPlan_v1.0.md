# Vestigium.Helpers.FileIo — Phase Implementation Plan

**Document ID:** VEST-HLP-FILEIO-PLAN-000  
**Version:** 1.0  
**Status:** Historical. Engine phases 0–5 shipped. Do not reopen.  
**Date:** 9 September 2026 (banner 19 September 2026)  
**Active plan:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

If this file and the SRS disagree, the SRS wins. Active packaging / Logging / test work is v1.2.

## Commands

From the repo root. There is no FileIo Demo project.

```
dotnet build src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~FileIo
```

Phase narrative (paper → UniqueName → Copy engine → Pause/Cancel/Audit → Move/Delete/Mirror → harden) is closed. Gallery / `dotnet run --project src/Vestigium.Helpers.FileIo.Demo` was never left in the slnx; do not add that command back.
