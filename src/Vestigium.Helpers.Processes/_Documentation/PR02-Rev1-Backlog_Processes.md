# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.0  
**Status:** Active. Build mode follows this file.  
**Date:** 11 September 2026  
**Scope:** `Vestigium.Helpers.Processes` and `Vestigium.Helpers.Kql` **libraries only**  
**Out of scope:** Demo galleries, Vestigium product hosts, Services / Network / Charts UI  
**Parent:** Processes [`Requirements_v1.0.md`](Requirements_v1.0.md), Kql [`Requirements_v1.0.md`](../../Vestigium.Helpers.Kql/_Documentation/Requirements_v1.0.md)

PR01 shipped Processes v1 and Kql v1 (catalog, parser, 3VL, LIKE, host Search/Watch/Campaign, harden).  
PR02 closes the gaps that make a compiled query evaluate empty on a live table.

Other helper libraries (Services, Network, …) may add Kql catalog fields later. That work is **not** this PR. When it happens, add a row to §6 and open a new PR.

---

## 0. How build mode uses this file

1. One phase at a time. Flip **Done** only when that phase's Close gate is green on `main`.
2. Commit form: `PR02 phase N: <short goal>`.
3. Do not change Demo projects unless a library test host is required. No gallery tabs, no XAML.
4. `Vestigium.Helpers.Kql` must not reference Processes / Services / Network.
5. Processes may reference Kql (already does).
6. HelperLog only. Never log passwords, command lines unless the caller set `LogCommandLine`, row values, or Kql RHS strings.
7. If this file and the SRS disagree, the SRS wins after a written lock change here.

---

## 1. Scoreboard

| Phase | Library | Goal | Status |
|---|---|---|---|
| **A Catalog** | Kql | Process-row fields + watch-only hint + tighter bind errors | Not started |
| **B Bind** | Processes | Full `ProcessKqlRow`; Kql `Search` uses the fields the AST names | Not started |
| **C Tempo** | Processes | Query watcher + campaign ticks keep last sample (CPU % / I/O deltas) | Not started |
| **D Campaign** | Processes | `Match` optional when `Query` set; JSONL carries watcher counters | Not started |
| **E Safety** | Processes | KillTree/KillSearch honor protection + PID reuse; search order | Not started |
| **F Rows** | Processes | Thread row + system row as `IKqlRow` | Not started |
| **G Harden** | both | Tests, guides, no new Demo work | Not started |

---

## 2. Locks (do not debate in build mode)

| # | Lock |
|---|---|
| 1 | Libraries only. Vestigium solution UI is a later host. |
| 2 | Kql remains a filter dialect + catalog. No SCM, no `GetProcesses`, no PDH. |
| 3 | Watch-only fields still compile. Missing previous sample → unknown, not a parse error. |
| 4 | Slim `List` stays the cheap default. Kql `Search` may take Full when the AST needs it. |
| 5 | GPU missing → `Unsupported` / unknown. Never write `0`. |
| 6 | Protected / Critical / denylist processes are never killed by a wide query, even with `Confirm`. |
| 7 | Campaign remains in-process. Host must stay alive. Document, do not pretend it is Task Scheduler. |
| 8 | No regex. No Azure Kusto pipes. |
| 9 | `IN` / `BETWEEN` are **not** this PR unless Phase A finishes early and the catalog is stable. Parked in §5. |
| 10 | Tests inject nothing into ProgramData except existing comment/campaign hooks under temp roots. |

---

## 3. Backlog (source of phases)

### 3.1 Kql

| Pri | ID | Gap | Why it bites | Phase |
|---|---|---|---|---|
| P1 | K1 | Catalog missing Full process fields | Description, Version, Signer, Package, Autostart, Comment, WindowStatus, DEP, ASLR, CFG, Stack, Protection, DPI, UI Access, Virtualized, EnterpriseContext cannot be queried | A |
| P1 | K2 | Bind error lists every enabled field | Hosts cannot show why `PID` failed | A |
| P3 | K3 | No watch-only flag on a field | `CPU.Usage` looks like a snapshot column | A |
| P2 | K4 | Empty string vs unknown not documented on `KqlValue` | Hosts bind `""` for missing WindowTitle | A (docs + helper) |
| P2 | K5 | No built-in system/thread row type | Packs exist; hosts invent shape | F (Processes supplies rows; Kql stays generic `IKqlRow`) |
| P3 | K6 | `IN` / `BETWEEN` | Parked | §5 |

