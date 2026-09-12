# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.6  
**Status:** Phase 0 Locked. Phases 1–5 Done. Phase 6 of 8 in flight.  
**Date:** 12 September 2026

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. | **Locked** |
| **1 List / Get / Search** | Visible Win32. StartsWith / EndsWith / Contains. | **Done** |
| **2 Full row** | Config, account, image path, delayed, failure actions (read) | **Done** |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by | **Done** |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | **Done** |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon rights | **Done** |
| **6 Recovery** | First / second / subsequent + reset period | **In progress** |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests, no password in logs, guide matches engine | Planned |

---

## 2. Phase 6 scope

```text
ServiceHelper.GetRecovery(name) -> ServiceRecoveryInfo?
ServiceHelper.SetRecovery(name, ServiceRecoveryRequest)
```

| Field | Meaning |
|---|---|
| FirstFailure | SC_ACTION 0 |
| SecondFailure | SC_ACTION 1 |
| SubsequentFailures | SC_ACTION 2 |
| ActionDelay | Delay applied to each action |
| ResetPeriod | Fail-count reset. Days are `TimeSpan.FromDays(n)`. 0 = never reset. |
| Command | Required when any action is RunCommand |
| RebootMessage | Optional when an action is Reboot |
| Confirm | Required for write |

Protected names cannot SetRecovery. Missing name is NotFound. Negative reset/delay is InvalidState.

Read is also on `Get(name, Full).FailureActions`.

---

## 3. Close gates

1. GetRecovery EventLog returns Name + Actions + ResetPeriod.
2. GetRecovery missing is null.
3. SetRecovery confirm false is Denied.
4. SetRecovery EventLog confirm true is Denied / protected.
5. SetRecovery missing is NotFound.
6. RunCommand without Command is InvalidState.
7. Negative ResetPeriod is InvalidState.

---

## 4. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase6
```
