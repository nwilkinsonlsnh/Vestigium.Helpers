# Vestigium.Helpers.WinReg — Requirements Specification

**Document ID:** VEST-HLP-WINREG-SRS-000  
**Version:** 1.0  
**Status:** Draft — first lossless revision. Replaces the Probe-only skeleton.  
**Date:** 12 September 2026  
**Package:** `Vestigium.Helpers.WinReg`  
**Project ID:** HLP-REG  
**TFM:** `net10.0-windows` (.NET 10 LTS) — **Windows-only**  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Cousins:** Services (remote SCM), Processes (no registry), Kql (not referenced in v1)  
**Hosts:** later Vestigium solution. The WPF gallery is **not** in scope for this SRS.

If implementation and this file disagree, this file wins.

This package is a **library of resources**, not regedit and not a shell. Hosts subscribe. It talks to the Windows Registry API. It does **not** spawn `reg.exe`, `regedit.exe`, `regedt32.exe`, `powershell.exe`, or `cmd.exe`.

---

## 0. How to read this document

It records:

- one façade (`RegistryHelper`) for identity, Probe, connect, CRUD, search, export, import, hive mount / dismount
- one immutable **key snapshot** (`RegistryKeyInfo`) and **value snapshot** (`RegistryValueInfo`)
- local hives and **remote** hives on a named machine
- 32-bit and 64-bit views
- HelperLog only; Category `Helpers`; APPID `WinReg`
- partial data: a key the token cannot open is Denied, not invented

regedit is the *column reference*. It is not spawned and it is not a dependency.

---

## 1. Purpose

Give every Vestigium host one way to:

1. Read and write the registry on this box and on a remote box.
2. Create, read, update, and delete keys and values.
3. Export a key tree to a `.reg` file without `reg.exe`.
4. Import a `.reg` file **silently** without `reg.exe` and without a UI.
5. Mount a hive file under a key and dismount it without `reg.exe`.

Typical hosts: ProbeHost checking a product key; an operator editing a remote HKLM policy; a forensic pass that loads an offline `SYSTEM` hive, reads it, then unloads it.

```csharp
var local = RegistryHelper.Local;
var box  = RegistryHelper.For("BOX01");

var value = box.GetValue(RegistryHiveKind.LocalMachine, @"SOFTWARE\Vestigium", "InstallPath");
box.SetValue(RegistryHiveKind.LocalMachine, @"SOFTWARE\Vestigium", "InstallPath", @"C:\Vestigium", confirm: true);

RegistryHelper.Export(path: @"C:\Temp\vest.reg", hive: RegistryHiveKind.CurrentUser, key: @"Software\Vestigium");
RegistryHelper.Import(path: @"C:\Temp\vest.reg", confirm: true);   // silent

using var mounted = RegistryHelper.MountHive(
    hiveFile: @"D:\offline\SYSTEM",
    destination: RegistryHiveKind.LocalMachine,
    subKey: @"OFFLINE_SYSTEM",
    confirm: true);
// ... read mounted.GetValue(...)
// Dispose / DismountHive unloads
```

---

## 2. Architectural constraints (binding)

| ID | Constraint |
|---|---|
| A1 | Class library `net10.0-windows`. No WPF. Demo gallery is a separate host and is **not** in this SRS. |
| A2 | Independently referenced. Core `Vestigium.Helpers` for guards and `HelperLog` only. Kql / Processes / Services / Json are **not** referenced in v1. |
| A3 | The library never calls `VestigiumLogger.Initialize`. It never chooses a log folder. |
| A4 | Public work lives on `RegistryHelper` plus `RegistryClient` (local or remote) plus immutable snapshots. `Microsoft.Win32.RegistryKey` and P/Invoke stay internal. |
| A5 | **No tool spawn.** No `reg export`. No `reg import`. No `reg load`. No `reg unload`. No `regedit /s`. |
| A6 | Destructive writes (create key, set value, delete, import, mount, dismount) require `confirm: true`. `confirm: false` returns Denied and does not touch the hive. |
| A7 | Privileged keys the token cannot open are missing / Denied. List does not throw because one subkey refused. Throw only on contract violations (blank path, bad hive file path, interval out of range if a watcher is added later). |
| A8 | Passwords, product keys, and binary secrets passed as values are **never** written to HelperLog or to any JSONL this library owns. Log the key path and value **name**, not the value data. |
| A9 | Tests write only under `HKCU\Software\Vestigium\Helpers.Tests\*` or a temp hive file in `%TEMP%`. Tests must not delete `HKLM\SYSTEM`, `HKLM\SOFTWARE`, or any machine policy key. |
| A10 | Linux / macOS wine registry is **out of v1**. |
| A11 | Registry transactions (`RegCreateKeyTransacted`) are **out of v1**. |
| A12 | Changing ACLs / owner is **out of v1** (read of a DACL summary may appear later; not required to ship CRUD). |
| A13 | Alternate credentials on the remote connect are **out of v1**. Remote uses the current Windows token (`OpenRemoteBaseKey` / `RegConnectRegistry`). Same rule as Services Phase-remote. |

