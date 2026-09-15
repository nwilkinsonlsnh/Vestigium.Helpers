# Vestigium.Helpers.WinReg — Rollback + Edit List Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-RB-000  
**Version:** 1.2  
**Status:** Paper ready for R0 lock  
**Date:** 14 September 2026  
**SRS:** `Requirements_Rollback_v1.1.md` (rev 1.3)

ACL slice A0–A7 stays closed. This is a new slice.

## Locked decisions

| Item | Decision |
|---|---|
| Journal | Disk JSONL `vest-regjnl/1`. Developer passes the path. |
| Edit list | Disk JSON `vest-regedits/1` **and** C# builder. |
| CRUD batch | CreateKey, DeleteKey, SetValue, DeleteValue, RenameValue, RenameKey |
| Import undo | `Import(..., journalPath)` |
| Export restore | `Restore(path)` for `.reg` **and** `vest-regidx/1` |
| DPAPI | **Off by default.** `protect: true` opt-in |
| Collision | Skip row if live hash ≠ after-hash unless `force` |
| Confirm | Apply, Import-with-journal, Restore, Rollback, Purge |
| Reads | Stay on GetKey/GetValue. Not in the edit list. |
| Redo | Out of this slice |
| ACL on the edit list | R6, not R4 |

## Scoreboard

| Phase | Covers | Status |
|---|---|---|
| **R0 Paper** | This plan + SRS 1.3. | **Ready** |
| **R1 Journal file** | `RegistryJournal` Create / Load / BeginBatch / CommitBatch. Header + batch + mut JSONL. Caps. | Planned |
| **R2 CRUD capture** | Optional `journalPath` on CreateKey, DeleteKey, SetValue, DeleteValue. Before-image + after-hash. | Planned |
| **R3 Import + Restore** | Import appends muts. `Restore(.reg \| index)` = apply + journal. Index without payload → Unsupported on that row. | Planned |
| **R4 Edit list** | `RegistryEditList` load/save/builder. `RenameValue`. `Apply(list\|path, journalPath)`. | Planned |
| **R5 Rollback** | `Rollback(journalPath)` last batch, then by batch id. Collision skip. `force`. Append `mut-undo`. | Planned |
| **R6 Copy / RenameKey / ACL muts** | CopyKey, RenameKey, SetOwner, SetSddl, TakeOwnership on the same journal. | Planned |
| **R7 Harden** | `protect: true` DPAPI. Purge. 100k mut cap. 64 KiB payload cap. Tests `RegistryRollbackR*`. | Planned |

## Public surface (target)

```csharp
RegistryJournal.Create(path, confirm, protect = false)
RegistryJournal.Load(path, confirm)

RegistryEditList.Load(path) / Save(path)
new RegistryEditList().CreateKey(...).SetValue(...).RenameValue(...).DeleteValue(...).DeleteKey(...).RenameKey(...)

RegistryHelper.Apply(RegistryEditList\|string editsPath, string journalPath, confirm, force = false, protect = false)
RegistryHelper.Import(..., journalPath = null, protect = false)
RegistryHelper.Restore(snapshotPath, journalPath, confirm, protect = false)  // .reg or vest-regidx/1
RegistryHelper.Rollback(journalPath, confirm, force = false)
RegistryHelper.PurgeJournal(journalPath, confirm)
```

CRUD methods gain an optional `journalPath` so a host that still calls SetValue one-by-one can journal without the list.

## Files to add

| File | Phase |
|---|---|
| `RegistryJournal.cs` + `RegistryJournalTypes.cs` | R1 |
| `RegistryClient` optional journal args (Writes) | R2 |
| `RegistryRestore.cs` | R3 |
| `RegistryEditList.cs` | R4 |
| `RegistryJournal.Rollback.cs` | R5 |
| Wire Copy/Rename/ACL | R6 |
| `RegistryJournal.Protect.cs` | R7 |
| `RegistryRollbackR*Tests.cs` | each phase |

## Gates

- No journal arg → behavior identical to today.
- HelperLog: batch id, hive, path, op. Never value bytes, never SDDL, never DPAPI blob.
- Forbidden paths never applied, never journaled.
- HKCU `Helpers.Tests` only for write tests.
- Restore-from-index documents `includePayload` requirement.
- `protect: true` tests skip or no-op payload compare; default path tests read JSON.

## Suggested filter

`FullyQualifiedName~RegistryRollbackR`
