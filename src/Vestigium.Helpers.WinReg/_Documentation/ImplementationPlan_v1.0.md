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

## 2. Phase 1 commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryPhase1
```

Shared rules, later phases, and file map are unchanged from v1.0.
