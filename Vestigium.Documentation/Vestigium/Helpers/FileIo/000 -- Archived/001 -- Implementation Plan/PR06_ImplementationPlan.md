# Vestigium.Helpers.FileIo — PR06 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR06  
**Version:** 1.1  
**Status:** Closed with a publish hold.  
**Date:** 19 September 2026  
**Priority:** P2  
**Depends on:** PR05 closed  
**Package:** `Vestigium.Helpers.FileIo` 1.1.0  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR06.001** | P2 | Nupkg README + `PackageReadmeFile` / URL like Analytics. | **Done** 19 Sep 2026. `src/Vestigium.Helpers.FileIo/README.md`. |
| **PR06.002** | P2 | Version **1.1.0** (PR02 catalog shipped). | **Done** |
| **PR06.003** | P2 | Hashing on the same feed? | **Hold.** `Vestigium.Helpers.Hashing` is **404** on nuget.org. Project reference stays. Do **not** push FileIo 1.1.0 to nuget.org until Hashing is a PackageReference. |
| **PR06.004** | P2 | `dotnet pack` for nuget.org. | **Blocked** by 003. Local pack is allowed for inspection; do not `nuget push`. |
| **PR06.005** | P2 | Release notes. | **Done.** In the package README. Analytics 1.0.1; Logging 1.7.1; EVENTID 12500–12610; no robocopy.exe. |

## Publish gate (later)

When `Vestigium.Helpers.Hashing` exists on nuget.org at a pinned version:

1. Replace the Hashing `<ProjectReference>` with `<PackageReference Include="Vestigium.Helpers.Hashing" Version="…" />`.
2. `dotnet pack src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj -c Release`
3. Confirm nupkg has Analytics 1.0.1, Logging 1.7.1, Hashing as a **package**, and `contentFiles/.../EventCatalog/fileio.json`.
4. Then push.
