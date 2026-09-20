# Vestigium.Helpers.FileIo — PR01 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR01  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P0  
**Depends on:** nothing  
**Blocks:** PR02–PR06 may start only after this close gate  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR01 is packaging only. No EVENTID changes. No engine changes. No HelperCompat delete.

---

## Goal

FileIo consumes the published Analytics package instead of the sibling project.

```xml
<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />
```

Logging stays where it already is: `Directory.Build.props` → `Vestigium.Logging` 1.7.1 for every project in this repo. Do not remove that import.

## Why

A FileIo nupkg that project-references `Vestigium.Helpers.Analytics.csproj` cannot restore on a host that only has NuGet. Analytics 1.0.1 is the published contract FileIo already compiled against (`NumericSeries.FromDecimal`, `Confidence`, empty-series rules).

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR01.001** | P0 | In `src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj`, remove `<ProjectReference Include="..\Vestigium.Helpers.Analytics\Vestigium.Helpers.Analytics.csproj" />`. | Open |
| **PR01.002** | P0 | Add `<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />` to the same csproj. | Open |
| **PR01.003** | P0 | Keep the Hashing **project** reference. Hashing is not in scope. | Open |
| **PR01.004** | P0 | Confirm restore comes from the feed, not `src/Vestigium.Helpers.Analytics`. If restore fails, stop and fix nuget.config / package source / push. Do not point FileIo back at the csproj. | Open |
| **PR01.005** | P2 | Optional, same PR if it stays one line: explicit `<PackageReference Include="Vestigium.Logging" Version="$(VestigiumLoggingVersion)" />` in FileIo so the csproj is readable without knowing about `Directory.Build.props`. Version **must** be `$(VestigiumLoggingVersion)` so it cannot drift from 1.7.1. | Open |
| **PR01.006** | P2 | Tests project may keep its Analytics project reference (it tests Analytics itself). FileIo tests compile through the FileIo project reference. Do not add a second Analytics package ref on Tests unless restore demands it. | Open |
| **PR01.007** | P2 | One sentence in [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md) Sibling section: FileIo takes Analytics 1.0.1 as NuGet. Logging 1.7.1 arrives via `Directory.Build.props`. | Open |

## Do not

- Change `FileIoAnalytics.cs`, `FileIoJob.cs`, or event catalogs.
- Delete `src/Vestigium.Helpers.Analytics` from the solution. That project still ships Analytics.
- Switch Charts or Network off their Analytics project references in this PR.
- Bump FileIo `<Version>` in this PR (that is PR06).
- Vendor Analytics source.

## Files expected to change

| Path | Change |
|---|---|
| `src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj` | ProjectReference → PackageReference |
| `src/Vestigium.Helpers.FileIo/_Documentation/DevelopersGuide_v1.0.md` | Sibling sentence |
| `src/Vestigium.Helpers.FileIo/_Documentation/PR01_ImplementationPlan.md` | This file; mark steps Done at close |

## Close gate

```text
dotnet restore src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet list src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj package
dotnet build src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

`dotnet list ... package` must show:

- `Vestigium.Helpers.Analytics` **1.0.1**
- `Vestigium.Logging` **1.7.1** (from props, and from FileIo if PR01.005 landed)

Commit: `FileIo PR01: consume Analytics 1.0.1 NuGet`.

## Out of PR01

EVENTIDs, HelperCompat, session tests, dest index, Demo, Hashing NuGet, FileIo version bump.
