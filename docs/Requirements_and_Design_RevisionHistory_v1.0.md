# Vestigium.Helpers — requirements and design revision history

**Document ID:** VEST-HLP-DOC-REV-000  
**Version:** 1.1  
**Date:** 15 September 2026  
**Status:** Current. Suite green as of this date.

**Rule:** Per-library requirements and design stay in `src/<library>/_Documentation/`. This file is the lossless index. Do not delete older files; append a revision.

## Live specs

| Library | Requirements | Design (as built) |
|---|---|---|
| Processes | `Requirements_v1.0.md` + [`Requirements_v1.3_Addendum.md`](../src/Vestigium.Helpers.Processes/_Documentation/Requirements_v1.3_Addendum.md) | [`Design_v1.3.md`](../src/Vestigium.Helpers.Processes/_Documentation/Design_v1.3.md) |
| Kql | `Requirements_v1.0.md` + [`Requirements_v1.1_Addendum.md`](../src/Vestigium.Helpers.Kql/_Documentation/Requirements_v1.1_Addendum.md) | [`Design_v1.1.md`](../src/Vestigium.Helpers.Kql/_Documentation/Design_v1.1.md) |
| Services | `Requirements_v1.0.md` + [`Requirements_v1.1_Addendum.md`](../src/Vestigium.Helpers.Services/_Documentation/Requirements_v1.1_Addendum.md) | [`Design_v1.1.md`](../src/Vestigium.Helpers.Services/_Documentation/Design_v1.1.md) |
| WinReg | `Requirements_v1.0.md` + ACL/Comparer/Rollback/Purge + [`Requirements_v1.4_Addendum.md`](../src/Vestigium.Helpers.WinReg/_Documentation/Requirements_v1.4_Addendum.md) | [`Design_v1.4.md`](../src/Vestigium.Helpers.WinReg/_Documentation/Design_v1.4.md) |
| Network | `Requirements_v1.0.md` + [`Requirements_v1.4_Addendum.md`](../src/Vestigium.Helpers.Network/_Documentation/Requirements_v1.4_Addendum.md) | `BandwidthTypes.cs` |
| Analytics, Charts, ClosedXml, Csv, Encryption, FileIo, Hashing, Json, Xml, Core | each `src/<lib>/_Documentation/Requirements_v1.0.md` | unchanged this cycle |

## Shared contract

1. Mutating writes need `confirm: true`.
2. Caps throw `ArgumentOutOfRangeException`. Blank input throws `ArgumentException`.
3. Missing process / key / service → null / NotFound / Gone, not a fake row.
4. Logs never print secrets, passwords, SDDL, or value payloads.
5. Tests follow the library.

## Revision history (newest first)

| Date | Rev | Library | What |
|---|---|---|---|
| 2026-09-15 | **Docs 1.1** | all four + Network | Status set to Accepted. Tests passing. Addenda + as-built design files. |
| 2026-09-15 | **B6** | Network / Logging / Tests | `DataUnit.Megabit`. WinReg InternalsVisibleTo. Warning status. |
| 2026-09-15 | **B5** | Kql | Compile diagnostics. Alias listing. |
| 2026-09-15 | **B4** | Services | Tree cap = unique names, 256. |
| 2026-09-15 | **B3** | Processes | Gone vs null. Cap OutOfRange. ThreadState kept. |
| 2026-09-15 | **B1–B2** | WinReg | beforeTree, RenameValue mut, ReadJournalText, InUse, Restore Unsupported. |
| 2026-09-15 | **R0–R8** | WinReg | Journal, edit list, rollback, purge. |
| 2026-09-15 | **A1–A7** | WinReg | ACL + atomic rename. |
| 2026-09 | **C1–C4** | WinReg | Offline compare. |
| 2026-09 | Phases 0–8 | Services / Processes / Kql / WinReg | First ship of each library. |
