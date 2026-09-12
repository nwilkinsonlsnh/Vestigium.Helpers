# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.0  
**Status:** Open. Phases 1-6 implemented in first drop.  
**Date:** 11 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins after the v1.1 deltas in §2. If this file and working code disagree, change the code.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. Accept logon / recovery / hidden. TFM `net10.0-windows`. | **Done** |
| **1 List / Get / Search** | Visible Win32 rows, StartsWith / EndsWith / Contains | **Done** |
| **2 Full row** | Config, account, image path, delayed, failure actions (read) | **Done** |
| **3 Hidden + tree** | Registry-vs-SCM hidden list; depends-on / depended-by walk | **Done** |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | **Done** |
| **5 Logon** | LocalSystem, interact-with-desktop, account+password, SeServiceLogonRight / SeBatchLogonRight | **Done** |
| **6 Recovery** | First / second / subsequent failure + reset period | **Done** |
| **7 Watch + KQL + campaigns** | Watcher shipped. Campaign JSONL still planned. | **Partial** |
| **8 Harden** | Tests shipped read-only. Guide polish remaining. | **Partial** |

Demo gallery is **out** of this plan.

---

## 2. v1.1 deltas vs the 11 Sep SRS draft

| Item | Locked as |
|---|---|
| TFM | `net10.0-windows` |
| Set startup type | In v1. `confirm: true`. Includes AutomaticDelayed. |
| Start / Stop / Restart | In v1. Typed `ServiceControlResult`. |
| Pause / Continue | In v1 when the service accepts pause/continue. Otherwise `Unsupported`. |
| Read Log On account | In v1. Password is never returned. |
| Set Log On = Local System | In v1. Optional `InteractWithDesktop`. |
| Set Log On = account + password | In v1. Password never logged, never JSONL, never kept. |
| Grant log on as a service / batch | In v1 as an **opt-in** on the logon request. |
| Recovery options | In v1. First / second / subsequent + reset fail count after *n* days. |
| Dependency walk | In v1. Cycle guard. |
| Hidden services | `List()` = services.msc Win32. `ListHidden()` = registry keys SCM Win32 enum does not return. `List(All)` = union with `IsHidden`. |
| Create / Delete / change binary path | Still **out** of v1. |
| Demo | Still **out**. |

---

## 3. Commands

```
dotnet build src/Vestigium.Helpers.Services/Vestigium.Helpers.Services.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Service
```
