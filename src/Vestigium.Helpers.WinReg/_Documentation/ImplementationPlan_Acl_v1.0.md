# Vestigium.Helpers.WinReg — ACL + Hygiene Plan

**Document ID:** VEST-HLP-WINREG-PLAN-ACL-000  
**Version:** 1.5  
**Status:** A0 paper. A1–A4 **Done**. A5–A7 planned.  
**Date:** 14 September 2026

| Phase | Covers | Status |
|---|---|---|
| **A0 Paper** | SRS + design. Rename invariant: never two keys. | **Ready** |
| **A1 Façade + copy cancel** | Helper Copy/Rename. Progress + cancel. | **Done** |
| **A2 Privilege previous state** | Restore was-enabled. | **Done** |
| **A3 IndexFromHive** | Mount → WriteIndex → Dismount. | **Done** |
| **A4 Rare type hash** | Link / resource list as raw bytes. | **Done** |
| **A5 ACL + owner read** | Full snapshot Owner + Sddl. | Planned |
| **A6 ACL write + take ownership** | SetOwner, TakeOwnership, SetSddl. | Planned |
| **A7 Atomic rename** | `RegRenameKey` same parent. Else copy + delete + rollback dest. | Planned |
