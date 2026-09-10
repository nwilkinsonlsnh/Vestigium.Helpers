# Vestigium.Helpers.Kql — Phase Implementation Plan

**Document ID:** VEST-HLP-KQL-PLAN-000  
**Version:** 1.0  
**Status:** Active. Build mode follows this file.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Kql` (product name **KQL**)  
**TFM:** `net10.0`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS **v1.0-draft** until Phase 0 Accept)  
**Counters:** [`Counters_v1.0.md`](Counters_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins after Acceptance. If this file and working code disagree, change the code.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Kql/`.

---

## 0. How build mode uses this file

1. Read SRS §1 locks and [`Counters_v1.0.md`](Counters_v1.0.md) before touching code.
2. Implement **one phase**. Do not start the next until that phase's **Close gate** is green.
3. Commit form: `Kql phase N: <short goal>`.
4. Do not invent APIs that are not in SRS §5. Names may move a token; shapes may not.
5. This package does **not** call `Process.GetProcesses`, SCM, PDH, or ETW.
6. This package does **not** parse pipes, `summarize`, `join`, `let`, or regex.
7. HelperLog: field names and error codes only. Never row values, command lines, or credentials.
8. Do not grow `KqlHelper` past Identity + Probe until Phase 0 Status is **Accepted** and the close gate is green.

Progress is the table in §1. Flip a row to **Done** only when its Close gate is green on `main`.

---

## 1. Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper + scaffold** | Accept SRS, taxonomy APPID `Kql`, slnx, Identity/Probe, Demo skeleton | Not started |
| **1 Catalog + session** | Packs, groups, aliases, `KqlHelper.Create`, field lookup | Not started |
| **2 Parser** | Filter grammar, line/col errors | Not started |
| **3 Bind + 3VL** | Enabled-field bind, true/false/unknown evaluate | Not started |
| **4 LIKE + fixture row** | `*` `%` `?` on LIKE only; in-memory test rows | Not started |
| **5 Processes host** | `Search(query)`, Watch, Campaign accept a Kql string | Not started |
| **6 Demo** | Catalog explorer + query box | Not started |
| **7 Harden** | Guide matches engine, sparse log, full tests | Not started |

---

## 2. Locked decisions (do not debate in build mode)

| # | Lock |
|---|---|
| 1 | Project `Vestigium.Helpers.Kql`. Façade `KqlHelper`. APPID `Kql`. |
| 2 | TFM `net10.0`. Not Windows-only. |
| 3 | Filter dialect only. Not Azure Kusto compatible. |
| 4 | Two axes: entity pack (`Process`, `Service`, `Thread`, `System`, `Adapter`) + resource group (`CPU`, `GPU`, `MEM`/`RAM`, `DISK`, `IO`, `NET`, `PROC`). |
| 5 | Session enablement: a query cannot see a disabled group. |
| 6 | Canonical names are honest. `CPU.PrivateBytes` is an **alias** of `MEM.PrivateBytes`. `CPU.IO.*` aliases `IO.*`. `RAM` aliases `MEM`. |
| 7 | Unknown field → compile error listing enabled fields. |
| 8 | Missing / Denied / Unsupported → three-valued **unknown**. Top-level unknown is not a hit. |
| 9 | Wildcards only on `LIKE` / `!LIKE`. `==` is exact, ordinal ignore case. |
| 10 | Keywords and identifiers case-insensitive. |
| 11 | Packs are catalog presets, not data providers. |
| 12 | New counters are catalog rows. Parser stays `ident ( '.' ident )*`. |
| 13 | Siblings: Core Helpers only. No project reference to Processes / Services / Network / Charts. |
| 14 | Processes consumes Kql in **Phase 5**, not Phase 1. |
| 15 | NET on a Process row is not v1. NET is System/Adapter only. |
| 16 | GPU clocks / power / temperature are not v1. |
| 17 | Paging Priority 0–7 are not v1 KQL names. |
| 18 | Tests inject nothing into ProgramData. No live Desktop. |

---

## 3. Phase 0 — Paper, taxonomy, scaffold

**Do**

- Flip SRS / Guide / this plan **Status: Accepted** when you say Accept (until then they stay draft/active as written).
- Add `HelperLog.AppIds.Kql`.
- Register APPID and subcategories: `Probe`, `Identity`, `Guard`, `Query`, `Document` (reuse), plus `Kql` if we keep a dedicated name.
- `src/Vestigium.Helpers.Kql/Vestigium.Helpers.Kql.csproj` — `net10.0`, reference Core Helpers only.
- `KqlHelper.Identity` = `Vestigium.Helpers.Kql`.
- `KqlHelper.Probe()` — Pending then Success. No parser.
- `src/Vestigium.Helpers.Kql.Demo` — SkeletonWindow + `HelperWpfHost.Start(..., Kql)` until Phase 6.
- Add both projects to `Vestigium.Helpers.slnx` (`/Library/`, `/Demo/`).
- Tests: `Identity_is_stable`, `Kql_subcategories_are_registered`, Probe writes Pending/Success to a temp log dir.
- Umbrella `_Documentation/Requirements_v1.0.md` gets an HLP-KQL row.

**Do not** parse a query in this phase.

**Close gate**

```
dotnet build src/Vestigium.Helpers.Kql/Vestigium.Helpers.Kql.csproj
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Kql
```

Façade is still Identity + Probe only.

---

## 4. Phase 1 — Catalog and session

**Ship**

