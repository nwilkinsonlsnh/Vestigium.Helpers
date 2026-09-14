# Vestigium.Helpers.WinReg — Rollback Journal Plan

**Document ID:** VEST-HLP-WINREG-PLAN-RB-000  
**Version:** 1.0  
**Status:** Paper. R1–R6 planned.  
**Date:** 14 September 2026

ACL slice A0–A7 stays closed.

| Phase | Covers | Status |
|---|---|---|
| **R0 Paper** | This SRS + design. Journal ≠ RAM buffer. DPAPI. Collision skip. | **Ready** |
| **R1 Types + file** | `RegistryJournal`, header/batch/mut JSONL, Create/Load, confirm, cap. | Planned |
| **R2 Capture** | Before-image helper. Optional `journal` on SetValue / DeleteValue / CreateKey / DeleteKey. | Planned |
| **R3 Import + copy/rename** | Import lines and Copy/Rename append muts. | Planned |
| **R4 RollbackLast / Batch** | Inverse apply, collision skip, `force`, append `mut-undo`. | Planned |
| **R5 ACL muts** | SetOwner / SetSddl / TakeOwnership journal + restore SDDL. | Planned |
| **R6 Harden** | DPAPI round-trip, purge, max payload, tests `RegistryRollbackR*`. | Planned |

## Gates

- HelperLog never prints journal payload or SDDL.
- Writes without `journal:` unchanged.
- HKCU sandbox tests only.
- Rollback of a value > 64 KiB is `Unsupported` on that row, not a throw.