### 3.2 Processes

| Pri | ID | Gap | Why it bites | Phase |
|---|---|---|---|---|
| P0 | P1 | Query watcher drops previous sample | `CPU.Usage GT 20` never hits | C |
| P0 | P2 | `Search(query)` always Slim | GPU / command line / signer queries compile and return nothing | B |
| P0 | P3 | `ProcessKqlRow` thin | Full snapshot fields never reach Kql | B |
| P1 | P4 | `Match` required when `Query` set | Dummy term in every recipe | D |
| P1 | P5 | Campaign JSONL is a stub | Cannot chart a window later | D |
| P1 | P6 | Query watcher swallows tick exceptions | Sampler death looks like zero hits | C |
| P1 | P7 | Search cap is encounter-order | `LIKE '%.exe'` truncates wherever `GetProcesses` walked | E |
| P2 | P8 | KillTree / KillSearch vs PID reuse and PPL | Wrong tree or a wide LIKE | E |
| P2 | P9 | GPU instance mapping | PID recycle → 0% looks like a hit | C (keep Unsupported) |
| P2 | P10 | Session 0 title bound as `""` | `WindowTitle` predicates lie | B |
| P2 | P11 | Comment key path-only | Two command lines, one comment | G (hash optional, SRS §3.13) |
| P2 | P12 | Thread list not an `IKqlRow` | `KqlPack.Thread` unused | F |
| P2 | P13 | System counters not an `IKqlRow` | Cannot `MEM.PhysicalPercent GT 90` | F |
| P3 | P14 | Full list handle pressure | Already mitigated by Slim default; do not change List default | — |
| P3 | P15 | Campaign in-process only | Accepted; document in Campaigns + D | D |

---

## 4. Phases

### Phase A — Kql catalog (library `Vestigium.Helpers.Kql`)

**Ship**

- Add Process-pack fields (string unless noted):
  - `PROC.Description`, `PROC.Version`, `PROC.Signer` (publisher string), `PROC.SignerTrust`, `PROC.Package`, `PROC.Autostart`, `PROC.Comment`, `PROC.WindowStatus`
  - `PROC.Dep`, `PROC.Aslr` (bool), `PROC.Cfg`, `PROC.StackProtection`, `PROC.Protection`, `PROC.Dpi`, `PROC.UiAccess` (bool), `PROC.Virtualized` (bool), `PROC.EnterpriseContext`
- Aliases: `Description`, `Version`, `Signer`, `Package`, `Comment`
- Optional `KqlField.WatchOnly` (bool). Set on `CPU.Usage`, `CPU.TimeDelta`, `MEM.*Delta`, `IO.*Delta`, `GPU.Usage` when those names already exist.
- Bind error format: `unknown field '{name}' on pack={packs}. enabled ({group}): {up to 12 names} … +{rest}`.
- `KqlValue` XML / guide: missing string → `Unknown`, not `""`.

**Close gate**

- `Create(Process).TryGetField("Description")` → `PROC.Description`
- `Create(Service)` still rejects `PROC.Description`
- `Compile("Nope == 1", Process)` message contains `pack=Process` and does not dump the entire catalog as one undifferentiated blob
- Kql.csproj still references only Helpers

### Phase B — Bind (library `Vestigium.Helpers.Processes`)

**Ship**

- `ProcessKqlRow` maps every Phase A field from `ProcessInfo`. Null / Denied / Unsupported → `KqlValue.Unknown`. Do not bind empty window title as `""`.
- `Search(string query, …)` inspects the bound AST:
  - identity/resource-only → Slim is enough
  - any Full-only field → capture Full
- Keep the `Search(term, mode, …)` overload unchanged.

**Close gate**

- Fixture/live row with `Description` set matches `Description LIKE '%'`
- Missing `WindowTitle` does not match `WindowTitle == ''`
- `Search("CommandLine LIKE '%dotnet%'")` can see command lines the token is allowed to read (skip assertion if Denied on the host)

### Phase C — Tempo

**Ship**