- `KqlPack`: Process, Service, Thread, System, Adapter
- `KqlGroups` flags: Cpu, Gpu, Mem, Disk, Io, Net, Proc
- Built-in field table from SRS §3 **plus** [`Counters_v1.0.md`](Counters_v1.0.md) “Add in v1”
- Aliases resolve to one canonical field
- `KqlHelper.Create(params KqlPack[])` and `Create(KqlOptions)`
- `KqlSession.Fields` — enabled canonical names + aliases
- `TryGetField(name)` case-insensitive
- Unknown pack/group combination: Process+Net does not expose `NET.*`

**Close gate**

- `Create(KqlPack.Process)` exposes `PID`, `CPU.Usage`, `MEM.PrivateBytes`, `GPU.Usage`, `IO.Reads`
- `CPU.PrivateBytes` resolves to the same field as `MEM.PrivateBytes`
- `Create(KqlPack.Service)` does **not** expose `GPU.Usage`
- `Create(KqlPack.System)` exposes `MEM.CommitLimit`, `CPU.ContextSwitchDelta`, not `PROC.CommandLine`

---

## 5. Phase 2 — Parser

**Grammar (v1)**

```text
query     := or
or        := and ( ( '||' | OR ) and )*
and       := not ( ( '&&' | AND ) not )*
not       := NOT primary | primary
primary   := '(' query ')' | comparison
comparison:= field op value
op        := '==' | '!=' | '<>' | '>' | '<' | '>=' | '<=' | GT | LT | GE | LE | LIKE | NOT LIKE | !LIKE
field     := ident ( '.' ident )*
value     := number | string | boolean | timespan
```

**Ship**

- `KqlHelper.Parse(text)` → AST or `KqlError` with line, column, message
- Strings `'...'` and `"..."` with `\'` `\\`
- No bind yet (unknown field names still parse)

**Close gate**

- `(PID == 45944 || Name LIKE 'CCleaner%') && GPU.Usage GT 20` parses
- `Name ==` reports column of the missing value
- `A | where B` is a parse error (pipe)

---

## 6. Phase 3 — Bind and three-valued evaluate

**Ship**

- `KqlHelper.Compile(text, session)` binds names against **enabled** fields only
- `IKqlRow` / `KqlValue` (null = unknown)
- Compare rules: number vs number, string vs string; mismatch → compile error
- `unknown && false` = false; `unknown || true` = true; top-level unknown = not a match
- `Compile` of `GPU.Usage` on a Service session fails with the enabled-field list

**Close gate**

- In-memory process row `Name=msedge, PID=10` matches `Name LIKE '%edge%' && PID == 10`
- Same row does not match `GPU.Usage GT 20` when GPU value is missing (unknown)
- Service session rejects `MEM.PrivateBytes > 1`

---

## 7. Phase 4 — LIKE matcher and fixture rows

**Ship**

- LIKE: `*` and `%` = any run, `?` = one character
- `==` does not honor wildcards
- `KqlFixtureRow` dictionary host for tests and Demo until Processes binds real rows
- Escape rules for `\%` `\*` if we accept them; otherwise document “no escape in v1” and lock it here

**Close gate**

- `Name LIKE 'CCleaner%'` matches `CCleaner64.exe`
- `Name == 'CCleaner%'` does not
- `Name LIKE '%EDGE%'` matches `msedge`
- `Name LIKE 'ms?'` matches `ms1`, not `msedge`

---

## 8. Phase 5 — Processes host adapter

**Ship in `Vestigium.Helpers.Processes`**

```csharp
ProcessHelper.Search(string query, ProcessDetailLevel level = Slim, int maxResults = 256)
ProcessHelper.Watch(string query, TimeSpan interval, ProcessWatchFields fields)
ProcessCampaignRecipe.Query   // optional; Match term+mode remains
```

- Bind `ProcessInfo` + watcher sample + `SystemCounters` to `IKqlRow`
- Denied / Unsupported fields → unknown
- CPU % unknown on a single snapshot
- Empty query box in the gallery still means List-all; a string with `==` `LIKE` `&&` `||` uses Kql
- Simple StartsWith/EndsWith/Contains **stay**. Do not delete them.

**Close gate**

- `Search("PID == " + Environment.ProcessId)` returns the host
- `Search("Name LIKE '%testhost%'")` returns the host
- Campaign recipe can store the query string and still write JSONL
- Kql project still has **no** reference to Processes

---

## 9. Phase 6 — Demo gallery

Tabs:

- Catalog (enabled pack + field list + aliases)
- Query (text, compile errors, fixture or live Process rows)
- JSONL audit (shared `JsonlTab`)

`HelperWpfHost.Start(this, HelperLog.AppIds.Kql)`.

**Close gate:** Demo is not a SkeletonWindow. Probe still does not parse user input.

---

## 10. Phase 7 — Harden

- Developers Guide lists real files and call shapes
- HelperLog: parse/compile/session only, never values
- `FullyQualifiedName~Kql` green
- Scoreboard rows 0–7 **Done**

---

## 11. Out of this plan

Full Kusto, regex, pipes, `in (...)`, `between`, `let`, JSONPath, per-process NET, GPU thermals, paging Priority 0–7, Services host adapter (a later plan), Network host adapter.

---

## 12. Commands

```
dotnet build src/Vestigium.Helpers.Kql/Vestigium.Helpers.Kql.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Kql
dotnet run --project src/Vestigium.Helpers.Kql.Demo/Vestigium.Helpers.Kql.Demo.csproj
```

After Phase 5 also:

```
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Process
```