---

## 3. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | TFM | `net10.0-windows`. |
| 2 | Façade | `RegistryHelper` + `RegistryClient`. `RegistryHelper.Local` and `RegistryHelper.For(machine)`. `.` / `localhost` / this PC name normalize to local. |
| 3 | Remote | `RegistryKey.OpenRemoteBaseKey` / `RegConnectRegistry`. Remote Registry service must be running on the target. Missing host → connect fails, List is empty, Get is null, writes are Denied. |
| 4 | Views | Every open takes `RegistryViewKind` (`Default`, `Registry64`, `Registry32`). Default is `Default` (native view of this process). |
| 5 | Path form | Hive enum + key path **without** the hive prefix. Example: hive `LocalMachine`, key `SOFTWARE\Vestigium`. Leading `\` is stripped. `/` is not accepted (reject). |
| 6 | Value types | `None`, `String`, `ExpandString`, `Binary`, `DWord`, `MultiString`, `QWord`, `Unknown`. ExpandString is stored as expand-string; Get may optionally expand. Default Get does **not** expand. |
| 7 | CRUD result | Writes return `RegistryWriteResult` (`Ok`, `Denied`, `NotFound`, `InvalidPath`, `TypeMismatch`, `InUse`, `Unsupported`). They do not throw on Access Denied. |
| 8 | Export format | Windows Registry Editor Version **5.00** Unicode `.reg`. Also a binary hive export via `RegSaveKeyEx` for a single key tree (offline-readable). Host picks `RegFile` or `HiveFile`. |
| 9 | Import | Silent. Parses 5.00 `.reg` (and 4.00 ANSI if the file declares it). Applies under the hives named in the file. No UI. `confirm: true` required. Partial failure stops and returns the first failing line; already-applied keys are **not** rolled back (no transactions). |
| 10 | Mount | `RegLoadKey` under `HKLM` or `HKU` only (Win32 rule). Requires backup/restore privilege enablement inside the library for the call, then revert. `confirm: true`. |
| 11 | Dismount | `RegUnLoadKey`. Flush first. `confirm: true`. Dispose of `IRegistryMount` unloads if still loaded. |
| 12 | Search | `StartsWith` / `EndsWith` / `Contains` on key name, value name, and optional string value data. Depth cap default 16, max 32. Result cap 256. |
| 13 | Probe | Local only. Opens `HKCU\Software\Vestigium` if present (creates nothing). Does not write. |
| 14 | Logging | Sparse: connect, list count, create/set/delete (path + name only), export path, import path + line count, mount / dismount path. No value payloads. |

---

## 4. Glossary (binding language)

| Term | Meaning |
|---|---|
| **Hive** | `ClassesRoot`, `CurrentUser`, `LocalMachine`, `Users`, `CurrentConfig`. `PerformanceData` is out of v1. |
| **Key path** | Subkey under a hive. `SOFTWARE\Vestigium`. Not `HKLM\SOFTWARE\Vestigium`. |
| **Value** | Named datum under a key. The default value has name `""` (empty string), exposed as `IsDefault = true`. |
| **View** | 64-bit vs 32-bit redirected node (`Wow6432Node`). |
| **Snapshot** | Immutable values captured at one instant. Not a live `RegistryKey` leaked to the caller. |
| **Remote** | Another machine’s registry via RPC. |
| **Mount** | Load a hive file onto a live key (`RegLoadKey`). |
| **Dismount** | Unload that key (`RegUnLoadKey`). |
| **Export** | Write `.reg` or a hive file from a live key tree. |
| **Import** | Apply a `.reg` to the live registry with no UI. |
| **Confirm** | Required boolean on every write, import, mount, dismount. |
| **Silent** | No shell, no regedit dialog, no `MessageBox`. |

These words are not interchangeable.

---

## 5. Goals

**G1.** One façade owns Probe, connect, CRUD, search, export, import, mount, dismount.  
**G2.** Local and remote use the same `RegistryClient` methods.  
**G3.** Create / read / update / delete keys and values with typed results.  
**G4.** Export `.reg` (5.00) and save hive files without `reg.exe`.  
**G5.** Import `.reg` silently without `reg.exe`.  
**G6.** Mount and dismount hive files without `reg.exe`.  
**G7.** Logs meet ALCOA+ through HelperLog. Value data never appears in logs.  
**G8.** Tests never touch live machine policy keys.

---

## 6. Snapshots

### 6.1 Key (`RegistryKeyInfo`)

| Field | Type | Notes |
|---|---|---|
| `Machine` | `string?` | Null on local. |
| `Hive` | `RegistryHiveKind` | |
| `Path` | `string` | Key path without hive prefix. Empty string = hive root. |
| `Name` | `string` | Last segment. |
| `View` | `RegistryViewKind` | |
| `SubKeyCount` | `int?` | |
| `ValueCount` | `int?` | |
| `LastWriteTime` | `DateTimeOffset?` | When queryable. |
| `SubKeyNames` | `IReadOnlyList<string>` | Empty list, not null. |
| `Values` | `IReadOnlyList<RegistryValueInfo>` | Present on Full; empty on Identity. |
| `Availability` | `IReadOnlyList<RegistryFieldAvailability>` | |

### 6.2 Value (`RegistryValueInfo`)

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Empty string = default value. |
| `IsDefault` | `bool` | |
| `Type` | `RegistryValueKind` | |
| `Data` | `object?` | `string`, `string[]`, `int`, `long`, `byte[]`. Never a live handle. |
| `DataText` | `string?` | Display form. Binary is hex. Multi-string is joined with `\0` escaped. |

### 6.3 Slim vs full

| Level | Includes |
|---|---|
| `Identity` | Machine, Hive, Path, Name, View |
| `Slim` | Identity + counts + subkey names |
| `Full` | Slim + values + last write |

Default `List` / `GetKey` = Slim. Default `GetValue` reads one value.

---

## 7. Connect and inventory

```text
RegistryHelper.Local                          -> RegistryClient  (this box)
RegistryHelper.For(string machine)            -> RegistryClient
RegistryHelper.CanConnect(string machine)     -> bool

