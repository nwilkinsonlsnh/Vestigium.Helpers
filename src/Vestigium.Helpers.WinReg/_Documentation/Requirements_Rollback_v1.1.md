# Vestigium.Helpers.WinReg — Rollback + Edit List (Rev 1.1)

**Document ID:** VEST-HLP-WINREG-SRS-RB-000  
**Version:** 1.1  
**Status:** Brainstorm / paper  
**Date:** 14 September 2026

## Three features, do not mash them into one button

| Feature | What the user did | How we undo / restore |
|---|---|---|
| **Import undo** | Applied a `.reg` | Journal of what Import wrote. `Rollback(journalPath)`. |
| **Export restore** | Saved a `.reg` or index *before* edits | Re-apply that snapshot. Export itself is read-only; the file *is* the restore point. |
| **Edit list** | Developer/host sends 10 targeted muts (set / rename value / delete / …) | Same journal. One batch. One rollback path. |

Rollback of “export” does **not** mean un-writing a file on disk. It means: we took a snapshot, then we changed the hive, then we put the snapshot back.

## Edit list (the new API)

A host should not have to call `SetValue` ten times and remember the order. They build a list, apply once, get a journal path back.

Actions v1:

| Op | Meaning |
|---|---|
| `CreateKey` | Key must exist after apply |
| `DeleteKey` | Recursive flag required |
| `SetValue` | Create or overwrite value |
| `DeleteValue` | Remove value |
| `RenameValue` | New name, same type/data (or new data in the same op) |
| `RenameKey` | Same rules as A7 |

ACL ops stay on the journal later (R5). Edit list v1 is data + structure.

Shape:

```csharp
var edits = new RegistryEditList()
    .SetValue(HKCU, path, "DisplayName", "New")
    .RenameValue(HKCU, path, "OldName", "NewName")
    .SetValue(HKCU, path, "NewName", "changed")
    .DeleteValue(HKCU, path, "Gone");

var result = RegistryHelper.Apply(edits, journalPath, confirm: true);
RegistryHelper.Rollback(journalPath, confirm: true);
```

Same list can be serialized (JSON) so a host stores “this week’s 10 fixes” and reapplies on another box with a *new* journal per machine.

## Import

```csharp
RegistryHelper.Import(regPath, confirm: true, journalPath: journal);
RegistryHelper.Rollback(journal, confirm: true);
```

If `journalPath` is omitted, apply-and-forget (today).

## Export as restore point

```csharp
RegistryHelper.Export(outReg, hive, key, confirm: true);
// later, after drift:
RegistryHelper.Import(outReg, confirm: true, journalPath: undoThisRestore);
```

Optional helper so hosts do not invent the dance:

```csharp
RegistryHelper.RestoreFromExport(outReg, confirm: true, journalPath);
```

Restore-from-export **is** an Import. It gets a journal so you can undo the restore too.

Do **not** treat a raw `.hiv` as a rollback file without Mount + index. `.reg` and `vest-regidx/1` are the restore artifacts.

## Rollback path

Always a file path the developer chooses. Library does not pick a hidden temp and lose it. Host owns lifetime (keep next to the ticket, delete when the change window closes).

Collision rule stays: live hash ≠ what we wrote → skip that row unless `force`.

## Still out of v1

Redo, SACL, alt creds, journal as a deployment package to 500 machines, storing values in HelperLog, auto-journal in ProgramData.
