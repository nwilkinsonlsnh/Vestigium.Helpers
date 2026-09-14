# Vestigium.Helpers.WinReg — Rollback Journal Requirements

**Document ID:** VEST-HLP-WINREG-SRS-RB-000  
**Version:** 1.0  
**Status:** Draft  
**Date:** 14 September 2026

## Problem

Import and CRUD apply immediately. Cancel stops *further* writes. Already-applied lines stay. A modern registry tool needs a **disk journal** so a session (or an import) can be undone after the process exits.

This is not Win32 `RegCreateKeyTransacted`. That API does not cover every hive/view/ACL write we already have, and it dies with the process. The journal is the product feature.

## Decisions

| Topic | Decision |
|---|---|
| Unit of undo | **Batch** (one user action: Import file, paste, “set these 12 values”). Mutations inside a batch undo together. |
| Persistence | JSONL on disk. Schema `vest-regjnl/1`. |
| What we store | Inverse + before-hash + after-hash. Values needed to restore **do** live in the journal. They must **not** go to HelperLog. |
| Secrets | Journal is a secret file. Default path under the user’s profile. DPAPI `CurrentUser` protect the payload fields. |
| Confirm | Open, append, rollback, and purge all require `confirm`. |
| Collision | Before restore, hash current value. If it does not match the recorded after-hash, skip that row unless `force: true`. |
| Scope | Local journal. Remote writes can append if the client is `For(machine)` but the file lives on the **operator** box. |
| Out | Cross-machine replay as a deployment tool. Redo is in v1. SACL. Alt creds. |

## Buffer vs journal

Call it a **journal**, not a buffer. “Buffer” sounds like RAM. The file *is* the buffer:

- `RegistryJournal.Create(path, confirm)` — header
- `BeginBatch(kind, label)` / `CommitBatch()` / `AbortBatch()` (abort still records what already hit the hive; rollback uses that)
- Helpers that write (`SetValue`, `Import`, …) take an optional `RegistryJournal`
- `RollbackBatch(id)` / `RollbackLast()` / `RollbackAll()`
- `Load(path)` to reopen tomorrow

Import without a journal stays as it is today (apply and forget).

## Functional

1. Journal records CreateKey, DeleteKey, SetValue, DeleteValue, CopyKey, RenameKey, SetOwner, SetSddl, and each Import line as one of those.
2. Before a mutating write, capture before-image (missing = `created`). After success, append the journal row.
3. Rollback walks the batch **newest mutation first**. Restore old value/key/ACL or delete what we created.
4. Rollback itself is journaled as a batch `kind=rollback` so you can see history. v1 does not roll back a rollback (no redo).
5. `force: false` (default): skip a row when live hash ≠ after-hash. Count skips in the result.
6. Forbidden paths never written, never journaled.
7. Max rows per journal: 100_000. Max payload per value: 64 KiB stored; larger values store hash only and rollback of those rows is `Unsupported`.
8. Purge journal file requires confirm. Do not leave plaintext copies.

## Tests

HKCU `Helpers.Tests` only. Import three values → RollbackLast → values gone. External edit of one value → RollbackLast skips that row, restores the others. Load existing journal file from disk.
