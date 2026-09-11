# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.1  
**Status:** Active. Build mode follows this file.  
**Date:** 11 September 2026  
**Scope:** `Vestigium.Helpers.Processes` and `Vestigium.Helpers.Kql` **libraries only**  
**Out of scope:** Demo galleries, Vestigium product hosts, Services / Network / Charts UI  
**Parent:** Processes [`Requirements_v1.0.md`](Requirements_v1.0.md), Kql [`Requirements_v1.0.md`](../../Vestigium.Helpers.Kql/_Documentation/Requirements_v1.0.md)

PR01 shipped Processes v1 and Kql v1.  
PR02 closes library gaps so a compiled query can evaluate on a live table. No Demo work.

---

## 0. How build mode uses this file

1. One phase at a time. Flip **Done** only when that phase's Close gate is green on `main`.
2. Commit form: `PR02 phase X: <short goal>`.
3. Do not change Demo projects. No gallery tabs, no XAML.
4. `Vestigium.Helpers.Kql` must not reference Processes / Services / Network.
5. Processes may reference Kql.
6. HelperLog only. Never log passwords, command lines unless `LogCommandLine`, row values, or Kql RHS strings.
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
| **G Completeness** | both | `IN`/`BETWEEN`, type-mismatch copy, WOW64 path, Start-As audit | Not started |
| **H Harden** | both | Tests, guides, comment-key hash, no Demo work | Not started |

---

## 2. Locks

| # | Lock |
|---|---|
| 1 | Libraries only. Vestigium solution UI is a later host. |
| 2 | Kql remains a filter dialect + catalog. No SCM, no `GetProcesses`, no PDH. |
| 3 | Watch-only fields still compile. Missing previous sample → unknown, not a parse error. |
| 4 | Slim `List` stays the cheap default. Kql `Search` may take Full when the AST needs it. |
| 5 | GPU missing → `Unsupported` / unknown. Never write `0`. |
| 6 | Protected / Critical / denylist processes are never killed by a wide query, even with `Confirm`. |
| 7 | Campaign remains in-process. Host must stay alive. |
| 8 | No regex. No Azure Kusto pipes. |
| 9 | `IN` / `BETWEEN` ship in **Phase G**. Literals only. No subqueries. |
| 10 | Tests inject nothing into ProgramData except existing hooks under temp roots. |
| 11 | Start-As never logs the password. `LogCommandLine` defaults false for Start-As. |
| 12 | Comment and Autostart keys use a normalized image path (WOW64-aware). |

---

## 3. Backlog

### 3.1 Kql

| Pri | ID | Gap | Why it bites | Phase |
|---|---|---|---|---|
| P1 | K1 | Catalog missing Full process fields | Cannot query Description / Signer / mitigations | A |
| P1 | K2 | Bind error lists every enabled field | Hosts cannot show why `PID` failed | A |
| P3 | K3 | No watch-only flag | `CPU.Usage` looks like a snapshot column | A |
| P2 | K4 | Empty string vs unknown | Hosts bind `""` for missing WindowTitle | A |
| P2 | K5 | No system/thread row type in Kql | Packs exist; hosts invent shape | F (Processes `IKqlRow`; Kql stays generic) |
| P3 | K6 | `IN` / `BETWEEN` | PID sets and ranges are noisy OR-chains | **G** |
| P3 | K7 | Type-mismatch message names field, not intent | `PID LIKE '%10%'` fails with prose hosts cannot parse | **G** |

### 3.2 Processes

| Pri | ID | Gap | Why it bites | Phase |
|---|---|---|---|---|
| P0 | P1 | Query watcher drops previous sample | `CPU.Usage GT 20` never hits | C |
| P0 | P2 | `Search(query)` always Slim | GPU / command line queries return nothing | B |
| P0 | P3 | `ProcessKqlRow` thin | Full snapshot never reaches Kql | B |
| P1 | P4 | `Match` required when `Query` set | Dummy term in every recipe | D |
| P1 | P5 | Campaign JSONL is a stub | Cannot chart a window later | D |
| P1 | P6 | Query watcher swallows tick exceptions | Sampler death looks like zero hits | C |
| P1 | P7 | Search cap is encounter-order | Truncation depends on `GetProcesses` order | E |
| P2 | P8 | KillTree / KillSearch vs PID reuse and PPL | Wrong tree or a wide LIKE | E |
| P2 | P9 | GPU instance mapping | PID recycle → 0% looks like a hit | C |
| P2 | P10 | Session 0 title bound as `""` | `WindowTitle` predicates lie | B |
| P2 | P11 | Comment key path-only | Two command lines, one comment | H |
| P2 | P12 | Thread list not an `IKqlRow` | `KqlPack.Thread` unused | F |
| P2 | P13 | System counters not an `IKqlRow` | Cannot `MEM.PhysicalPercent GT 90` | F |
| P2 | P16 | WOW64 path vs ImageType | `System32`/`SysWOW64` merge two images on comments and Autostart | **G** |
| P3 | P17 | Start-As audit | Password or command line can leak into HelperLog | **G** |
| P3 | P14 | Full list handle pressure | Slim default stays | — |
| P3 | P15 | Campaign in-process only | Document in D + H | D, H |
| P3 | P18 | Autostart completeness | Best-effort only; stay `None`/`Unsupported` | H (docs) |

