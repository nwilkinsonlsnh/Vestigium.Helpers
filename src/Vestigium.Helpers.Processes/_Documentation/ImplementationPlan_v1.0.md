# Vestigium.Helpers.Processes — Phase Implementation Plan

**Document ID:** VEST-HLP-PROCESSES-PLAN-000  
**Version:** 1.0  
**Status:** Active. Build mode follows this file.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Processes`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS **v1.2 Accepted**)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins. If this file and working code disagree, change the code. Do not reopen locked decisions to make a slice easier.

Working tree of record: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Processes/`.

---

## 0. How build mode uses this file

1. Read SRS §2 / §3 / §10.1 / §18 before touching code.
2. Implement **one phase**. Do not start the next phase until that phase's **Close gate** is green.
3. Commit form: `Processes phase N: <short goal>`.
4. Do not invent APIs that are not in SRS §11. Names may move a token; shapes may not.
5. Do not spawn `tasklist`, `taskkill`, Process Explorer, System Informer, or `schtasks`.
6. Do not log passwords or command lines unless `LogCommandLine` is on.
7. After each phase run the commands in §15.

Progress is the table in §1. Flip a row to **Done** only when its Close gate is green on `main`.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS Accepted, this plan, taxonomy, umbrella HLP-PRC | **In progress** |
| **1 List / Get / Search + TFM** | Slim rows, search modes, `net10.0-windows` | **This commit** |
| **2 Full process row** | Image, signer, window, mitigations, comment, autostart (Run + Startup) | Not started |
| **3 Tree + threads** | PPID walk, `GetThreads` | Not started |
| **4 Watchers** | Process + system interval samples, no JSONL yet | Not started |
| **5 Start / Start-As / Kill** | Lifetime verbs, `KillConfirm` | Not started |
| **6 System counters** | GPU, commit, physical, kernel, paging lists, topology | Not started |
| **7 Campaign JSONL** | Recipe, windows, subscribe, `samples.jsonl` | Not started |
| **8 Demo** | Six gallery tabs including Campaign | Not started |
| **9 Harden** | Sparse HelperLog, injected roots, Developers Guide matches engine, full SRS tests | Not started |

---

## 2. Locked decisions (do not debate)

Unchanged from v1.0 of this plan.

---

## 4. Phase 1 — List, Get, Search, TFM

Shipped in this change set:

- `Vestigium.Helpers.Processes.csproj` → `net10.0-windows`
- `ProcessInfo` slim + image path + resource counters
- `List` / `Get` / `TryGet` / `Search` (StartsWith / EndsWith / Contains)
- `ProcessHelperTests`

Not in this phase: signer, command line, window title fill, mitigations, watchers, start, kill.

**Close gate.** `dotnet test --filter FullyQualifiedName~ProcessHelper` on Windows-latest. Host PID present; search modes hit the test host name; empty term throws.

Commit: `Processes phase 1: list get search and net10.0-windows`.

---

## 15. Commands

```
dotnet build src/Vestigium.Helpers.Processes/Vestigium.Helpers.Processes.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ProcessHelper
dotnet run --project src/Vestigium.Helpers.Processes.Demo/Vestigium.Helpers.Processes.Demo.csproj
```
