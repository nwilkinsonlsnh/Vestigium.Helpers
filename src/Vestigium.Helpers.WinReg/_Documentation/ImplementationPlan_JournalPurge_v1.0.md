# Vestigium.Helpers.WinReg — Journal Purge Plan (R8)

**Document ID:** VEST-HLP-WINREG-PLAN-PURGE-000  
**Version:** 1.0  
**Status:** Paper. R8a–R8c planned.  
**Date:** 15 September 2026

Rollback R0–R7 stays closed. This is **R8** only.

| Phase | Covers | Status |
|---|---|---|
| **R8a Paper** | This SRS + design. Batch-level compact. | **Ready** |
| **R8b Compact** | `RegistryPurgeOptions`, rewrite tmp→replace, keep-last, older-than, batch id, undone-only, dry run. | Planned |
| **R8c Archive + tests** | `ArchivePath`. Tests `RegistryRollbackR8*`. Delete-all unchanged. | Planned |

## Surface

```csharp
RegistryHelper.PurgeJournal(path, confirm: true);
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { KeepLastBatches = 5, Confirm = true });
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { OlderThan = TimeSpan.FromDays(14), Confirm = true });
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { UndoneOnly = true, Confirm = true });
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { BatchId = id, Confirm = true });
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { KeepLastBatches = 5, ArchivePath = bak, Confirm = true });
RegistryHelper.PurgeJournal(path, new RegistryPurgeOptions { KeepLastBatches = 5, DryRun = true });
```

## Gates

- In-place JSONL edit is forbidden.
- HelperLog: counts only.
- Protected journals stay protected on rewrite.
- HKCU / temp-file tests only.
