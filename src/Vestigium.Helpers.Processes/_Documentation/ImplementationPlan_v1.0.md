# Vestigium.Helpers.Processes — Phase Implementation Plan

**Document ID:** VEST-HLP-PROCESSES-PLAN-000  
**Version:** 1.1  
**Status:** Closed through Phase 9 on 10 September 2026.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Processes`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS **v1.2 Accepted**)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins. If this file and working code disagree, change the code.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS Accepted, this plan, taxonomy, umbrella HLP-PRC | **Done** |
| **1 List / Get / Search + TFM** | Slim rows, search modes, `net10.0-windows` | **Done** |
| **2 Full process row** | Image, signer, window, mitigations, comment, autostart | **Done** |
| **3 Tree + threads** | PPID walk, `GetThreads` | **Done** |
| **4 Watchers** | Process + system interval samples | **Done** |
| **5 Start / Start-As / Kill** | Lifetime verbs, `KillConfirm` | **Done** |
| **6 System counters** | GPU, commit, physical, kernel, paging, topology | **Done** |
| **7 Campaign JSONL** | Recipe, windows, subscribe, `samples.jsonl` | **Done** |
| **8 Demo** | Six gallery tabs including Campaign | **Done** |
| **9 Harden** | Sparse HelperLog, injected roots, guide matches engine | **Done** |

---

## 2. Locked decisions

Unchanged from SRS §3 / §18. TFM `net10.0-windows`. No `tasklist` / `taskkill` / `schtasks`. Campaigns are in-process. Tests inject `ProcessTestHooks.CampaignRoot`.

---

## 15. Commands

```
dotnet build src/Vestigium.Helpers.Processes/Vestigium.Helpers.Processes.csproj
dotnet build src/Vestigium.Helpers.Processes.Demo/Vestigium.Helpers.Processes.Demo.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Process
dotnet run --project src/Vestigium.Helpers.Processes.Demo/Vestigium.Helpers.Processes.Demo.csproj
```
