# Vestigium.Helpers.WinReg — Rollback + Edit List (Rev 1.3)

**Document ID:** VEST-HLP-WINREG-SRS-RB-000  
**Version:** 1.3  
**Status:** Brainstorm / paper  
**Date:** 14 September 2026

## Locked

- Edit list is a file (`vest-regedits/1`) and a C# builder.
- Restore accepts `.reg` and `vest-regidx/1`.
- **DPAPI off by default.** `protect: true` is opt-in advanced.
- Edit list **is** the developer CRUD batch: keys, values, value data.

## DPAPI

Default journal is readable JSON so a host can copy it, inspect it, and Load it on another box as the same operator workflow. `protect: true` wraps payload fields with CurrentUser DPAPI. HelperLog never prints value bytes either way.

## Yes: developer list of actions (CRUD)

`RegistryEditList` / `Apply(list, journalPath)` is how a developer says “do these N things” in one shot.

| Target | Create | Read | Update | Delete |
|---|---|---|---|---|
| **Key** | `CreateKey` | (use `GetKey`, not in the list) | `RenameKey` | `DeleteKey` |
| **Value name** | `SetValue` (new name) | (use `GetValue`) | `RenameValue` | `DeleteValue` |
| **Value data** | `SetValue` | (use `GetValue`) | `SetValue` (same name, new data) | `DeleteValue` |

Read stays on `GetKey` / `GetValue` / Search. A mutation list does not need Get.

Example:

```csharp
var edits = new RegistryEditList()
    .CreateKey(hive, @"Software\Contoso\App")
    .SetValue(hive, path, "DisplayName", "New", RegistryValueKind.String)
    .SetValue(hive, path, "Timeout", 30, RegistryValueKind.DWord)
    .RenameValue(hive, path, "OldFlag", "NewFlag")
    .DeleteValue(hive, path, "Deprecated")
    .DeleteKey(hive, path + @"\Temp", recursive: true);

edits.Save(editsPath);
RegistryHelper.Apply(editsPath, journalPath, confirm: true);
RegistryHelper.Rollback(journalPath, confirm: true);
```

Same ops in the JSON file so a host can ship a recipe without compiling.

Apply walks the list in order. One journal batch. Rollback walks newest mut first.

ACL (`SetOwner` / `SetSddl`) is a later row type on the same list, not v1 of Apply unless we pull R6 forward.
