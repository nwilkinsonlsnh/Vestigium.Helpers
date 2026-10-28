# Vestigium.Helpers.WinReg — Rollback Journal Design

**Document ID:** VEST-HLP-WINREG-DSN-RB-000  
**Version:** 1.0  
**Date:** 14 September 2026

## File

UTF-8 JSONL. First line header. Then `batch` / `mut` / `footer`.

```
{"rec":"header","schema":"vest-regjnl/1","machine":"BOX","user":"DOMAIN\\u","createdAt":"..."}
{"rec":"batch","id":"b1","kind":"import","label":"box.reg","startedAt":"..."}
{"rec":"mut","batch":"b1","seq":1,"op":"SetValue","hive":"CurrentUser","path":"Software\\Vestigium\\...","name":"X","type":"String","beforeHash":"...","afterHash":"...","beforeDpapi":"...","existed":true}
{"rec":"batch-end","id":"b1","status":"committed","muts":3}
```

`beforeDpapi` is DPAPI-protected JSON `{type, data}` so the file is useless if copied to another user. Logs print batch id + path + op. Never print `beforeDpapi`.

## Wire-up

```
using var journal = RegistryJournal.Create(path, confirm: true);
journal.BeginBatch("import", fileName);
client.Import(regPath, confirm: true, journal: journal);
journal.CommitBatch();
// later, maybe another process:
using var loaded = RegistryJournal.Load(path, confirm: true);
loaded.RollbackLast(client, confirm: true);
```

`RegistryHelper.Import(..., journal)` and `SetValue(..., journal)` optional last args. Null journal = current behavior.

## Rollback algorithm

For each mut in the last committed batch, descending seq:

1. Read live value/key.
2. If `force` is false and live after-hash ≠ recorded after-hash → `Skipped`.
3. If `existed` is false → `DeleteValue` / `DeleteKey` (only if empty / only the value we created).
4. Else restore `before` payload via `SetValue` / `CreateKey` / `SetSddl`.
5. Mark mut `undone` in an appended `mut-undo` row so Load sees state.

Do not rewrite history in place. Append only. Crash mid-rollback: replay ignores muts that already have `mut-undo`.

## Collision example

Import set `Foo=1` (afterHash=H1). User later sets `Foo=2`. RollbackLast sees live hash ≠ H1 → skip Foo, restore the other keys from that import.

## Host map

- Editor “Undo import” → `RollbackLast`
- Editor “History” → parse batches
- Sneakernet: journal stays on the operator PC next to the `.reg` that was applied to a remote client
