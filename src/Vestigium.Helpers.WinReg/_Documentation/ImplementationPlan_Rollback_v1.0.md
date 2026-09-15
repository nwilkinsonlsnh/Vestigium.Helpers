# Vestigium.Helpers.WinReg — Rollback Journal Plan

**Document ID:** VEST-HLP-WINREG-PLAN-RB-000  
**Version:** 1.1  
**Status:** Paper. R1–R7 planned.  
**Date:** 14 September 2026

See also `Requirements_Rollback_v1.1.md` (import undo, export restore, edit list).

| Phase | Covers | Status |
|---|---|---|
| **R0 Paper** | Journal + edit list + export-as-restore. | **Ready** |
| **R1 Types + file** | `RegistryJournal` JSONL Create/Load. | Planned |
| **R2 Capture on CRUD** | Optional `journalPath` on Set/Delete/Create/DeleteKey. | Planned |
| **R3 Import + RestoreFromExport** | Import writes journal. RestoreFromExport = Import + journal. | Planned |
| **R4 Edit list + RenameValue** | `RegistryEditList` / `Apply`. RenameValue = set new + delete old inside one mut pair. | Planned |
| **R5 Rollback** | Rollback(path). Collision skip. `force`. | Planned |
| **R6 Copy/RenameKey + ACL muts** | Same journal rows. | Planned |
| **R7 Harden** | DPAPI, purge, 64 KiB cap, tests `RegistryRollbackR*`. | Planned |
