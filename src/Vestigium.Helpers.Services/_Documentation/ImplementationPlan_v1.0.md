# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.8  
**Status:** Phases 0–8 **Done**.  
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
| **6 Recovery** | First / second / subsequent + reset period | **Done** |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | **Done** |
| **8 Harden** | Tests, no password on snapshots, guide matches engine | **Done** |

Demo gallery stays out of this plan.

---

## 2. Phase 8 gates

1. `ServiceInfo`, `ServiceControlResult`, `ServiceRecoveryInfo`, `ServiceCampaignTick` have no Password property. Password exists only on `ServiceLogonRequest` (write input).
2. `WatchQuery` with a Process-only field throws at construct, not on the first tick.
3. `CreateCampaign` overwrites `recipe.json` (no destination-exists throw).
4. `LoadCampaign` round-trips. Missing name throws `FileNotFoundException`.
5. Protected names deny Stop / SetStartType / SetLogon / SetRecovery even with confirm.
6. Tests never write `%ProgramData%\Vestigium\Services` — they set `ServiceTestHooks.CampaignRoot`.
7. Guide lists the same public surface as `ServiceHelper`.

---

## 3. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase
```
