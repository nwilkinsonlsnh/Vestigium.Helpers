# Vestigium.Helpers.FileIo — PR06 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR06  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P2  
**Depends on:** PR05 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR06 is pack and the Hashing question. No engine features.

---

## Goal

FileIo is packable with a honest dependency graph and a bumped version.

## Baseline

- FileIo `<Version>` is **1.0.0**.
- After PR01 it PackageReferences Analytics **1.0.1**.
- Hashing is still `<ProjectReference Include="..\Vestigium.Helpers.Hashing\Vestigium.Helpers.Hashing.csproj" />` (Hashing itself is 1.3.0 in its csproj).
- Analytics already has `PackageReadmeFile`, `PackageProjectUrl`, `RepositoryUrl`. FileIo does not.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR06.001** | P2 | Add README.md for the nupkg (short: verbs, Audit Mode, Logging door, Analytics 1.0.1). Set `PackageReadmeFile`, `PackageProjectUrl`, `RepositoryUrl` like Analytics. | Open |
| **PR06.002** | P2 | Bump FileIo `<Version>` to **1.0.1** if only packaging changed after 1.0.0, or **1.1.0** if PR02 catalog ids shipped (they did, if this wave ran in order). Prefer **1.1.0** after PR02. | Open |
| **PR06.003** | P2 | Hashing: if `Vestigium.Helpers.Hashing` is on the same feed, replace the project reference with that PackageReference. If it is not published, **do not pack FileIo** for nuget.org — a packed FileIo would still drag a sibling csproj. | Open |
| **PR06.004** | P2 | `dotnet pack` FileIo. Inspect the nupkg: Analytics 1.0.1, Logging 1.7.1, `contentFiles/.../EventCatalog/fileio.json` present. | Open |
| **PR06.005** | P2 | Release notes sentence: consumes Analytics 1.0.1; Logging 1.7.1 via props; custom EVENTID 12500–12610; no robocopy.exe. | Open |

## Do not

- Publish a nupkg that still project-references Hashing.
- Change bucket edges or lead time.
- Add Charts as a FileIo dependency.

## Close gate

```text
dotnet pack src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj -c Release
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

Nuspec / nupkg dependencies: Analytics 1.0.1, Logging 1.7.1, Hashing only as a **package** version. Event catalog file inside the package.

Commit: `FileIo PR06: pack 1.1.0`.

## Wave done when

PR01–PR06 close gates are green. v1.2 plan status can flip to Closed. SRS §11 roadmap stays parked.
