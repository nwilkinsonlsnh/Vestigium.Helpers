# Vestigium.Helpers.WinReg — Backlog Design

**Document ID:** VEST-HLP-WINREG-DSN-BL-000  
**Version:** 1.0  
**Status:** Locked with [`Requirements_Backlog_v1.0.md`](Requirements_Backlog_v1.0.md)  
**Date:** 13 September 2026

Comparer JSONL schemas in [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md) do not change except optional payload fields on Changed deltas.

---

## B1 LastWriteTime

`RegQueryInfoKey` last-write FILETIME → `DateTimeOffset.Utc`.  
Open failure → field null, `Availability` entry `LastWriteTime / Denied`.

---

## B2 Client-scoped index

```text
RegistryClient.WriteIndex(path, hive, key, view, confirm, progress, cancel)
RegistryHelper.WriteIndex(...) → Local.WriteIndex(...)
```

Compare remains a static helper over two files. It does not open remote hives.

---

## B3 Progress + cancel on collect

Reuse `RegistryCompareProgress` (Phase = Export | Import | IndexLeft | Done).  
Cancel → `RegistryWriteStatus.Denied`, reason `canceled`. Partial files may exist; hosts delete.

---

## B4 Compare summary

```text
RegistryCompareSummary
  Verdict, Relatedness, Delta, Stopped
  Same, Changed, LeftOnly, RightOnly
  OutputPath
  IReadOnlyList<RegistryDelta> Deltas   // cap 256; file may contain more
```

`Compare` overload returns `(RegistryWriteResult, RegistryCompareSummary)` or a result type that includes both. JSONL format unchanged.

---

## B5 includePayload

On Changed only. If both canonical payloads ≤ 256 bytes, add `leftText` / `rightText` (strings) or `leftHex` / `rightHex` (binary).  
Index files never gain payload fields.

---

## B6 IndexFromReg

Parse with existing `RegistryRegFile`. Each key/value → index line. Header hive/path from the first key in the `.reg` (common prefix). If the file spans multiple hives → Denied (`multi-hive`).

---

## B7 Default value

Full GetKey always calls `GetValue("", doNotExpand)` and adds `RegistryValueInfo` with `IsDefault = true` when present.

---

## B8 Privilege revert

`EnablePrivileges` returns previous attributes. `RevertPrivileges` in `finally` after `RegSaveKeyEx` / `RegLoadKey` / `RegUnLoadKey`.

---

## B9 CanConnect

`Task.Wait(timeout)` stays. On timeout, do not observe the task. Accept a leftover RPC; document it **or** use `OpenRemoteBaseKey` on a worker with `CancelAfter` and ignore the result. Prefer: `CancellationTokenSource(timeout)` passed into a single wait. Do not start a second connect.

---

## B10 Mount signature

Shipped:

```text
IRegistryMount? MountHive(..., bool confirm, out RegistryWriteResult result)
```

Parent SRS §12 is amended. Do not break the engine to match the old paper.

---

## B11 Import allow-list

```text
Import(path, view, confirm, allowedHives: IReadOnlyList<RegistryHiveKind>? = null)
```

Null = all hives (today). Non-null = first key outside the list → Denied with line number.

---

## B12 Copy / rename

`CopyKey(srcHive, src, destHive, dest, view, confirm)` recursive values + subkeys.  
`RenameKey` = copy then `DeleteKey(recursive: true)`.

---

## B13 Search cancel

Walk checks `cancel` each key. Return list collected so far. Do not throw.

---

## B14 Ignore prefixes (after core)

Relative path `StartsWith` any prefix → skip in verify union and value merge. Documented in footer `ignored`.
