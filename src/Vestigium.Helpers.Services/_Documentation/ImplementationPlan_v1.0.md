# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.4  
**Status:** Phase 0 Locked. Phases 1–3 Done. Phase 4 of 8 in flight.  
**Date:** 11 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**TFM:** `net10.0-windows`

Demo gallery is **out** of all eight phases.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. | **Locked** |
| **1 List / Get / Search** | Visible Win32. StartsWith / EndsWith / Contains. | **Done** |
| **2 Full row** | Config, account, image path, delayed, failure actions (read) | **Done** |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by | **Done** |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | **In progress** |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon rights | Planned |
| **6 Recovery** | First / second / subsequent + reset period | Planned |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests, no password in logs, guide matches engine | Planned |

---

## 2. Phase 4 scope

Typed `ServiceControlResult`. No throw on Access Denied, timeout, or missing name.

| Verb | Rules |
|---|---|
| Start | Already running → Ok. Disabled → InvalidState. Missing → NotFound. |
| Stop | Protected name → Denied. Dependents and `confirmDependents=false` → HasDependents. Cannot stop → Unsupported. |
| Restart | Stop then Start. Protected → Denied before Stop. |
| Pause / Continue | `CanPauseAndContinue == false` → Unsupported. Wrong state → InvalidState. |
| SetStartType | `confirm: false` → Denied. Protected → Denied. Boot/System → Unsupported. AutomaticDelayed writes Automatic + delayed flag. |

Protected names (never Stop / Restart / SetStartType): EventLog, RpcSs, DcomLaunch, LSM, PlugPlay, ProfSvc, SamSs, Schedule, Winmgmt, CryptSvc, and a short companion list on `ServiceHelper.ProtectedNames`.

Create / Delete / change binary path stay out of v1.

---

## 3. Close gates

1. `Stop("EventLog")` is Denied / protected. EventLog stays running.
2. `Restart("EventLog")` is Denied.
3. `SetStartType(..., confirm: false)` is Denied.
4. `SetStartType("EventLog", confirm: true)` is still Denied (protected).
5. `SetStartType(..., Boot)` is Unsupported.
6. Missing name Start/Stop/Pause is NotFound.
7. Pause EventLog is Unsupported or Denied, never a throw.
8. Control verbs do not throw.

---

## 4. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase4
```
