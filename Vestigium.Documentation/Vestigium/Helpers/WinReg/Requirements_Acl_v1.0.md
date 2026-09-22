# Vestigium.Helpers.WinReg — ACL + Hygiene Requirements

**Document ID:** VEST-HLP-WINREG-SRS-ACL-000  
**Version:** 1.1  
**Status:** Draft for A0 lock  
**Date:** 14 September 2026

## Atomic rename (what that means)

Windows has no transactional “move key across the hive.” Today `RenameKey` is copy + delete. That can leave **two** keys.

**Invariant:** after `RenameKey` returns, the source path and the dest path are not both present. Either the rename succeeded (only dest exists) or it failed and we rolled back (only source exists).

How:

1. Same hive, same view, dest is `parent\NewName` and source is `parent\OldName` → `RegRenameKey`. One kernel call. Children move. Dest exists beforehand → `InUse`.
2. Otherwise copy dest, then delete source. If delete source fails, **delete dest** (the copy) and return `Denied`. Caller still has the original key. Never keep both.
3. Cross-hive is allowed only through that copy + delete + rollback path. No silent second tree.

## Scope

| In | Out |
|---|---|
| Helper façades for Copy / Rename | Alt creds |
| Cancel/progress on Copy / Rename | Remote mount |
| Restore previous backup/restore privilege state | Kql `REG.*` |
| `IndexFromHive` | Watchers |
| Hash REG_LINK / resource list as bytes | Import transactions |
| ACL + owner read | Taking ownership of HKLM roots in tests |
| ACL write + take ownership with confirm | Changing DACL on forbidden paths |
| Rename never leaves two keys | Demo product UI |

## Functional

### Hygiene

- `RegistryHelper.CopyKey` / `RenameKey` pass through to `Local`.
- Copy and Rename accept progress + cancel. Cancel during copy deletes the partial dest, then `Denied` / `canceled`.
- `PrivilegeScope` restores was-enabled per privilege.
- `IndexFromHive` mounts, indexes, dismounts. Index failure still dismounts.
- Rare types hash as type-byte + raw bytes.

### ACL read

- Full `GetKey` fills `Owner` and `Sddl` when security APIs succeed.
- Slim / Identity skip ACL.
- Failure → null + Availability Denied. No throw. Logs never print SDDL.

### ACL write

- `SetOwner`, `TakeOwnership`, `SetSddl` require `confirm`.
- Forbidden paths Denied. Tests only under `HKCU\Software\Vestigium\Helpers.Tests\*`.
- Privilege scopes restore previous state.

### Rename invariant tests

- Same-parent rename: source gone, dest present, values intact.
- Dest exists: `InUse`, source unchanged.
- Copy-fallback path: if dest delete-on-rollback runs, source still has the original values.
- Cancel mid-copy: dest not left behind.
