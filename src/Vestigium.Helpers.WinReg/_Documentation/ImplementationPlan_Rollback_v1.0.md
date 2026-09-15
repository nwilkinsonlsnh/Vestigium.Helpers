# Vestigium.Helpers.WinReg — Rollback + Edit List Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-RB-000  
**Version:** 1.8  
**Status:** R0 locked. R1–R6 **Done**. R7 planned.  
**Date:** 15 September 2026

| Phase | Covers | Status |
|---|---|---|
| **R0 Paper** | SRS 1.3 + this plan. | **Locked** |
| **R1 Journal file** | Create / Load / BeginBatch / CommitBatch. | **Done** |
| **R2 CRUD capture** | Optional journal on Create/Delete/Set/DeleteValue. | **Done** |
| **R3 Import + Restore** | Import + Restore(.reg \| index). | **Done** |
| **R4 Edit list** | RegistryEditList + Apply + RenameValue. | **Done** |
| **R5 Rollback** | Rollback(journalPath). | **Done** |
| **R6 Copy / RenameKey / ACL** | Same journal. | **Done** |
| **R7 Harden** | protect, purge, caps. | Planned |
