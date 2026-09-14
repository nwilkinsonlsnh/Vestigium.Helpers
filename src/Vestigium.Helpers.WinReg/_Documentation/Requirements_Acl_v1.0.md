# Vestigium.Helpers.WinReg — ACL + Hygiene Requirements

**Document ID:** VEST-HLP-WINREG-SRS-ACL-000  
**Version:** 1.0  
**Status:** Draft for A0 lock  
**Date:** 14 September 2026

## Atomic rename (what that means)

Windows has no “move key” that copies values then deletes the old key as one transaction. Today `RenameKey` is **copy tree + delete source**. If delete fails you have **two** trees. If copy fails mid-walk you have a **partial** dest.

**Atomic rename** here means: when source and dest are in the **same hive**, same view, and dest is only a new name under the same parent (or a new sibling name), call `RegRenameKey` so the key identity moves in one kernel operation. No second copy of values. Children come along. That is what `regedit` rename does.

Cross-hive or dest that is a different path prefix still uses copy + delete and stays non-atomic. Document that.

## Scope

| In | Out |
|---|---|
| Helper façades for Copy / Rename | Alt creds |
| Cancel/progress on Copy / Rename | Remote mount |
| Restore **previous** backup/restore privilege state | Kql `REG.*` |
| `IndexFromHive` = mount + index + dismount | Watchers |
| Hash `REG_LINK` / resource list as raw bytes | Import transactions |
| ACL + owner **read** on Full snapshot | Taking ownership of HKLM roots in tests |
| ACL **write** + take ownership with `confirm` | Changing DACL on forbidden paths |
| `RegRenameKey` same-parent rename | Demo product UI |

## Functional

### Hygiene

- `RegistryHelper.CopyKey` / `RenameKey` pass through to `Local`.
- Copy and Rename accept `IProgress<RegistryCompareProgress>` and `CancellationToken`. Cancel returns `Denied` / `canceled` and does not keep walking.
- `PrivilegeScope` records whether each privilege was already enabled. Dispose restores that, it does not force-off a privilege the process already had.
- `RegistryHelper.IndexFromHive(hiveFile, destIndex, destination, subKey, confirm, …)` mounts, writes index, dismounts. Mount failure returns that result. Index failure still tries dismount.
- `REG_NONE`, `REG_LINK`, `REG_RESOURCE_LIST`, `REG_FULL_RESOURCE_DESCRIPTOR`, `REG_RESOURCE_REQUIREMENTS_LIST` hash as type-byte + raw bytes, not Unicode text.

### ACL read

- Full `GetKey` fills `Owner` (DOMAIN\User or SID string) and `Sddl` when `GetSecurityInfo` / `ConvertSecurityDescriptorToStringSecurityDescriptor` succeeds.
- Slim / Identity do not pay for ACL.
- Failure → field null + `Availability` Denied (`GetNamedSecurityInfo`). Never throw for a missing ACL on an otherwise readable key.
- Logs carry path + status, never the SDDL string.

### ACL write

- `SetOwner(hive, key, account, confirm)` — account is `DOMAIN\User`, `.小User`, or SID. Requires `confirm`.
- `TakeOwnership(hive, key, confirm)` — owner becomes the current process token user.
- `SetSddl(hive, key, sddl, confirm)` — replace DACL from SDDL. Owner-only SDDL is invalid → `InvalidPath`.
- Forbidden paths stay Denied. Test writes stay under `HKCU\Software\Vestigium\Helpers.Tests\*`.
- Enable `SeTakeOwnershipPrivilege` / `SeRestorePrivilege` / `SeSecurityPrivilege` only inside a scope that restores previous state.

### Rename

- Same hive, dest is `parent\NewName` where source is `parent\OldName` → `RegRenameKey`.
- Dest exists → `InUse`.
- Otherwise keep copy + delete.

## Tests

HKCU sandbox only for writes. ACL write tests skip or Denied when the token cannot change owner (non-admin is fine: assert Denied, not crash).
