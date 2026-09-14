# Vestigium.Helpers.WinReg — ACL + Hygiene Plan

**Document ID:** VEST-HLP-WINREG-PLAN-ACL-000  
**Version:** 1.0  
**Status:** A0 paper. A1–A7 planned.  
**Date:** 14 September 2026

B0–B7 stay closed. This is a new slice.

| Phase | Covers | Status |
|---|---|---|
| **A0 Paper** | This SRS + design. Atomic rename defined. | **Ready** |
| **A1 Façade + copy cancel** | `RegistryHelper.CopyKey` / `RenameKey`. Progress + cancel on copy. | Planned |
| **A2 Privilege previous state** | `PrivilegeScope` restores was-enabled, does not force off. | Planned |
| **A3 IndexFromHive** | Mount → WriteIndex → Dismount. | Planned |
| **A4 Rare type hash** | Link / resource list as raw bytes. | Planned |
| **A5 ACL + owner read** | Full snapshot `Owner` + `Sddl`. | Planned |
| **A6 ACL write + take ownership** | `SetOwner`, `TakeOwnership`, `SetSddl`. confirm. sandbox tests. | Planned |
| **A7 Atomic rename** | Same-parent `RegRenameKey`; else copy+delete. | Planned |

## Gates

- Confirm still required on every write including ACL write.
- Forbidden paths unchanged.
- HelperLog never prints SDDL, owner SID bytes, or value payload.
- ACL write tests use HKCU `Helpers.Tests` only.
- A7 tests: rename sibling under the sandbox; dest exists → `InUse`.

## Suggested test filter

`FullyQualifiedName~RegistryAclA`
