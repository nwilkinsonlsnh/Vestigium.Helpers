# Vestigium.Helpers.WinReg — Phase Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-000  
**Version:** 1.0  
**Status:** Phase 0 ready to lock. Phases 1–7 not started.  
**Date:** 12 September 2026  
**Package:** `Vestigium.Helpers.WinReg`  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**TFM:** `net10.0-windows`

If this plan and the SRS disagree, the SRS wins. Demo gallery stays out.

Engine rule: **no `reg.exe`, no `regedit.exe`, no PowerShell.** Advapi32 + `Microsoft.Win32.RegistryKey` only.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. | **Ready to lock** |
| **1 Local read** | GetKey / GetValue / ListSubKeys / views | Planned |
| **2 Local CRUD** | CreateKey / SetValue / DeleteKey / DeleteValue + confirm | Planned |
| **3 Remote** | `For(machine)` + CanConnect | Planned |
| **4 Export** | `.reg` 5.00 + HiveFile (`RegSaveKeyEx`) | Planned |
| **5 Import** | Silent `.reg` apply | Planned |
| **6 Mount / dismount** | `RegLoadKey` / `RegUnLoadKey` | Planned |
| **7 Search + harden** | Search modes, no secrets in logs, test root only | Planned |

---

## 2. Shared rules (every phase)

| Rule | Behavior |
|---|---|
| Test root | `HKCU\Software\Vestigium\Helpers.Tests\*` or a hive file under `%TEMP%\vest-winreg-*`. |
| Forbidden | Never delete or rewrite `HKLM\SYSTEM`, `HKLM\SOFTWARE\Microsoft`, `HKLM\SAM`, `HKLM\SECURITY`. |
| Confirm | Every write / import / mount / dismount needs `confirm: true`. False → Denied, no touch. |
| Logs | Hive + path + value **name**. Never value bytes, never product keys. |
| Views | `Default` / `Registry64` / `Registry32` on every open. |
| Path | Hive enum + key without `HKLM\` prefix. `/` rejected. |
| Results | Writes return `RegistryWriteResult`. Access Denied is a status, not a throw. |
| Hooks | `RegistryTestHooks` for temp paths only. Tests never write live ProgramData. |

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryPhase
```

---

## 3. Phase 0 — Paper

**In**

- SRS v1.0 locked as written.
- This plan accepted.
- Public names frozen: `RegistryHelper`, `RegistryClient`, `RegistryKeyInfo`, `RegistryValueInfo`, `RegistryWriteResult`, `IRegistryMount`.

**Out**

- Code beyond the existing Probe stub.

**Close**

1. SRS and this file are in `_Documentation/`.
2. No `reg.exe` anywhere in the plan.

---

## 4. Phase 1 — Local read

**In**

```text
RegistryHelper.Identity
RegistryHelper.Probe()
RegistryHelper.Local
RegistryClient.GetKey(hive, key, view, level)
RegistryClient.TryGetKey(...)
RegistryClient.GetValue(hive, key, valueName, view, expand: false)
RegistryClient.TryGetValue(...)
RegistryClient.ListSubKeys(hive, key, view, level)
```

- Hives: ClassesRoot, CurrentUser, LocalMachine, Users, CurrentConfig.
- Levels: Identity / Slim / Full.
- Default value name `""` → `IsDefault = true`.
- ExpandString stored as-is unless `expand: true`.

**Out**

- Writes, remote, export, import, mount, search.

**Close**

1. Probe returns `Vestigium.Helpers.WinReg` and does not create keys.
2. GetKey missing path is null. Blank path throws ArgumentException.
3. GetValue on a known HKCU test value (tests may create it in Phase 2; Phase 1 can read a well-known HKCU name or skip live write).
4. ListSubKeys of HKCU `Software` is not empty.
5. Registry32 view on a 64-bit process does not throw.
6. `/` in a key path throws ArgumentException.

---

## 5. Phase 2 — Local CRUD

**In**

```text
CreateKey(hive, key, view, confirm)
DeleteKey(hive, key, recursive, view, confirm)
SetValue(hive, key, valueName, data, kind, view, confirm)
DeleteValue(hive, key, valueName, view, confirm)
```

- Missing parents on CreateKey / SetValue are created.
- DeleteKey `recursive: false` refuses a key that still has subkeys.
- TypeMismatch when kind and CLR type disagree.

**Out**

- Remote, export, import, mount.

**Close**

1. `confirm: false` on SetValue leaves the value unchanged (or absent).
2. SetValue + GetValue round-trips String, DWord, QWord, MultiString, Binary, ExpandString.
3. DeleteValue removes the name. GetValue after that is null.
4. DeleteKey recursive removes the test tree. Non-recursive on a parent with children is Denied / InvalidPath.
5. HelperLog after SetValue does not contain the payload string used in the test.

---

## 6. Phase 3 — Remote