- `ProcessQueryWatcher` keeps `pid → previous ProcessInfo` (or raw CpuTime + I/O counters).
- Second tick onward fills `CpuPercent` and I/O / memory deltas; bind those into `ProcessKqlRow` extras.
- Campaign `TickOnce` uses the same previous-sample map while a window stays open. Clear the map when the window closes.
- Tick exceptions: HelperLog Warning (APPID Processes, subcategory Watch or Campaign), then raise `Sampled` with `Matches = []` only after logging. Do not swallow silently.
- GPU: if the catalog instance is missing, leave unknown. Never coerce to 0.

**Close gate**

- Two synthetic samples: first `CPU.Usage GT 0` is not a hit; second with increased `CpuTime` is a hit.
- A thrown sampler logs a Warning line that does not include command line or query RHS.

### Phase D — Campaign recipe + JSONL

**Ship**

- `ProcessSearchRequest? Match` (optional). Validate: `Query` or (`Match` + term), not both required.
- If both set, **Query wins**. Log one Information line `Campaign query-overrides-match name=...` (no term, no query body).
- JSONL process line adds: `cpuPercent`, `ioReadBytes`, `ioWriteBytes`, `ioReadBytesDelta`, `ioWriteBytesDelta`, `privateBytesDelta` when known.
- Campaigns doc states: in-process only; host process must be running.

**Close gate**

- Recipe with only `Query` + windows creates and samples.
- Recipe with neither Query nor term throws.
- Sample file contains `cpuPercent` after two ticks inside a window (may be null on tick 1).

### Phase E — Safety and search order

**Ship**

- `KillTree` / `KillSearch` skip denylist + Protected + Critical. Result `Denied` per PID. Confirm does not override.
- Skip a child whose `AmbiguousParent` is true.
- Kql and term search: when hitting `maxResults`, sort by Name then Pid before take so truncation is stable.

**Close gate**

- Unit test: denylist name is Denied even with Confirm.
- Two searches with the same query + cap return the same PID set (stable sort).

### Phase F — Other rows

**Ship**

- `ThreadKqlRow` from `ThreadInfo` for `THR.*` + thread CPU.
- `SystemKqlRow` from `SystemCounters` for SYS / MEM / CPU topology / IO deltas already on that type.
- `ProcessHelper.SearchThreads(int pid, string query)` and `ProcessHelper.MatchSystem(string query)` — small façade, no Demo.

**Close gate**

- `GetThreads` + `TID == {known}` returns that thread when the token can read it.
- `MatchSystem("SYS.ProcessCount GT 0")` is true on a live box.

### Phase G — Harden

**Ship**

- Processes DevelopersGuide + Campaigns_v1.2: Query optional, Slim vs Full for Kql Search, in-process campaign.
- Kql DevelopersGuide: new fields, watch-only, bind-error shape, empty vs unknown.
- Tests under `FullyQualifiedName~Kql` and `~ProcessKql` / `~ProcessCampaign` cover A–F.
- Comment store: optional command-line hash per SRS §3.13 if the existing key helper is a one-line change; otherwise file a follow-up ID and leave P11 open.

**Close gate**

- Guides name only public types that compile.
- Kql project references Helpers only.
- No Demo project diff required to close PR02.

---

## 5. Parked (not PR02)

| Item | Notes |
|---|---|
| Kql `IN` / `BETWEEN` | New grammar. Open PR03 if hosts need PID sets. |
| Suspend / Resume / dump | New verbs. |
| Remote / WMI process table | Out of v1 SRS. |
| Regex | LIKE is the wildcard path. |
| GPU clocks / power / temp | Locked out of Kql v1. |
| Task Scheduler campaign | Would be a different host, not this library. |
| Services / Network Kql packs filled from those libraries | Separate PR when those libraries are reviewed. |
| Demo / Vestigium solution UI | Explicitly excluded. |

---

## 6. Later Kql expansions (other libraries)

When Services, Network, or WinReg are reviewed, add catalog fields in **Kql** first (pack + group), then bind an `IKqlRow` in that library. Do not grow Processes to own SVC.* or NET.*.

| Library | Likely pack | Trigger |
|---|---|---|
| Services | `KqlPack.Service` (already stubbed) | SCM review |
| Network | `KqlPack` + `KqlGroups.Net` | Inventory / echo review |
| WinReg | new pack or group | If query-over-keys is wanted |

---

## 7. Commands

```
dotnet test src/Vestigium.Helpers.Tests --filter "FullyQualifiedName~Kql|FullyQualifiedName~Process"
```

No `dotnet run` of a Demo is a close gate for this PR.
