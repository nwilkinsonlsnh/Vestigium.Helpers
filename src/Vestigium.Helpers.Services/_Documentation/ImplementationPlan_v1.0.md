# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.1  
**Status:** Phase 0 **Locked**. Phase 1 of 8 in flight.  
**Date:** 11 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**TFM:** `net10.0-windows`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins after the locked deltas in §2. If this file and working code disagree, change the code.

Demo gallery is **out** of all eight phases.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. Lock TFM, verbs, logon, recovery, hidden. | **Locked** |
| **1 List / Get / Search** | Visible Win32 rows. StartsWith / EndsWith / Contains. Probe. | **In progress** |
| **2 Full row** | Config, account, image path, delayed auto-start, failure actions (read) | Planned |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by walk | Planned |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | Planned |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon-as-service / batch | Planned |
| **6 Recovery** | First / second / subsequent + reset period | Planned |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests ≥ coverage bar, no password in logs, guide matches engine | Planned |

Later-phase types may already exist in the tree. A phase is **Done** only when its close gate in §4 passes.

---

## 2. Phase 0 — locked decisions

Accepted 11 September 2026.

| Item | Locked as |
|---|---|
| TFM | `net10.0-windows`. SCM is Windows-only. |
| Façade | `ServiceHelper`. Snapshots are immutable. No `sc.exe` / `net.exe` / WMI control path. |
| Visible list | `List()` = services.msc Win32 (`ServiceController.GetServices()`). |
| Hidden list | `ListHidden()` = `HKLM\SYSTEM\CurrentControlSet\Services` keys that look like a service/driver and are **not** in the visible Win32 set. |
| Combined list | `List(..., scope: All)` returns both and sets `IsHidden`. |
| Search (Phase 1) | `StartsWith` / `EndsWith` / `Contains`, case-insensitive ordinal. Fields: Name, DisplayName, Description, ImagePath. Cap 256. |
| KQL search | Phase 7. `KqlPack.Service`. |
| Start / Stop / Restart | Phase 4. Typed `ServiceControlResult`. No throw on Access Denied. |
| Pause / Continue | Phase 4. `Unsupported` when the service does not accept pause/continue. |
| Set startup type | Phase 4. `confirm: true`. AutomaticDelayed supported. |
| Read Log On account | Phase 2 read / Phase 5 write. Password never returned. |
| Set Log On = Local System + interact with desktop | Phase 5. Desktop interact requires LocalSystem. |
| Set Log On = account + password | Phase 5. Password never logged, never JSONL, never kept. |
| Grant logon rights | Phase 5 **opt-in**. `SeServiceLogonRight` and/or `SeBatchLogonRight`. Default is do not touch policy. |
| Recovery | Phase 6. First / second / subsequent + reset fail count after *n* days. |
| Dependency walk | Phase 3. Cycle guard. |
| Process join | Slim `ProcessHelper.Get(pid)` when running. Shared-process CPU is not attributed to one service. |
| Create / Delete / change binary path | **Out of v1.** |
| Demo | **Out of these eight phases.** |
| Campaigns | Phase 7. |
| Tests | Never stop EventLog, RpcSs, DcomLaunch, LSM, PlugPlay, ProfSvc, SamSs, Schedule, Winmgmt, CryptSvc. |

---

## 3. Phase 1 scope

Public surface that must work:

```text
ServiceHelper.Identity
ServiceHelper.Probe()
ServiceHelper.List(level = Slim, kind = Win32, scope = Visible)
ServiceHelper.Get(name, level = Full)
ServiceHelper.TryGet(name, out info)
ServiceHelper.Search(term, StartsWith | EndsWith | Contains, fields, level, scope, maxResults)
```

`List()` default is visible Win32 only. Stopped services are included. One unreadable service does not fail the table.

`Get` is case-insensitive on the SCM name. Missing name → `null`, not throw.

Search cap is 256. Above that → `ArgumentException`. Blank term → `ArgumentException`.

---

## 4. Close gates

### Phase 1

1. `Probe()` returns `Vestigium.Helpers.Services` and does not start or stop anything.
2. `List()` is non-empty on a standard workstation and contains `EventLog`.
3. Every `List()` row has `IsHidden == false`.
4. `Get("EventLog")` returns Name + DisplayName + a real Status.
5. `Get("NoSuchService_Vestigium")` returns null.
6. `TryGet(" ")` is false.
7. Search Contains `spool` hits Print Spooler by name or display name.
8. Search StartsWith / EndsWith against `EventLog` each return at least one row.
9. Search `maxResults: 300` throws `ArgumentException`.
10. Project builds as `net10.0-windows`.

### Later phases

Written when that phase starts. Do not treat control / logon / recovery as Phase 1 work.

---

## 5. Commands

```
dotnet build src/Vestigium.Helpers.Services/Vestigium.Helpers.Services.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase1
```
