# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.5  
**Status:** Phase 0 Locked. Phases 1–4 Done. Phase 5 of 8 in flight.  
**Date:** 12 September 2026  
**Package:** `Vestigium.Helpers.Services`

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. | **Locked** |
| **1 List / Get / Search** | Visible Win32. StartsWith / EndsWith / Contains. | **Done** |
| **2 Full row** | Config, account, image path, delayed, failure actions (read) | **Done** |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by | **Done** |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | **Done** |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon rights | **In progress** |
| **6 Recovery** | First / second / subsequent + reset period | Planned |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests, no password in logs, guide matches engine | Planned |

---

## 2. Phase 5 scope

```text
ServiceHelper.SetLogon(name, ServiceLogonRequest)
ServiceHelper.QueryLogonRights(account)
ServiceHelper.GrantLogonRights(account, Service | Batch | ServiceAndBatch)
```

| Rule | Behavior |
|---|---|
| `Confirm = false` | Denied. No SCM write. |
| Protected name | Denied. EventLog never changes account. |
| Kind = LocalSystem | Account `LocalSystem`, empty password. |
| InteractWithDesktop | LocalSystem only. Own-process only. Shared-process → InvalidState. |
| Kind = LocalService / NetworkService | Built-in names. Empty password. |
| Kind = Account | Account required. Password accepted, never logged, never returned. |
| GrantLogonRight | Opt-in. Default None. `SeServiceLogonRight` / `SeBatchLogonRight`. |
| Read | `Get` Account + DesktopInteract. No Password property anywhere. |

Logs write `password=***` only.

---

## 3. Close gates

1. SetLogon without confirm is Denied.
2. SetLogon EventLog with confirm is Denied / protected.
3. InteractWithDesktop + Account kind is InvalidState.
4. Account kind with blank account throws ArgumentException.
5. QueryLogonRights on `NT AUTHORITY\SYSTEM` does not throw.
6. Grant None does not add rights.
7. ServiceInfo and ServiceControlResult have no Password property.

---

## 4. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase5
```
