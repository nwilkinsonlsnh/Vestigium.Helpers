# Vestigium.Helpers.FileIo — PR01 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR01  
**Version:** 1.6  
**Status:** Closed 19 September 2026  
**Date:** 19 September 2026  
**Priority:** P0  
**Depends on:** nothing  
**Blocks:** PR02 may start  
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
| **PR01.001** | P0 | In `src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj`, remove `<ProjectReference Include="..\Vestigium.Helpers.Analytics\Vestigium.Helpers.Analytics.csproj" />`. | **Done** 19 Sep 2026 |
| **PR01.002** | P0 | Add `<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />` to the same csproj. | **Done** 19 Sep 2026 |
| **PR01.003** | P0 | Keep the Hashing **project** reference. Hashing is not in scope. | **Done** 19 Sep 2026. Still `ProjectReference` to `Vestigium.Helpers.Hashing.csproj`. No Hashing NuGet in this wave. |
| **PR01.004** | P0 | Confirm restore comes from the feed, not `src/Vestigium.Helpers.Analytics`. If restore fails, stop and fix nuget.config / package source / push. Do not point FileIo back at the csproj. | **Done** 19 Sep 2026. See evidence below. |
| **PR01.005** | P2 | Optional, same PR if it stays one line: explicit `<PackageReference Include="Vestigium.Logging" Version="$(VestigiumLoggingVersion)" />` in FileIo so the csproj is readable without knowing about `Directory.Build.props`. Version **must** be `$(VestigiumLoggingVersion)` so it cannot drift from 1.7.1. | **Done** 19 Sep 2026. FileIo lists Logging explicitly. Version is `$(VestigiumLoggingVersion)` from props (1.7.1). Duplicate of the props import is intentional and must stay in lockstep. |
| **PR01.006** | P2 | Tests project may keep its Analytics project reference (it tests Analytics itself). FileIo tests compile through the FileIo project reference. Do not add a second Analytics package ref on Tests unless restore demands it. | **Done** 19 Sep 2026. Tests still project-references Analytics and FileIo. No `PackageReference` to `Vestigium.Helpers.Analytics` on Tests. Restore/build on `6a5716be` did not demand a second package. |
| **PR01.007** | P2 | One sentence in [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md) Sibling section: FileIo takes Analytics 1.0.1 as NuGet. Logging 1.7.1 arrives via `Directory.Build.props`. | **Done** 19 Sep 2026. Guide v1.2 Sibling section. |

## PR01.004 evidence

- Repo has **no** `nuget.config`. Restore uses nuget.org.
- FileIo csproj has `PackageReference` 1.0.1 only. No Analytics `ProjectReference`.
- nuget.org flat container lists `1.0.0` and `1.0.1`: `https://api.nuget.org/v3-flatcontainer/vestigium.helpers.analytics/index.json`
- nupkg `Vestigium.Helpers.Analytics.1.0.1.nupkg` downloads from nuget.org. Nuspec id/version = `Vestigium.Helpers.Analytics` / `1.0.1`. Contains `lib/net10.0/Vestigium.Helpers.Analytics.dll`. Depends on MathNet.Numerics 5.0.0 and Vestigium.Logging 1.7.1.
- GitHub Actions on `6a5716be` (the 001–002 commit): **Restore success**, **Build success**. Test step failed; that failure exists on earlier main runs and is not a restore miss. Run: https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/runs/35483041226

Do not point FileIo back at `src/Vestigium.Helpers.Analytics.csproj`.

## PR01.006 evidence

`src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj` still has:

```xml
<ProjectReference Include="..\Vestigium.Helpers.FileIo\Vestigium.Helpers.FileIo.csproj" />
<ProjectReference Include="..\Vestigium.Helpers.Analytics\Vestigium.Helpers.Analytics.csproj" />
```

No Analytics PackageReference on Tests. FileIo tests reach Analytics types through FileIo and through the Analytics project (Analytics tests live in the same assembly).

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

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Plan opened. |
| 1.1 | 19 Sep 2026 | PR01.001 and PR01.002 landed. Hashing project reference kept. |
| 1.2 | 19 Sep 2026 | PR01.003 closed. Hashing remains a project reference. |
| 1.3 | 19 Sep 2026 | PR01.004 closed. nuget.org 1.0.1 restore confirmed. CI restore+build green. |
| 1.4 | 19 Sep 2026 | PR01.005 closed. FileIo lists Vestigium.Logging at `$(VestigiumLoggingVersion)`. |
| 1.5 | 19 Sep 2026 | PR01.006 closed. Tests keep Analytics project reference. |
| 1.6 | 19 Sep 2026 | PR01.007 closed. Developers Guide Sibling sentence. PR01 Closed. |