**In**

```text
RegistryHelper.For(machine)
RegistryHelper.CanConnect(machine)
RegistryHelper.CanConnect(machine, out reason)
```

- `.` / `localhost` / `Environment.MachineName` → local (`Machine` null).
- Remote Registry service + current token.
- Same CRUD/read methods on `RegistryClient`.

**Out**

- Alternate credentials.
- Remote mount.

**Close**

1. `For(".")` and `For(Environment.MachineName)` are `IsLocal`.
2. `CanConnect(".")` is true.
3. `CanConnect("no-such-host-vestigium-xyz")` is false.
4. `For(".").ListSubKeys(CurrentUser, "Software")` is not empty.
5. `For("no-such-host-vestigium-xyz").GetKey(...)` is null. Writes return Denied.

---

## 7. Phase 4 — Export

**In**

```text
Export(path, hive, key, format = RegFile | HiveFile, view, confirm)
```

- RegFile: Unicode 5.00 `.reg`. `dword:`, `hex:`, `hex(2)`, `hex(7)`, `hex(b)`.
- HiveFile: `RegSaveKeyEx`. Enable backup privilege for the call, then revert.
- File is written on **this** box even when the client is remote.
- `confirm: true` required. Existing file overwritten only with confirm.

**Out**

- Import (Phase 5).

**Close**

1. Export of the test key writes a file that starts with `Windows Registry Editor Version 5.00`.
2. File contains the test value name.
3. Export missing key is NotFound.
4. Export `confirm: false` does not create the file.
5. HiveFile export of the test key produces a non-empty file. No `reg.exe` process is started.

---

## 8. Phase 5 — Silent import

**In**

```text
Import(path, view, confirm)
```

- Parse 5.00 Unicode and 4.00 ANSI `.reg`.
- `-` key / value prefix deletes.
- First failing line stops. Prior lines stay applied. Line number in `Reason`.
- Hive files are not Import — that is Mount.

**Out**

- Transactions / rollback.

**Close**

1. Export then Import of the test tree restores values without UI.
2. `confirm: false` does not change the registry.
3. A `.reg` delete line (`[-HKCU\...]` or `"Name"=-`) removes that key or value.
4. Bad line reports line number. No `regedit` / `reg.exe`.

---

## 9. Phase 6 — Mount / dismount

**In**

```text
MountHive(hiveFile, destination, subKey, confirm) -> IRegistryMount
DismountHive(destination, subKey, confirm)
```

- Destination `LocalMachine` or `Users` only.
- Enable `SeBackupPrivilege` + `SeRestorePrivilege` for the API call, then revert.
- Dispose unloads if still loaded.
- Local only.

**Close**

1. Mount of a Phase-4 HiveFile under `HKLM\VESTIGIUM_TEST_<guid>` succeeds with confirm (or Denied if the token lacks restore — test asserts either Ok or Denied, never a hang).
2. After Ok, GetValue under the mount reads the exported test value.
3. Dismount / Dispose unloads. Second dismount is Ok.
4. Mount missing file is NotFound. Destination CurrentUser is Unsupported.
5. `confirm: false` does not load.
6. No `reg.exe`.

If the CI token cannot load hives, mark those facts `[Trait("Privilege","Restore")]` and keep the Denied path covered.

---

## 10. Phase 7 — Search + harden

**In**

```text
Search(hive, key, term, mode, fields, maxDepth = 16, maxResults = 256, view)
```

- Modes: StartsWith / EndsWith / Contains.
- Fields: KeyName, ValueName, ValueData (strings only).
- Depth cap 32. Result cap 256. Over cap → ArgumentException.
- Guide matches `RegistryHelper` / `RegistryClient`.
- No Password / secret property on snapshots. SetValue payload never logged.

**Close**

1. Search Contains on the test value name hits.
2. ValueData search finds a String value and does not scan Binary.
3. maxResults 300 throws.
4. `RegistryKeyInfo` / `RegistryValueInfo` / `RegistryWriteResult` have no Password property.
5. Developers guide lists the same public surface as the engine.
6. Full `RegistryPhase` filter is green.

---

## 11. Suggested file map

| File | Role |
|---|---|
| `RegistryHelper.cs` | Façade: Identity, Probe, Local, For, CanConnect, Export, Import, MountHive |
| `RegistryClient.cs` | Bound hive operations |
| `RegistrySnapshots.cs` | Key / value / write result / enums |
| `RegistryNative.cs` | RegLoadKey, RegUnLoadKey, RegSaveKeyEx, privilege adjust |
| `RegistryRegFile.cs` | 5.00 / 4.00 parse and emit |
| `RegistrySearch.cs` | Walk + match |
| `RegistryTestHooks.cs` | Temp root only |
| `RegistryPhaseNTests.cs` | One file per phase |

---

## 12. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryPhase1
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryPhase
```
