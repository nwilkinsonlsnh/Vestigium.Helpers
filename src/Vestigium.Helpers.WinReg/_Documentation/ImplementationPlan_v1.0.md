# Vestigium.Helpers.WinReg — Phase Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-000  
**Version:** 1.1  
**Status:** Phase 0 Locked. Phase 1 of 7 in flight.  
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
| **0 Paper** | SRS + this plan. | **Locked** |
| **1 Local read** | GetKey / GetValue / ListSubKeys / views | **In progress** |
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

Locked. Public names: `RegistryHelper`, `RegistryClient`, `RegistryKeyInfo`, `RegistryValueInfo`, `RegistryWriteResult`, `IRegistryMount`.

---

## 4. Phase 1 — Local read

Shipped surface:

```text
RegistryHelper.Identity / Probe() / Local / For(machine) / CanConnect(machine)
RegistryClient.GetKey / TryGetKey / GetValue / TryGetValue / ListSubKeys
```

Close gates: Probe identity; missing GetKey is null; `/` throws; HKCU Software list not empty; Registry32 view does not throw; GetValue on HKCU Environment\Path is null or a string.

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryPhase1
```

---

## 5. Phase 2 — Local CRUD

CreateKey / DeleteKey / SetValue / DeleteValue with confirm. TypeMismatch. Recursive delete. Logs never contain value payload.

---

## 6. Phase 3 — Remote

`For(machine)` already exists; Phase 3 hardens CanConnect on a missing host and remote List/Get. No alt creds. No remote mount.

---

## 7. Phase 4 — Export

`.reg` 5.00 + HiveFile via `RegSaveKeyEx`. File lands on this box.

---

## 8. Phase 5 — Silent import

Parse 5.00 / 4.00 `.reg`. Delete prefix. Line number on first failure. No rollback.

---

## 9. Phase 6 — Mount / dismount

`RegLoadKey` / `RegUnLoadKey` on HKLM or HKU. Privilege toggle. Dispose unloads. Local only.

---

## 10. Phase 7 — Search + harden

StartsWith / EndsWith / Contains. Cap 256. No secrets on snapshots. Guide matches engine.