### 3.3 Cross-cutting

| Pri | ID | Gap | Why it bites | Phase |
|---|---|---|---|---|
| P1 | X1 | Watch-only unmarked and uncomputed | Valid Kql is permanently unknown | A + C |
| P2 | X2 | Pack vs field (`PID` on Adapter) | Correct compile error; Process search stays Process pack | lock 4 |

---

## 4. Phases

### Phase A — Kql catalog

Unchanged from v1.0: Full process fields, `WatchOnly`, bind-error shape, empty vs unknown on `KqlValue`.

### Phase B — Bind

Unchanged: full `ProcessKqlRow`, Kql Search takes Full when the AST needs it, missing WindowTitle is unknown.

### Phase C — Tempo

Unchanged: previous-sample map on query watcher and campaign ticks; log tick failures; GPU never coerced to 0.

### Phase D — Campaign

Unchanged: optional `Match` when `Query` set; richer JSONL; in-process note.

### Phase E — Safety

Unchanged: denylist / PPL / `AmbiguousParent`; stable Name+Pid sort before `maxResults`.

### Phase F — Other rows

Unchanged: `ThreadKqlRow`, `SystemKqlRow`, `SearchThreads`, `MatchSystem`.

### Phase G — Completeness (was parked leftovers)

**Ship — Kql**

- Grammar (filter dialect only):
  - `field IN (literal, literal, …)` — same type as the field; empty list is a compile error
  - `field BETWEEN low AND high` — numeric / timespan / datetime; inclusive; `low` and `high` literals
  - Keywords case-insensitive. No subqueries. No `IN` of identifiers.
- Type-mismatch error shape:
  - `type mismatch field={canonical} type={KqlType} op={op} rhs={rhsKind}`
  - Example: `type mismatch field=PROC.Pid type=Integer op=LIKE rhs=string`
  - Still no RHS string value in the log or the message.

**Ship — Processes**

- Normalize image path for comment key and Autostart lookup:
  - If ImageType is X86 on a 64-bit OS and path contains `\System32\`, also consider `\SysWOW64\` (and the reverse) as the same image key.
  - Store the normalized path + optional command-line hash hook used in H.
- Start-As:
  - Password never appears in HelperLog (assert on RecentJsonLines in tests).
  - `LogCommandLine` defaults **false** on Start-As. When false, arguments are not logged.
  - Credential object is not retained after `CreateProcess` returns.

**Close gate**

- `PID IN (10, 20, 30)` matches 20, not 40
- `MEM.PrivateBytes BETWEEN 1000 AND 2000` inclusive at both ends
- `PID LIKE '%10%'` compile error contains `op=LIKE` and `type=Integer` and does not contain `%10%`
- Two WOW64-equivalent paths resolve to one comment key in a unit test with a fake path pair
- Start-As test (or Start with `LogCommandLine=false`) leaves no password substring in HelperLog

### Phase H — Harden

**Ship**

- Processes DevelopersGuide + Campaigns: Query optional, Slim vs Full for Kql Search, in-process campaign, WOW64 key, Start-As logging, Autostart is best-effort.
- Kql DevelopersGuide: new fields, watch-only, bind-error shape, empty vs unknown, `IN` / `BETWEEN`, type-mismatch shape.
- Tests `~Kql` and `~ProcessKql` / `~ProcessCampaign` / `~ProcessStart` cover A–G.
- Comment store: optional command-line hash per SRS §3.13 on the normalized path from G. If the hook is already there, turn it on; do not invent a second store.

**Close gate**

- Guides name only public types that compile
- Kql.csproj references Helpers only
- No Demo project diff required to close PR02

---

## 5. Parked (not PR02)

| Item | Notes |
|---|---|
| Suspend / Resume / dump | New verbs. |
| Remote / WMI process table | Out of v1 SRS. |
| Regex | LIKE / `IN` cover the v1 filter set. |
| GPU clocks / power / temp | Locked out of Kql v1. |
| Task Scheduler campaign | Different host, not this library. |
| Services / Network Kql packs filled from those libraries | Separate PR when those libraries are reviewed. |
| Demo / Vestigium solution UI | Explicitly excluded. |

---

## 6. Later Kql expansions (other libraries)

When Services, Network, or WinReg are reviewed, add catalog fields in **Kql** first, then bind `IKqlRow` in that library. Do not grow Processes to own SVC.* or NET.*.

---

## 7. Commands

```
dotnet test src/Vestigium.Helpers.Tests --filter "FullyQualifiedName~Kql|FullyQualifiedName~Process"
```

No Demo `dotnet run` is a close gate for this PR.
