# Vestigium.Helpers.FileIo — PR05 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR05  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P2  
**Depends on:** PR04 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR05 makes the repo description match the slnx.

---

## Goal

Either ship `Vestigium.Helpers.FileIo.Demo` as the Developers Guide describes, or delete every sentence that says it already exists.

`Vestigium.Helpers.slnx` today has Library + Tests only. No Gallery. No Demo. Root README still lists FileIo as a shipped gallery and `dotnet run --project src/Vestigium.Helpers.FileIo.Demo`.

## Decision for build mode

Prefer **Option A** if WPF chrome can be copied from another Vestigium gallery without inventing a new shell. Choose **Option B** if Gallery is still a deleted project.

| Option | Meaning |
|---|---|
| A | Add `src/Vestigium.Helpers.FileIo.Demo`, add it to the slnx, host APPID `FileIo`. |
| B | No Demo this wave. Strike README, Developers Guide tabs list, and v1.0 plan §7 `dotnet run` line. |

Do not leave the middle state (docs say Demo, slnx does not).

## Steps — Option A

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR05.A01** | P2 | Create `Vestigium.Helpers.FileIo.Demo` (`net10.0-windows`, WPF). | Open |
| **PR05.A02** | P2 | Host init: `VestigiumLogger.Initialize` with `AppId = FileIoCatalog.AppId`, `FileIoCatalog.Register`, `AnalyticsCatalog.Register`. Library still does not Initialize. | Open |
| **PR05.A03** | P2 | Seed trees under `%TEMP%\Vestigium.Helpers.FileIo.Demo`. Gallery `ReconLeadTime` = 2 s. Library default stays 15 s. | Open |
| **PR05.A04** | P2 | Tabs from the Developers Guide: Overview, Copy, Move, Delete, Mirror, Audit Mode, UniqueName, Compare, Index, Stats, JSONL. Empty tabs with “not this wave” are forbidden; cut the list instead. | Open |
| **PR05.A05** | P2 | Add the project to `Vestigium.Helpers.slnx`. Fix root README Logging version **1.7.1** and drop the deleted `Vestigium.Helpers` core row if that project is still gone. | Open |

## Steps — Option B

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR05.B01** | P2 | Root README: FileIo has no Demo. Remove `dotnet run --project src/Vestigium.Helpers.FileIo.Demo`. Logging 1.7.1. | Open |
| **PR05.B02** | P2 | Developers Guide: strike the tab list and “WPF gallery hosts APPID FileIo”. Keep the host-init snippet. | Open |
| **PR05.B03** | P2 | `ImplementationPlan_v1.0.md` §7: remove the Demo `dotnet run` line. Leave a pointer to v1.2. | Open |

## Close gate

- Option A: Demo builds. F5 on Windows initializes Logging. JSONL lands under the host log directory. `dotnet test --filter FileIo` still green.
- Option B: grep the repo for `FileIo.Demo` returns only historical / “not shipped” sentences.

Commit: `FileIo PR05: gallery host` or `FileIo PR05: docs match slnx (no Demo)`.

## Out of PR05

Pack, Hashing NuGet, LAD, Charts inside FileIo.
