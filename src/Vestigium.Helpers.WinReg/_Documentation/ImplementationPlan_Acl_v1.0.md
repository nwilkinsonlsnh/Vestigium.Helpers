# Vestigium.Helpers.WinReg — ACL + Hygiene Plan

**Document ID:** VEST-HLP-WINREG-PLAN-ACL-000  
**Version:** 1.1  
**Status:** A0 paper. A1–A7 planned.  
**Date:** 14 September 2026

B0–B7 stay closed. This is a new slice.

| Phase | Covers | Status |
|---|---|---|
| **A0 Paper** | SRS + design. Rename invariant: never two keys. | **Ready** |
| **A1 Façade + copy cancel** | Helper Copy/Rename. Progress + cancel. Cancel deletes partial dest. | Planned |
| **A2 Privilege previous state** | Restore was-enabled. | Planned |
| **A3 IndexFromHive** | Mount → WriteIndex → Dismount. | Planned |
| **A4 Rare type hash** | Link / resource list as raw bytes. | Planned |
| **A5 ACL + owner read** | Full snapshot Owner + Sddl. | Planned |
| **A6 ACL write + take ownership** | SetOwner, TakeOwnership, SetSddl. | Planned |
| **A7 Atomic rename** | `RegRenameKey` same parent. Else copy + delete source; if delete fails delete dest. Cancel rolls dest back. | Planned |

## Rename rule (A7 gate)

After every `RenameKey` result:

- `Ok` → dest exists, source does not.
- Any failure → source exists, dest does not (unless dest already existed before the call → `InUse` and we never wrote dest).

No third state.

## Gates

- Confirm on every write including ACL write.
- Forbidden paths unchanged.
- HelperLog never prints SDDL or value payload.
- ACL write tests use HKCU `Helpers.Tests` only.
- A7 tests assert the two-key invariant.
