# Vestigium.Helpers.FileIo — PR03 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR03  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P1  
**Depends on:** PR02 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR03 puts the engine under test again. No new verbs.

---

## Goal

`dotnet test --filter FullyQualifiedName~FileIo` covers UniqueName, Audit Mode, Pause/Cancel, retries, and Stats — not only Analyze + three logging facts.

## Baseline

`Vestigium.Helpers.Tests.csproj` currently has:

```xml
<Compile Remove="FileIoCoverageTests.cs" />
<Compile Remove="FileIoSessionTests.cs" />
```

Those two files exist on disk (~19 KB each) and are not in the suite. SRS §10 is therefore not the gate.

Live today: `FileIoAnalyzeTests`, `FileIoLoggingTests`.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR03.001** | P0 | Inventory `FileIoSessionTests` and `FileIoCoverageTests`. List fixtures that still compile against the public / `InternalsVisibleTo` surface after PR02. | Open |
| **PR03.002** | P0 | Either remove the two `<Compile Remove>` lines, or extract a public `FileIoContractTests.cs` / `FileIoPR03Tests.cs` and leave broken internals tests removed. Prefer extract if the old files call deleted HelperLog APIs. | Open |
| **PR03.003** | P0 | Contract fixtures that must be green: UniqueName `.##`; NameCap does not overwrite; Audit Mode creates no dest files; Cancel deletes dest this job created and does not delete source on Copy; `ReconLeadTime` above 180 throws; empty Stats bins are Count = 0 and Series = null. | Open |
| **PR03.004** | P1 | Retry / InUse: a locked dest fails after `RetryCount` and the job continues unless `StopOnError`. | Open |
| **PR03.005** | P1 | Tests use `%TEMP%` only. `IndexRootOverride` for any index test. Never live ProgramData or Desktop. | Open |
| **PR03.006** | P2 | Logger collection stays serial (`[Collection("Logger")]`) for fixtures that Initialize. | Open |

## Do not

- Expand the EVENTID block again.
- Implement dest jsonl persist (PR04).
- Add a Demo project (PR05).
- Hit `%ProgramData%\Vestigium\FileIo\Indexes` for real.

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

Zero skipped Compile-Remove of a file that this PR claimed to restore. Contract fixtures above are present and passing.

Commit: `FileIo PR03: restore engine contract tests`.

## Out of PR03

Index persistence, wildcard unification, gallery, pack.
