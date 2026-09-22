# Vestigium.Helpers.WinReg — Backlog Requirements

**Document ID:** VEST-HLP-WINREG-SRS-BL-000  
**Version:** 1.0  
**Status:** Locked paper — 13 September 2026  
**Package:** `Vestigium.Helpers.WinReg` (library only; demo out)  
**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Comparer:** [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md)  
**Design:** [`Design_Backlog_v1.0.md`](Design_Backlog_v1.0.md)  
**Plan:** [`ImplementationPlan_Backlog_v1.0.md`](ImplementationPlan_Backlog_v1.0.md)

If this file and the parent SRS disagree on a row below, **this file wins** for that row.  
CRUD / no-reg.exe / confirm / no payloads in logs still come from the parent.

---

## 0. Scope

Close the library gaps found after Phase 7 + Comparer C4. Not a new package.

| Pri | Gap | Binding outcome |
|---|---|---|
| P0 | `LastWriteTime` never filled | Full `GetKey` populates it when Win32 returns it. Missing → null + Availability Denied. |
| P0 | `WriteIndex` / `Compare` only on `RegistryHelper.Local` | Same methods on `RegistryClient`. Remote collect uses `For(machine).WriteIndex`. Compare stays file-vs-file on the desk. |
| P0 | Export / Import / WriteIndex have no progress or cancel | All three take `IProgress<RegistryCompareProgress>` (or a shared `RegistryJobProgress`) + `CancellationToken`. Cancel → Denied, no throw. |
| P1 | Compare result is only a JSONL file | `Compare` still writes the file and also returns `RegistryCompareSummary` (verdict, counts, path). Optional `deltas` list cap 256. |
| P1 | `includePayload` is a no-op | Changed rows may carry left/right text when `includePayload: true` and canonical size ≤ 256 bytes. Never on Same / Left / Right. Never in HelperLog. |
| P1 | `.reg` cannot become an index | `IndexFromReg(regPath, indexPath, confirm, progress, cancel)` parses 5.00/4.00 and writes `vest-regidx/1`. |
| P1 | Default value (`@` / `""`) missing on Full GetKey | Full snapshots include the default value when present. Index / export / compare use name `""`. |
| P1 | Backup/restore privilege not reverted | Enable for the native call, revert immediately after, even on failure. |
| P2 | `CanConnect` abandons `Task.Run` on timeout | Cancel the wait; do not leave a runaway RPC after false. |
| P2 | `MountHive` signature ≠ parent SRS | Keep the shipped `out RegistryWriteResult` shape. Parent SRS §12 is amended to match the engine. |
| P2 | Import applies any hive the `.reg` names | Import takes optional `allowedHives`. Default = all. Hosts that want a sandbox pass `CurrentUser` only. Forbidden HKLM paths still Denied. |
| P2 | No copy / rename key | `CopyKey` / `RenameKey` with confirm. Rename = copy + recursive delete of source. Forbidden paths Denied. |
| P2 | Search has no cancel | `Search(..., CancellationToken cancel = default)`. Cancel stops the walk and returns hits so far. |
| P3 | No ACL / owner read | Deferred. Not in this backlog's ship gate. |
| P3 | No compare ignore-list | `Compare(..., IReadOnlyList<string>? ignorePathPrefixes = null)`. Prefix match on relative path. After B-core. |
| P3 | No `RegistryTestHooks` | Test hook for dest folder only if a later host needs it. Not a ship gate. |

Still out: alt creds, remote mount, Kql `REG.*`, watchers, transactions, ACL write.

---

## 1. Acceptance (backlog)

1. Full GetKey on HKCU test root has `LastWriteTime` non-null.
2. `For(".").WriteIndex` equals `RegistryHelper.WriteIndex` on the same key.
3. WriteIndex / Export cancel with a pre-canceled token → Denied.
4. Compare returns a summary whose `Changed` matches footer `changed`.
5. `includePayload: true` on a Changed string ≤ 256 bytes puts text on the delta line; HelperLog still has no payload.
6. `IndexFromReg` of a Phase-4 export compares Related to a live `WriteIndex` of the same key.
7. Full GetKey sees a default value set on the test key.
8. After Export HiveFile, `SeBackupPrivilege` is not left enabled (best-effort assert via native query or documented revert call).
9. Import with `allowedHives: CurrentUser` refuses an HKLM key line.
10. Search canceled token returns without hanging.
