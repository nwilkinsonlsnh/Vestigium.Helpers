# Vestigium.Helpers.Services — Phase Implementation Plan

**Document ID:** VEST-HLP-SERVICES-PLAN-000  
**Version:** 1.3  
**Status:** Phase 0 Locked. Phases 1–2 Done. Phase 3 of 8 in flight.  
**Date:** 11 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**TFM:** `net10.0-windows`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

Demo gallery is **out** of all eight phases.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan. | **Locked** |
| **1 List / Get / Search** | Visible Win32. StartsWith / EndsWith / Contains. | **Done** |
| **2 Full row** | Config, account, image path, delayed auto-start, failure actions (read) | **Done** |
| **3 Hidden + tree** | `ListHidden` + `List(All)`. Depends-on / depended-by walk | **In progress** |
| **4 Control** | Start, Stop, Restart, Pause, Continue, SetStartType | Planned |
| **5 Logon** | LocalSystem, desktop interact, account+password, logon rights | Planned |
| **6 Recovery** | First / second / subsequent + reset period | Planned |
| **7 Watch + KQL + campaigns** | Interval samples, `KqlPack.Service`, JSONL windows | Planned |
| **8 Harden** | Tests, no password in logs, guide matches engine | Planned |

---

## 2. Phase 3 scope

Visible set = `ServiceController.GetServices()` union `GetDevices()`. Kind filter still applies.

Hidden set = `HKLM\SYSTEM\CurrentControlSet\Services` keys with a service/driver Type that are **not** in the visible set.

| Call | Meaning |
|---|---|
| `List()` | Visible Win32. `IsHidden == false`. services.msc default. |
| `List(..., kind: Driver)` | Visible drivers. |
| `ListHidden()` | Registry-only rows. Every row `IsHidden == true`. |
| `List(..., scope: All)` | Union. Count = visible + hidden. |

Tree:

```text
GetDependsOn(name)
GetDependedBy(name)
GetDependencyTree(name, DependsOn | DependedBy | Both)
ServiceTree.Flatten()
```

Cycle or depth > 32 sets `AmbiguousDependency` and stops that branch. Missing root throws `InvalidOperationException`.

Depends-on / depended-by fill at Slim so the tree does not require Full.

---

## 3. Close gates

### Phase 2 correction

EventLog account is whatever SCM reports (`LocalSystem`, `NT AUTHORITY\LocalService`, or `NetworkService`). Do not hard-code LocalSystem.

### Phase 3

1. Visible Win32 list never sets `IsHidden`.
2. Hidden list is disjoint from visible names. Every hidden row has `IsHidden`.
3. `List(All)` count = visible + hidden.
4. Visible drivers are not marked hidden.
5. `GetDependsOn` / `GetDependedBy` on EventLog do not throw.
6. EventLog DependsOn tree Flatten starts with EventLog.
7. Missing name throws `InvalidOperationException`.
8. Both-directions walk stays under 4096 nodes.

---

## 4. Commands

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase3
```
