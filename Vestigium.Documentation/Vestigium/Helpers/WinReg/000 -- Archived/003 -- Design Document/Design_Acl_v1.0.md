# Vestigium.Helpers.WinReg — ACL + Hygiene Design

**Document ID:** VEST-HLP-WINREG-DSN-ACL-000  
**Version:** 1.0  
**Date:** 14 September 2026

## PrivilegeScope

`AdjustTokenPrivileges` with a previous-state buffer per LUID. Store `(Luid, wasEnabled)`. Dispose writes that attribute back. If lookup failed, do nothing for that name.

## IndexFromHive

```
MountHive(file, dest, sub, confirm, out mountResult)
WriteIndex(indexPath, dest, sub, confirm, progress, cancel)
Dismount / Dispose
```

Destination remains HKLM or HKU. Subkey name stays caller-chosen (`VESTIGIUM_HIV_*`).

## ACL read

`GetNamedSecurityInfo` on the opened key handle (`SE_REGISTRY_KEY`) requesting `OWNER_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION`. Convert owner SID with `LookupAccountSid`; fallback `ConvertSidToStringSid`. SDDL via `ConvertSecurityDescriptorToStringSecurityDescriptor`.

New fields on `RegistryKeyInfo`:

- `string? Owner`
- `string? Sddl`

## ACL write

`SetNamedSecurityInfo` with `OWNER_SECURITY_INFORMATION` and/or `DACL_SECURITY_INFORMATION`. Parse account with `LookupAccountName` / `ConvertStringSidToSid`. Parse SDDL with `ConvertStringSecurityDescriptorToSecurityDescriptor`.

Take ownership: current process user SID from `OpenProcessToken` + `GetTokenInformation(TokenUser)`.

## RegRenameKey

```
[DllImport("advapi32.dll", CharSet=Unicode)]
int RegRenameKey(SafeRegistryHandle hKey, string? lpSubKeyName, string lpNewKeyName);
```

Open the **parent**, pass old child name + new child name. New name cannot contain `\`.

## Copy progress

Reuse `RegistryCompareProgress` (`Phase=Copy`, `KeysSeen`). Check cancel at each child.
