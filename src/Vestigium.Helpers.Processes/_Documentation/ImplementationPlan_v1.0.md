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
| **0 Paper** | SRS Accepted, this plan, taxonomy, umbrella HLP-PRC | **In progress** (this commit) |
| **1 List / Get / Search + TFM** | Slim rows, search modes, `net10.0-windows` | Not started |
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

**Goal.** Build mode and the repo agree on the contract. Façade stays `Identity` + `Probe`.

**Edit**

- `src/Vestigium.Helpers.Processes/_Documentation/Requirements_v1.0.md` — Status **Accepted**, v1.2.
- `src/Vestigium.Helpers.Processes/_Documentation/DevelopersGuide_v1.0.md` — point at this plan + campaign call shape.
- This file.
- Umbrella `_Documentation/Requirements_v1.0.md` HLP-PRC row: SRS v1.2 accepted, TFM will move with Phase 1.
- `src/Vestigium.Helpers/HelperLog.cs` — add and register `Process`, `Thread`, `Watch`, `Start`, `Kill`, `System`. Reuse existing `Campaign`, `Jsonl`, `Stats`, `Inventory`, `Job`, `Probe`, `Identity`, `Guard`.
- Test `Processes_subcategories_are_registered` in `HelperLogTests`.

**Do not** grow `ProcessHelper`, flip the csproj TFM, or write campaign files.

**Close gate**

- Docs in `_Documentation/` are Accepted SRS + this plan + the guide.
- `dotnet test --filter Processes_subcategories` (suite still compiles).
- Commit: `Processes phase 0: accept SRS v1.2 and register taxonomy`.

---

## 4. Phase 1 — List, Get, Search, TFM

Flip `Vestigium.Helpers.Processes.csproj` to `net10.0-windows`. Slim `ProcessInfo`, `List` / `Get` / `Search` (StartsWith / EndsWith / Contains). No signer, command line, mitigations, watchers, or kill.

**Close gate.** Host PID present; search modes hit the test host name; empty term throws.  
Commit: `Processes phase 1: list get search and net10.0-windows`.

---

## 5. Phase 2 — Full process row

Image, signer, window, mitigations, comment store (injected path), autostart = Run keys + Startup folder only.

**Close gate.** Test host has Name + ImagePath + ImageType; unavailable fields are null + Availability.  
Commit: `Processes phase 2: full process row`.

---

## 6. Phase 3 — Tree and threads

`GetTree` / children / descendants, `ParentAlive`, cycle guard. `GetThreads` without stack by default.

**Close gate.** Child fixture appears under the test host. Thread list has at least one TID.  
Commit: `Processes phase 3: tree and threads`.

---

## 7. Phase 4 — Watchers

`IProcessWatcher` / `ISystemWatcher`. Immediate first sample with null deltas. `Exited` when PID is gone. Skip overrun ticks. No campaign files.

**Close gate.** Two samples at 250 ms produce a non-null time or CPU delta.  
Commit: `Processes phase 4: process and system watchers`.

---

## 8. Phase 5 — Start, Start-As, Kill

`ProcessStartRequest`, `ProcessStartAs` (`SecureString` / `char[]`). `Kill` / `KillTree` / `KillSearch` + `KillConfirm` (cap 16). Soft kill = WM_CLOSE + 5 s. No self-kill. Protected → `Denied`. Tests only kill fixtures they started. Stdout capture is not required to close this phase.

**Close gate.** Start fixture, see PID in List, Kill fixture, Get returns null.  
Commit: `Processes phase 5: start start-as kill`.

---

## 9. Phase 6 — System counters

GPU, I/O deltas, commit + `*K`, physical, kernel, paging lists, topology. GPU may be Unsupported.

**Close gate.** `LogicalProcessors >= 1`, `PhysicalTotal > 0`.  
Commit: `Processes phase 6: system counters`.

---

## 10. Phase 7 — Campaign JSONL and windows

`ProcessCampaignRecipe` + windows + `ProcessCampaign`. `ProcessTestHooks.CampaignRoot`. Recipe via `Vestigium.Helpers.Json`. Append-only `samples.jsonl`. Live `Sampled` + `WindowChanged`. Fake clock in tests. Re-run search every tick. `MaxMatches` + Truncated.

Seed recipe:

```text
Name = "day-parts"
Match = Contains "vestigium" on Name|ImagePath
Interval = 1 s
IncludeSystemCounters = true
Windows =
  midnight  00:00  10 min  All days
  morning   08:00  10 min  All days
  noon      12:00  10 min  All days
```

Do not call `schtasks`. Do not write samples to HelperLog. Do not use live ProgramData in tests.

**Close gate.** Injected root has `recipe.json` and ≥1 `samples.jsonl` line after a forced open window; after close, ticks do not append.  
Commit: `Processes phase 7: campaign jsonl and windows`.

---

## 11. Phase 8 — Demo gallery

Tabs: Processes, Watch, Threads, System, Start/Kill, Campaign.

**Close gate.** `dotnet run --project src/Vestigium.Helpers.Processes.Demo` opens the six tabs.  
Commit: `Processes phase 8: demo gallery`.

---

## 12. Phase 9 — Harden

Sparse HelperLog, guide matches engine, full SRS §14, no passwords / EXCEPTION objects in audit lines.

**Close gate.** `dotnet test --filter FullyQualifiedName~Process` green on Windows-latest.  
Commit: `Processes phase 9: harden`.

---

## 13. Logging cheat sheet

Category = `Helpers`. APPID = `Processes`.

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing |
| Inventory | List / Search counts |
| Process | Get / full row |
| Thread | Thread list |
| Watch | Watcher start / stop (not ticks) |
| Start / Kill | Lifetime |
| System | System snapshot request |
| Campaign | Recipe start, window open/close, truncated, stop |
| Jsonl | Campaign file create / path (not each sample) |
| Job | Optional correlation id for a kill-tree |

Never: passwords, file bytes, raw PE dumps, `Exception` as a HelperLog argument.

---

## 14. Explicitly out of this plan

Linux process table, regex search, schtasks / Windows service host, PPL bypass, PDB stacks, handle dump, minidump, ETW/Procmon traces, SCM (Services helper), JSONL rotation (v1.4), remote machines.

---

## 15. Commands

```
dotnet build src/Vestigium.Helpers.Processes/Vestigium.Helpers.Processes.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter Processes_subcategories
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Process
dotnet run --project src/Vestigium.Helpers.Processes.Demo/Vestigium.Helpers.Processes.Demo.csproj
```

Phase 0 only needs the subcategory filter. Phases 1–9 need Windows-latest.

---

## 16. Definition of done (v1.2 implementation)

1. SRS Status is Accepted and this plan is in `_Documentation/`.
2. HelperLog subcategories in Phase 0 are registered and tested.
3. SRS §11 surface exists. No cousin process is spawned.
4. Search modes work. Tree and threads work.
5. Watchers honor 250 ms–60 s.
6. Start-As never logs a password. KillSearch refuses without `KillConfirm`.
7. System counters publish bytes (and `*K`). GPU may be Unsupported.
8. A campaign with three daily 10-minute windows writes JSONL only while a window is open, and raises `Sampled` for subscribers.
9. Tests inject campaign and log roots.
10. Demo has the six tabs.

Phase 0 closes items 1–2. Items 3–10 are Phases 1–9.
