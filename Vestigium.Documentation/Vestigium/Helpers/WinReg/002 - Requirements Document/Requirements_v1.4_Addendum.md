# WinReg — Requirements addendum 1.4

**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md) plus ACL / Comparer / Rollback / Purge files.  
**Status:** Accepted — shipped. Tests passing 15 September 2026.

| Topic | Rule |
|---|---|
| Journal | `vest-regjnl/1` on disk. Per-line open-append-close. `ReadJournalText` is the shared reader. |
| Compact / Purge | Fail `InUse` if a journal instance is open. |
| DeleteKey | `beforeTree` snapshot (cap 256 keys) for rollback. |
| RenameValue | One mut (`from` / `to` + before). |
| Restore index | Without payload → `Unsupported`. |
| Rollback | Last batch or `batchId`. Confirm required. |
| Purge | Keep last N, older than, undone-only, batch id, archive, dry-run. |
| ACL | Read DACL/owner. Write / take ownership with confirm. |
| Compare | Offline index vs index. Relatedness on keys first. |
| InternalsVisibleTo | `Vestigium.Helpers.Tests`. |