RegistryClient.ListSubKeys(hive, key, view = Default, level = Slim)
RegistryClient.GetKey(hive, key, view = Default, level = Slim)
RegistryClient.TryGetKey(...)
RegistryClient.GetValue(hive, key, valueName, view = Default, expand: false)
RegistryClient.TryGetValue(...)
```

`GetKey` / `GetValue` return null when the name does not exist. They do not throw for Gone.

Remote connect failure: `CanConnect` is false. `ListSubKeys` returns empty. Writes return Denied with reason `OpenRemoteBaseKey`.

---

## 8. CRUD

```text
RegistryWriteResult CreateKey(hive, key, view = Default, confirm = false)
RegistryWriteResult DeleteKey(hive, key, recursive = false, view = Default, confirm = false)
RegistryWriteResult SetValue(hive, key, valueName, data, kind = String, view = Default, confirm = false)
RegistryWriteResult DeleteValue(hive, key, valueName, view = Default, confirm = false)
```

| Rule | Behavior |
|---|---|
| `confirm: false` | Denied. No write. |
| Missing parent on CreateKey | Create the missing chain (same as regedit New Key). |
| DeleteKey `recursive: false` | Denied / InvalidPath if the key has subkeys. |
| DeleteKey `recursive: true` | Deletes the tree. Still needs confirm. |
| SetValue on missing key | CreateKey then set. |
| TypeMismatch | SetValue kind does not match `data` CLR type (e.g. DWord + string). |
| Protected test list | Tests never delete under `HKLM\SYSTEM`, `HKLM\SOFTWARE\Microsoft`, `HKLM\SAM`, `HKLM\SECURITY`. |

`RegistryWriteResult` carries `Status`, `Hive`, `Path`, `ValueName`, `Reason`.

---

## 9. Search

```text
IReadOnlyList<RegistryHit> Search(
    hive,
    key,                       // start path
    string term,
    RegistrySearchMode mode,   // StartsWith / EndsWith / Contains
    RegistrySearchFields fields = KeyName | ValueName,
    int maxDepth = 16,
    int maxResults = 256,
    view = Default)
```

Hits name the hive, path, value name (if a value matched), and a slim key snapshot. Searching value **data** is opt-in (`RegistrySearchFields.ValueData`) and only considers String / ExpandString / MultiString. Binary is not scanned in v1.

---

## 10. Export (no reg.exe)

```text
RegistryWriteResult Export(
    string path,
    RegistryHiveKind hive,
    string key,
    RegistryExportFormat format = RegFile,
    RegistryViewKind view = Default,
    confirm = false)
