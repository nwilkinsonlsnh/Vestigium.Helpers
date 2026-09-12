# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.7  
**Status:** Phase 0 Locked. Phases 1–6 Done. Phase 7 of 8 in flight.  
**Date:** 12 September 2026

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0–6** | List through recovery | **Done** |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | **In progress** |
| **8 Harden** | Tests, no password in logs, guide matches engine | Planned |

---

## 2. Phase 7 scope

Watch (continuous):

```text
ServiceHelper.Watch(name, interval)
ServiceHelper.WatchQuery(kql, interval)
```

Interval 250 ms – 60 s. Inner ticks do not write JSONL.

KQL search uses `KqlPack.Service`. `Name LIKE 'Event%'`. `PID` is not a Service field (`SVC.Pid` / `Pid` is).

Campaign (scheduled):

```text
CreateCampaign(recipe)   // recipe.json overwritten in campaign folder
LoadCampaign(name)
ListCampaigns()
RunAsync() / TickOnce() / Stop()
```

Root is `%ProgramData%\Vestigium\Services\Campaigns` unless `ServiceTestHooks.CampaignRoot` is set. Tests must set that.

A campaign needs ≥1 window (1 min–24 h) and Query or Match.Term. Samples append `samples.jsonl` only while a window is open. No password field on the line.

Example windows: 00:00 for 10 min, 08:00 for 10 min, 12:00 for 10 min. Filter `Name LIKE 'app%'` or StartsWith/Contains terms.

---

## 3. Close gates

1. Watch interval 10 ms throws.
2. Watch EventLog raises Sampled with EventLog.
3. `Search("Name LIKE 'Event%'")` hits EventLog.
4. `Search("PID == 1")` throws (wrong pack).
5. Open window writes samples.jsonl containing EventLog and `kind=service`.
6. Closed window does not create samples.jsonl.
7. Recipe without Query/Match throws.
8. JSONL has no password token.

---

## 4. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase7
```
