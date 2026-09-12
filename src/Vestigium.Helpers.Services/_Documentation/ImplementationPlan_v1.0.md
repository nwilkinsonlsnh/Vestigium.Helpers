# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.2  
**Status:** Phase 0 Locked. Phase 1 Done. Phase 2 of 8 in flight.  
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
| **1 List / Get / Search** | Visible Win32 rows. StartsWith / EndsWith / Contains. Probe. | **Done** |
| **2 Full row** | Config, account, image path, delayed auto-start, failure actions (read) | **In progress** |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by walk | Planned |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | Planned |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon-as-service / batch | Planned |
| **6 Recovery** | First / second / subsequent + reset period | Planned |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests ≥ coverage bar, no password in logs, guide matches engine | Planned |

Later-phase types may already exist in the tree. A phase is **Done** only when its close gate in §4 passes.

---

## 2. Phase 0 — locked decisions

Accepted 11 September 2026. Unchanged from v1.1.

---

## 3. Phase 2 scope

`Get(name, Full)` and `List(Slim|Full)` fill a config row from SCM. No writes.

| Field | Level | Source |
|---|---|---|
| StartType | Slim+ | `QueryServiceConfig`. Automatic + delayed flag → `AutomaticDelayed`. |
| DelayedAutoStart | Slim+ | `SERVICE_CONFIG_DELAYED_AUTO_START_INFO` |
| ImagePath | Slim+ | Binary path. Never rewritten in v1. |
| Account | Slim+ | Start name. `LocalSystem` / `NT AUTHORITY\SYSTEM` normalized to `LocalSystem`. Password is never read. |
| DesktopInteract | Slim+ | `SERVICE_INTERACTIVE_PROCESS` bit |
| ErrorControl, LoadOrderGroup, TagId | Slim+ | `QueryServiceConfig` |
| SidType, RequiredPrivileges, PreshutdownTimeout, LaunchProtected | Slim+ | `QueryServiceConfig2` when present |
| Description | Full | `SERVICE_CONFIG_DESCRIPTION` |
| FailureActions + reset period + command | Full | `SERVICE_CONFIG_FAILURE_ACTIONS` |

`Get` opens one service by name. It does not walk the whole SCM. Missing name still returns null.

`ServiceInfo` has no `Password` property.

---

## 4. Close gates

### Phase 1

Unchanged. Filter `FullyQualifiedName~ServicePhase1`.

### Phase 2

1. `Get("EventLog", Identity)` has no ImagePath / Account / StartType.
2. `Get("EventLog", Slim)` has ImagePath, Account, StartType. FailureActions is empty.
3. `Get("EventLog", Full)` has ImagePath containing `svchost`, Account `LocalSystem`, a StartType, a Description, and a FailureActions list (may be empty actions, never null).
4. Delayed Automatic services report `StartType == AutomaticDelayed` and `DelayedAutoStart == true`.
5. Search ImagePath Contains `windows` returns only rows whose path contains `windows`.
6. Search Account Contains `LocalSystem` returns only those accounts.
7. `ServiceInfo` has no public `Password` property.
8. A denied config query records Availability. It does not throw.

---

## 5. Commands

```
dotnet build src/Vestigium.Helpers.Services/Vestigium.Helpers.Services.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase2
```
