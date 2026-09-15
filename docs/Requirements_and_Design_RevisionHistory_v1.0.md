# Vestigium.Helpers — requirements and design revision history

**Document ID:** VEST-HLP-DOC-REV-000  
**Version:** 1.0  
**Date:** 15 September 2026  
**Rule:** Per-library requirements and design stay in `src/<library>/_Documentation/`. This file is the lossless index and history. Do not delete older `_Documentation` files; append a revision row instead.

## Where the live specs live

| Library | Requirements | Design / extra |
|---|---|---|
| Core (`Vestigium.Helpers`) | `_Documentation` if present | `HelperGuard`, `HelperLog` |
| Processes | `src/Vestigium.Helpers.Processes/_Documentation/Requirements_v1.0.md` | `Campaigns_v1.2.md` |
| Services | `src/Vestigium.Helpers.Services/_Documentation/Requirements_v1.0.md` | tree / control / campaigns in same folder |
| Kql | `src/Vestigium.Helpers.Kql/_Documentation/` | catalog + binder |
| WinReg | `src/Vestigium.Helpers.WinReg/_Documentation/Requirements_v1.0.md` | ACL, compare, journal, purge |
| Network | library `_Documentation` | `DataUnit` in `BandwidthTypes.cs` |
| Analytics, Charts, ClosedXml, Csv, Encryption, FileIo, Hashing, Json, Xml | each `src/<lib>/_Documentation/` | — |

Cross-cutting plans (do not replace library specs):

- `docs/Backlog_LibraryFromTests_v1.0.md`
- `docs/ImplementationPlan_LibraryBacklog_P1P3_v1.0.md`
- `docs/ImplementationPlan_JournalPurge_v1.0.md` (if present)

## Contract that every library shares

1. Writes that mutate a machine need `confirm: true`.
2. Caps throw `ArgumentOutOfRangeException` (`HelperGuard.InRange` / `AtMost`). Blank input throws `ArgumentException`.
3. Missing process / key / service returns null or `NotFound` / `Gone`, not a fake row.
4. HelperLog never prints secrets, passwords, SDDL, or value payloads.
5. Tests follow library behavior; they do not invent API members.

## Revision history (newest first)

| Date | Rev | Library | What changed |
|---|---|---|---|
| 2026-09-15 | **B6** | Network, Logging, Tests | `DataUnit.Megabit` (= `Mb`) and long names. WinReg `InternalsVisibleTo` Tests. `VestigiumStatus.Warning` required. |
| 2026-09-15 | **B5** | Kql | `KqlCompileResult.Diagnostics` for `==` / `!=` with `% * ?`. Short names and aliases listed on unknown-field errors. |
| 2026-09-15 | **B4** | Services | Tree cap = unique names visited. `MaxTreeNodes` = `MaxNodes` = 256 both directions. |
| 2026-09-15 | **B3** | Processes | Dead PID → null. Search cap `AtMost` → `ArgumentOutOfRangeException`. `ThreadState` kept (coverage alias). `ProcessThreadState` also exists with the same values. |
| 2026-09-15 | **B2** | WinReg | `ReadJournalText`, rollback `batchId`, single `RenameValue` mut. |
| 2026-09-15 | **B1** | WinReg | `DeleteKey` `beforeTree`, Restore index without payload → `Unsupported`, Compact/Purge `InUse` if journal open. |
| 2026-09-15 | **R8** | WinReg | Journal purge: keep last N, older than, undone-only, batch id, archive, dry-run. |
| 2026-09-15 | **R0–R7** | WinReg | Journal `vest-regjnl/1`, edit list, import/restore journal, rollback last batch, optional DPAPI. |
| 2026-09-15 | **A1–A7** | WinReg | ACL read/write, take ownership, atomic rename. |
| 2026-09-15 | **C1–C4 / B1–B7 compare** | WinReg | Offline index compare, relatedness, progress. |
| 2026-09 | **WinReg Phases 0–7** | WinReg | Local/remote CRUD, export/import no `reg.exe`, mount/dismount. |
| 2026-09 | **Services Phases 0–8** | Services | List (incl. hidden), control, logon, recovery, tree, campaigns, remote. |
| 2026-09 | **Processes Phases 1–9 + PR02** | Processes | Snapshot fields, watchers, campaigns, Kql search, GPU/system counters. |
| 2026-09 | **Kql Phases 1–7** | Kql | Lexer/parser/binder/eval, packs, `==` wildcard is literal + warning. |

## How to add a revision without losing history

1. Do not rewrite `Requirements_v1.0.md` in place as a silent change.
2. Append a dated subsection **Revision N** at the bottom of that file, or add `Requirements_v1.N.md` and leave v1.0 in the tree.
3. Add one row to the table above in the same PR.