```

| Format | File |
|---|---|
| `RegFile` | Unicode `.reg`, header `Windows Registry Editor Version 5.00`. Hex for binary. `hex(2)` expand-string. `hex(7)` multi-string. DWord as `dword:`. QWord as `hex(b)`. |
| `HiveFile` | `RegSaveKeyEx` binary hive. Needs backup privilege for some keys. |

Export of a missing key is NotFound. Existing destination file is overwritten only when `confirm: true` (v1 does not invent a second flag).

Remote export: allowed. The file is written on **this** box from data read over RPC.

---

## 11. Import silent (no reg.exe)

```text
RegistryWriteResult Import(
    string path,
    RegistryViewKind view = Default,
    confirm = false)
```

- No UI. No `regedit /s`.
- Accepts 5.00 Unicode and 4.00 ANSI `.reg`.
- Applies keys in file order.
- `-` prefix on a key or value in `.reg` means delete (same as Microsoft `.reg` grammar).
- First failure returns Denied / InvalidPath with line number in `Reason`. Prior lines stay applied.
- Hive files are **not** imported by this method. Use `MountHive` for a `.hiv` / `SYSTEM` file.

---

## 12. Mount and dismount (no reg.exe)

```text
IRegistryMount MountHive(
    string hiveFile,
    RegistryHiveKind destination,   // LocalMachine or Users only
    string subKey,
    confirm = false)

RegistryWriteResult DismountHive(
    RegistryHiveKind destination,
    string subKey,
    confirm = false)
```

```text
interface IRegistryMount : IDisposable
{
    string HiveFile { get; }
    RegistryHiveKind Destination { get; }
    string SubKey { get; }
    bool IsLoaded { get; }
    RegistryClient Client { get; }   // reads/writes under the mounted key
}
```

| Rule | Behavior |
|---|---|
| Destination | `LocalMachine` or `Users` only. Other hives → Unsupported. |
| Privilege | Library enables `SeBackupPrivilege` and `SeRestorePrivilege` for the load/unload call, then reverts. Failure → Denied. |
| In use | Load of a path already mounted → InUse. |
| Missing file | NotFound. |
| Dispose | Unloads if still loaded. Second unload is Ok / already dismounted. |
| Remote mount | **Out of v1.** Mount is local only. Hosts copy the hive file here first. |

This is `RegLoadKey` / `RegUnLoadKey`, not `reg load`.

---

## 13. Logging and Probe

```text
string Identity { get; }   // "Vestigium.Helpers.WinReg"
string Probe()
```

Probe opens HKCU read-only and returns Identity. It does not create keys.

HelperLog APPID `WinReg`. Lines name hive + path + value **name**. They never contain value bytes or passwords.

---

## 14. Out of v1

| Item | Why |
|---|---|
| `reg.exe` / `regedit.exe` | Binding. |
| Alternate credentials on remote | Same deferral as Services remote. |
| Remote mount | LoadKey is local. |
| ACL write / take ownership | Wrong blast radius for v1. |
| Transactions / rollback of Import | Win32 transactions are a later phase. |
| KQL pack for registry | Add when a host actually queries `REG.*` daily. Search modes cover v1. |
| Campaigns / watchers | Host can poll GetKey. |
| PerformanceData hive | Not a configuration store. |
| AppX / per-user Classes overlay tricks | Out of scope. |

---

## 15. Acceptance gates (paper)

1. `GetValue` / `SetValue` / `DeleteValue` / `CreateKey` / `DeleteKey` work on HKCU under the test root with `confirm: true`.
2. `confirm: false` never writes.
3. `For(".")` is local. `For("no-such-host")` CanConnect is false.
4. Export writes a 5.00 `.reg` that Import applies silently. Process Explorer / regedit are not used.
5. Mount a copy of a test hive file under `HKLM\VESTIGIUM_TEST_*`, read a value, Dismount. `reg.exe` is not used.
6. Logs after SetValue do not contain the value data.
7. Tests never delete `HKLM\SYSTEM`.

---

## 16. Suggested implementation phases (not this document’s scoreboard)

A separate Implementation Plan should track:

| Phase | Goal |
|---|---|
| 0 | This SRS locked. |
| 1 | Local read: GetKey / GetValue / ListSubKeys / views. |
| 2 | Local CRUD + confirm. |
| 3 | Remote `For(machine)` + CanConnect. |
| 4 | Export `.reg` + HiveFile. |
| 5 | Silent Import `.reg`. |
| 6 | Mount / Dismount hive files. |
| 7 | Search + harden (no secrets in logs, tests under HKCU / temp hive). |
