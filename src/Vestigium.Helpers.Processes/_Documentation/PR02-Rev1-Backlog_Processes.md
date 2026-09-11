# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.2  
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
| **A Catalog** | Kql | Process-row fields + watch-only hint + tighter bind errors | **Done** |
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

## 3. Backlog IDs

K1–K5, X1 catalog side → A. K6–K7, P16–P17 → G. P1–P3, P10 → B. P1/P6/P9 → C. P4–P5 → D. P7–P8 → E. K5/P12–P13 → F. P11/P18 → H.

---

## 4. Phases

### Phase A — Done

Shipped in Kql: Full process fields listed in v1.1; `KqlField.WatchOnly`; bind error `unknown field '{name}' on pack=… enabled (…):` first 12 names `+{rest}`; `KqlValue.From` treats null/blank string as Unknown.

### Phase B — Bind

Full `ProcessKqlRow`. Kql `Search` takes Full when the AST names Full-only fields. Missing WindowTitle is unknown.

### Phase C — Tempo

Previous-sample map on query watcher and campaign ticks. Log tick failures. GPU never coerced to 0.

### Phase D — Campaign

Optional `Match` when `Query` set. Richer JSONL. In-process note.

### Phase E — Safety

Denylist / PPL / `AmbiguousParent`. Stable Name+Pid sort before `maxResults`.

### Phase F — Other rows

`ThreadKqlRow`, `SystemKqlRow`, `SearchThreads`, `MatchSystem`.

### Phase G — Completeness

`IN` / `BETWEEN`; type-mismatch `field= type= op= rhs=` without RHS text; WOW64 image key; Start-As audit.

### Phase H — Harden

Guides, tests A–G, comment-key hash, Autostart documented as best-effort. No Demo diff.

---

## 5. Parked (not PR02)

Suspend/Resume/dump, remote WMI, regex, GPU clocks, Task Scheduler campaign, Services/Network pack fill, Demo / Vestigium UI.

---

## 6. Commands

```
dotnet test src/Vestigium.Helpers.Tests --filter "FullyQualifiedName~Kql|FullyQualifiedName~Process"
```
