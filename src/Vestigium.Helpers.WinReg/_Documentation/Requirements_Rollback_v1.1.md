# Vestigium.Helpers.WinReg — Rollback + Edit List (Rev 1.2)

**Document ID:** VEST-HLP-WINREG-SRS-RB-000  
**Version:** 1.2  
**Status:** Brainstorm / paper  
**Date:** 14 September 2026

## Locked from review

- Edit list is a **file** the host can save and reload, not only a C# object.
- Restore-from-export accepts **`.reg` and `vest-regidx/1`**.
- DPAPI default still open (explained below).

## DPAPI, in English

The journal has to store **old value bytes**. That is the only way Rollback can put `Foo` back to what it was. Those bytes can be passwords, connection strings, license keys.

**DPAPI** (Windows Data Protection API) is not encryption we invent. It is “Windows, lock this blob to this user on this machine.”

| | DPAPI **on** (recommended default) | DPAPI **off** |
|---|---|---|
| File on disk | Encrypted blobs. Another Windows user on the same PC cannot read the values. Copy the file to a USB and open it on another PC → useless. | Plain JSON. Anyone with the file can read every old value. |
| Same user, same PC, next week | `Load` works. Rollback works. | Same. |
| Different admin, same PC | Cannot decrypt. They can still see paths and op types. | They see everything. |
| Sneakernet journal to your desk | You **cannot** rollback from that copy. Take a new journal on the box that applied the edit. | You could rollback from the copy — and you just carried secrets in clear text. |

So DPAPI is a tradeoff:

- **On:** safer journal, tied to the operator account that ran Apply/Import.
- **Off:** portable journal, dangerous if the file walks off the box.

Recommendation: **default on**. Hosts that need a portable journal pass `protect: false` and own that risk. HelperLog still never prints value bytes either way.

There is also `CurrentUser` vs `LocalMachine` DPAPI. We use **CurrentUser**. LocalMachine means any process on the box can decrypt.

## Edit list file

Schema `vest-regedits/1` JSON (not JSONL — small, edited by humans).

```json
{
  "schema": "vest-regedits/1",
  "label": "ticket-4412",
  "ops": [
    { "op": "SetValue", "hive": "CurrentUser", "path": "Software\\Contoso\\App", "name": "DisplayName", "type": "String", "data": "New" },
    { "op": "RenameValue", "hive": "CurrentUser", "path": "Software\\Contoso\\App", "from": "OldName", "to": "NewName" },
    { "op": "SetValue", "hive": "CurrentUser", "path": "Software\\Contoso\\App", "name": "NewName", "type": "String", "data": "changed" },
    { "op": "DeleteValue", "hive": "CurrentUser", "path": "Software\\Contoso\\App", "name": "Gone" }
  ]
}
```

```csharp
var list = RegistryEditList.Load(editsPath);
RegistryHelper.Apply(list, journalPath, confirm: true);
RegistryHelper.Rollback(journalPath, confirm: true);
```

The **edits file** is the recipe (can be reused). The **journal file** is the before-image of one apply on one machine (usually not reused).

## Restore sources

```csharp
RegistryHelper.Restore(snapshotPath, confirm: true, journalPath);
```

`snapshotPath`:

- `.reg` → existing Import parser, journal each line
- `.jsonl` that starts with `vest-regidx/1` → apply index rows as SetValue/CreateKey (hash-only rows cannot restore payload → those keys `Unsupported` unless the index was written with `includePayload` for small text)

That last point matters: an index without payloads cannot put string values back. Restore-from-index for real rollback needs `includePayload: true` at collect time, or the host should restore from `.reg` instead. Document that on the API.

## Still two files after every apply

| File | Role |
|---|---|
| `edits.json` or `export.reg` / `box.jsonl` | What we wanted |
| `apply-2026-09-14.jnl` | What the hive looked like before, so we can undo |
