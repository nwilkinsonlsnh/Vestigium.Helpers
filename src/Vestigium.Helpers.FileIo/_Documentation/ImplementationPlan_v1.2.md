# Vestigium.Helpers.FileIo — Implementation Plan v1.2

**Document ID:** VEST-HLP-FILEIO-PLAN-012  
**Version:** 1.2  
**Status:** Active. Build mode follows this file and the PR0N plans.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.FileIo`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Previous plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md) (engine phases 0–5; do not reopen)

If this file and the SRS disagree, the SRS wins. If a PR plan and this file disagree, this file wins on sequence; the PR plan wins on step list.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.FileIo/`.

---

## 0. Why v1.2 exists

v1.0 shipped the job engine (recon, buckets, UniqueName, Audit Mode, Pause/Cancel, Analytics snapshots). v1.2 does not grow verbs. It fixes the package graph, finishes Vestigium.Logging the way Analytics already did, and makes tests and docs tell the truth.

## 1. Logging baseline (do not re-discover)

`Directory.Build.props` at the repo root is imported by MSBuild for **every** project under this repository:

```xml
<VestigiumLoggingVersion>1.7.1</VestigiumLoggingVersion>
<PackageReference Include="Vestigium.Logging" Version="$(VestigiumLoggingVersion)" />
```

That is why `Vestigium.Helpers.FileIo.csproj` does not list Logging and still compiles `using Vestigium.Logging`.

Already in FileIo:

| Piece | What it does |
|---|---|
| `FileIoLog` | Writes through `VestigiumLog.Write`. No-op until the host calls `VestigiumLogger.Initialize`. Never attaches `Exception`. |
| `FileIoCatalog.Register` | Taxonomy + event rows for APPID `FileIo`, category `Helpers`. |
| `EventCatalog/fileio.json` | JSON twin of the event block 12500–12999. Packed as content. |
| `FileIoLoggingTests` | Probe → EVENTID 12505. Missing analyze → 12520. No host does not throw. |
| `HelperCompat` | Local `HelperLog` / `HelperGuard` names so `FileIoJob` still compiles. Not the Logging package. |

v1.2 work is: consume Analytics as NuGet, give decisions their own EVENTIDs, pass `JobId` as `correlationId`, delete the compat shim.

## 2. Locked decisions (still locked)

Copied from SRS §2. Do not debate in a PR.

| # | Lock |
|---|---|
| 1 | Lead time default 15 s, range 0–180. Above 180 throws. |
| 2 | Certainty is 100 only when `ReconComplete`. |
| 3 | Live progress is chatty. JSONL is sparse. |
| 4 | Product name is **Audit Mode**. |
| 5 | Delete uses the same recon team and five buckets. |
| 6 | LAD is not this wave. |
| 7 | Category `Helpers`. APPID `FileIo`. Library never calls `VestigiumLogger.Initialize`. Door is `FileIoLog` → `Vestigium.Logging`. |
| 8 | Default collision UniqueName `.##`. Cap is `NameCap`, never overwrite. |
| 9 | Pause finishes the current 64 KiB. Cancel aborts and deletes dest this job created. |
| 10 | No scheduler, admin, VSS, ACL copy. |
| 11 | BCL streams. 64 KiB. Never `ReadAllBytes` on a payload. |
| 12 | Paths and digest hex may be logged. File contents and `Exception` objects must not. |
| 13 | Five buckets, fixed edges and worker counts. |
| 14 | Unique-content is digest. UniqueName is name. |
| 15 | Retries default 3 / 2 s. |
| 16 | Purge default false. |
| 17 | Probe and tests stay on `%TEMP%`. |

## 3. PR sequence

| PR | Priority | Goal | Plan |
|---|---|---|---|
| **PR01** | P0 | Analytics 1.0.1 NuGet. Drop Analytics project reference. | [PR01](PR01_ImplementationPlan.md) |
| **PR02** | P0 | EVENTID catalog, JobId correlation, retire HelperCompat. | [PR02](PR02_ImplementationPlan.md) |
| **PR03** | P1 | Engine tests back in the suite. | [PR03](PR03_ImplementationPlan.md) |
| **PR04** | P1 | Dest index on disk. One mask helper. | [PR04](PR04_ImplementationPlan.md) |
| **PR05** | P2 | Demo gallery **or** README stops claiming one. | [PR05](PR05_ImplementationPlan.md) |
| **PR06** | P2 | Pack FileIo. Hashing NuGet only if published. | [PR06](PR06_ImplementationPlan.md) |

One PR at a time. Commit message form: `FileIo PR0N: <short goal>`.

## 4. Explicitly out of v1.2

LAD, directory monitor, scheduler, admin / VSS / ACL, archive-bit flags, ADS, configurable bin edges, encrypting the index, spawning `robocopy.exe`, `ReadAllBytes` on a payload, Charts project reference, tree-diff Compare.

Hashing stays a **project** reference until `Vestigium.Helpers.Hashing` is on the same feed as Analytics 1.0.1. That decision lives in PR06, not PR01.

## 5. Commands (every close gate)

From the repo root:

```text
dotnet restore src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet build src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~FileIo
```

PR-specific filters are in each PR plan.

## 6. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 9 Sep 2026 | Engine phases 0–5. |
| 1.2 | 19 Sep 2026 | Packaging + Logging + test/index/gallery wave. PR01–PR06. |
