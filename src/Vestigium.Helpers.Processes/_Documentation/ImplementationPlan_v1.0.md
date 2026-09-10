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
| **1 List / Get / Search + TFM** | Slim rows, search modes, `net10.0-windows` | **Shipped — confirm Windows CI** |
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

Copied from SRS §3 and §18. Build mode treats these as constants.

| # | Lock |
|---|---|
| 1 | TFM `net10.0-windows`. Flip in Phase 1, not Phase 0. |
| 2 | Process Explorer / System Informer are column references. Never spawned. |
| 3 | Snapshot vs Watcher. Identity fields are not watched. |
| 4 | Watcher interval default 1 s, allowed 250 ms–60 s. Outside throws. No clamp. |
| 5 | CPU % needs two samples. First snapshot `CpuPercent` is null. |
| 6 | Process memory is bytes. System snapshot also exposes `*K` convenience. |
| 7 | Partial rows. List never dies because one PPL process refused a handle. |
| 8 | Search is StartsWith / EndsWith / Contains. Ordinal ignore case. No regex. |
| 9 | Tree is live PPID. Orphans get `ParentAlive = false`. Cycle → `AmbiguousParent`. |
| 10 | Start-As uses create-with-logon. No credential UI. Never log the password. |
| 11 | Kill by PID. Tree-kill is explicit. `KillSearch` requires `KillConfirm` (cap 16). |
| 12 | Protected / Critical kill returns `Denied`. Do not disable Critical Process. |
| 13 | Comments: JSON, path injected in tests. |
| 14 | HelperLog only for audit. APPID `Processes`. Watcher ticks do not write HelperLog sample lines. |
| 15 | Missing GPU → `Unsupported`, not 0. |
| 16 | Probe and tests stay in `%TEMP%` / injected roots. |
| 17 | Campaign scheduler is in-process. Host stays alive. No `schtasks`. |
| 18 | Campaign samples go to `{CampaignRoot}\{name}\samples.jsonl`. Recipe is `recipe.json` via `Vestigium.Helpers.Json`. |
| 19 | Campaign match is the same search API, refreshed every tick. |
| 20 | Windows are local `TimeOnly` + duration + day flags. Example: 00:00, 08:00, 12:00 (noon) × 10 minutes. |
| 21 | Tests set `ProcessTestHooks.CampaignRoot`. Never live ProgramData. |
| 22 | Autostart in v1 = Run keys + Startup folder only (O2). |
| 23 | Stdout capture is opt-in and not required to close Phase 5 (O3). |

---

## 3. Phase 0 — Accept the paper and register taxonomy

Still open: HelperLog `Process` / `Thread` / `Watch` / `Start` / `Kill` / `System` constants and `Processes_subcategories_are_registered`.

---

## 4. Phase 1 — List, Get, Search, TFM

Shipped:

- csproj `net10.0-windows`
- slim `ProcessInfo` + image path + resource counters
- `List` / `Get` / `TryGet` / `Search`
- `ProcessHelperTests`

Not in this phase: signer, command line, window title, mitigations, watchers, start, kill.

**Close gate.** `dotnet test --filter FullyQualifiedName~ProcessHelper` on Windows-latest.

---

## 5. Phase 2 — Full process row

Image type, signer, window, mitigations, comment store, autostart = Run keys + Startup folder only.

---

## 6. Phase 3 — Tree and threads

`GetTree` / children / descendants, `ParentAlive`, cycle guard. `GetThreads` without stack by default.

---

## 7. Phase 4 — Watchers

`IProcessWatcher` / `ISystemWatcher`. Immediate first sample with null deltas. `Exited` when PID is gone.

---

## 8. Phase 5 — Start, Start-As, Kill

`ProcessStartRequest`, `ProcessStartAs`. `Kill` / `KillTree` / `KillSearch` + `KillConfirm` (cap 16).

---

## 9. Phase 6 — System counters

GPU, I/O deltas, commit + `*K`, physical, kernel, paging lists, topology.

---

## 10. Phase 7 — Campaign JSONL and windows

See [`Campaigns_v1.2.md`](Campaigns_v1.2.md).

---

## 11. Phase 8 — Demo gallery

Tabs: Processes, Watch, Threads, System, Start/Kill, Campaign.

---

## 12. Phase 9 — Harden

Sparse HelperLog, guide matches engine, full SRS §14.

---

## 15. Commands

```
dotnet build src/Vestigium.Helpers.Processes/Vestigium.Helpers.Processes.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ProcessHelper
dotnet run --project src/Vestigium.Helpers.Processes.Demo/Vestigium.Helpers.Processes.Demo.csproj
```
