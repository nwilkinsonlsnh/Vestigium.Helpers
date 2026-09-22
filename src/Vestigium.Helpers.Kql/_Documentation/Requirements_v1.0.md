# Vestigium.Helpers.Kql — Requirements Specification

**Document ID:** VEST-HLP-KQL-SRS-000  
**Version:** 1.0-draft  
**Status:** Draft (brainstorm). Not Accepted. Do not grow a parser past Identity/Probe until Status is Accepted.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Kql` (product name: **KQL**)  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Build plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)

If implementation and this file disagree, this file wins after Acceptance.

This library is not Processes, not Services, and not Azure Data Explorer. It owns a **filter dialect** (KQL-inspired predicates) and a **field catalog** (groups you enable at session start). Hosts bind values. Kql does not open process handles or query SCM.

---

## 0. Purpose

One way for a Vestigium host to say:

```text
(PID == 45944 || Name LIKE 'CCleaner%') && Name LIKE '%EDGE%'
GPU.Usage GT 20 && MEM.PrivateBytes > 104857600
```

and get back a compiled predicate plus errors with line/column — not a string `eval`.

Hosts (Processes, later Services / Network) enable the groups they can actually fill.

---

## 1. Decisions to lock (draft)

| # | Decision | Draft lock |
|---|---|---|
| 1 | Project | `Vestigium.Helpers.Kql`. Façade `KqlHelper`. APPID `Kql`. |
| 2 | Dialect | **Filter subset only.** `where`-style predicates. No pipes, `project`, `summarize`, `join`, `let`, or regex in v1. |
| 3 | Compatibility | KQL-*inspired*. Not Azure Kusto compatible. Do not claim “drop in a Kusto query.” |
| 4 | Groups | Catalog is grouped (`CPU`, `GPU`, `MEM`/`RAM`, `DISK`, `IO`, `NET`, plus entity packs). Groups are enabled at session init. |
| 5 | Unknown field | Compile error listing enabled fields. Never silent ignore. |
| 6 | Missing value | Three-valued logic: true / false / unknown. Denied and Unsupported are unknown. Top-level unknown is not a hit. |
| 7 | Wildcards | Only on `LIKE` / `!LIKE`. `*` and `%` = any run. `?` = one char. `==` is exact. |
| 8 | Case | Identifiers and keywords case-insensitive. String compare ordinal ignore case. |
| 9 | Enablement | `KqlSession` is created with packs/groups. A query cannot see a disabled group. |
| 10 | Packs | Built-in packs (`Process`, `Service`, `System`, …) are **catalog presets**, not data providers. |
| 11 | Expansion | Adding a field is catalog data, not a parser change. |
| 12 | Logging | HelperLog only. Paths, field names, error codes. Never row values or credentials. |
| 13 | Siblings | Core Helpers only. No reference to Processes / Services / Network / Charts in this package. |
| 14 | TFM | `net10.0`. |

---

## 2. Two axes (this is the model)

Do not mix “what the row is” with “what resource the number came from.”

**Entity axis** — the row:

| Pack | Row |
|---|---|
| `Process` | one OS process |
| `Thread` | one thread |
| `Service` | one SCM service |
| `System` | the machine snapshot |
| `Adapter` | one GPU or NIC |

**Resource axis** — the groups you enable:

| Group | Aliases | Meaning |
|---|---|---|
| `CPU` | | processor time / % |
| `GPU` | | adapter or per-process GPU |
| `MEM` | `RAM` | working set, private, commit |
| `DISK` | | filesystem / paging file |
| `IO` | | read/write counts and bytes |
| `NET` | | later |
| `PROC` | | identity: PID, PPID, Name, ImagePath, … |

A session is: **one entity pack + zero or more resource groups**.

```text
KqlHelper.Create(KqlPack.Process)           // PROC + CPU + MEM + IO + GPU fields the process row can fill
KqlHelper.Create(KqlPack.Service)           // service identity + state + optional PID join fields
KqlHelper.Create(KqlPack.System, KqlGroup.Gpu | KqlGroup.Mem)
```

That is what “when initiating the library, enable the groupings that make sense” means.

---

## 3. Field table (v1 draft)

Canonical name is `Group.Field`. Aliases are first-class so Task Manager language and KQL language can coexist.

### PROC (entity: Process)

| Canonical | Aliases | Type | Notes |
|---|---|---|---|
| `PROC.Pid` | `PID`, `Pid` | int | always present |
| `PROC.ParentPid` | `PPID` | int? | |
| `PROC.Name` | `Name` | string | |
| `PROC.ImagePath` | `Image`, `Path` | string? | |
| `PROC.CommandLine` | `Cmd` | string? | do not log |
| `PROC.Session` | `SessionId` | int? | |

### CPU

| Canonical | Aliases | Type | Notes |
|---|---|---|---|
| `CPU.Usage` | `CPU`, `CPU.Percent` | number? | needs two samples; unknown on a single snapshot |
| `CPU.Time` | `CPU.Total` | timespan? | |

`CPU.PrivateBytes` is **not** a CPU field. Accept it as an alias of `MEM.PrivateBytes` so the Task Manager grouping still types. Document the alias; do not invent a second counter.

`CPU.IO` is an alias **group** of `IO.*` (see IO). Same rule.

### MEM / RAM

| Canonical | Aliases | Type |
|---|---|---|
| `MEM.PrivateBytes` | `RAM.PrivateBytes`, `CPU.PrivateBytes`, `PrivateBytes` | long? |
| `MEM.WorkingSet` | `RAM.WorkingSet`, `WorkingSet` | long? |
| `MEM.Commit` | `RAM.Commit`, `Commit` | long? |

### GPU

| Canonical | Aliases | Type |
|---|---|---|
| `GPU.Usage` | `GPU.UsagePercent` | number? |
| `GPU.DedicatedMemory` | `GPU.DedicatedBytes` | long? |
| `GPU.SystemMemory` | `GPU.SharedBytes`, `GPU.SystemBytes` | long? |
| `GPU.CommittedMemory` | `GPU.Commit` | long? |

Missing GPU counter → unknown + host marks Unsupported. Never bind `0`.

### IO (and `CPU.IO.*`)

| Canonical | Aliases | Type |
|---|---|---|
| `IO.Reads` | `CPU.IO.Reads` | long? |
| `IO.ReadBytes` | `CPU.IO.ReadBytes` | long? |
| `IO.Writes` | `CPU.IO.Writes` | long? |
| `IO.WriteBytes` | `CPU.IO.WriteBytes` | long? |
| `IO.ReadDelta` | | long? |
| `IO.WriteDelta` | | long? |

### DISK (system pack first)

| Canonical | Type | Notes |
|---|---|---|
| `DISK.ReadDelta` | long? | system paging / disk |
| `DISK.WriteDelta` | long? | |
| `DISK.PagingFileWriteDelta` | long? | |

v1 may ship DISK only on `KqlPack.System`. Per-process disk is IO.

### Service pack (preset)

| Canonical | Aliases | Type |
|---|---|---|
| `SVC.Name` | `Name` | string |
| `SVC.DisplayName` | | string? |
| `SVC.Status` | `State` | string |
| `SVC.StartType` | | string? |
| `SVC.Pid` | `PID` | int? |

Calling `KqlPack.Service` enables that table. It does not query services. `Vestigium.Helpers.Services` binds the row.

---

## 4. Expanding a group

The parser only knows `ident ( '.' ident )*`. New counters are catalog rows.

```csharp
catalog.Group("GPU")
    .Field("Usage", KqlType.Number)
    .Field("DedicatedMemory", KqlType.Integer)
    .Alias("DedicatedBytes");
```

A host may add a private group (`APP.Foo`) without forking Kql. Built-in packs stay in this library so Processes and the gallery share one spelling.

---

## 5. Session init

```csharp
public static class KqlHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Kql"
    public static string Probe();

    public static KqlSession Create(params KqlPack[] packs);
    public static KqlSession Create(KqlOptions options);
    public static KqlResult Parse(string text);                  // syntax only
    public static KqlBoundQuery Compile(string text, KqlSession session);
}

public sealed class KqlOptions
{
    public IReadOnlyList<KqlPack> Packs { get; init; }      // Process, Service, System, …
    public KqlGroups Groups { get; init; }                  // extra resource flags
    public IKqlCatalog? Extra { get; init; }                // host fields
}
```

`Compile` fails if the query names a group the session did not enable.

---

## 6. Dialect (v1)

```text
(PID == 45944 || Name LIKE 'CCleaner%') && GPU.Usage GT 20
```

| Kind | Tokens |
|---|---|
| Logic | `&&` `\|\|` `AND` `OR` `NOT` `(` `)` |
| Compare | `==` `!=` `<>` `<` `>` `<=` `>=` `GT` `LT` `GE` `LE` |
| String | `LIKE` `!LIKE` `NOT LIKE` |
| Wildcards | `*` `%` any run; `?` one char — **LIKE only** |

No regex. No pipes. No `in (...)` in v1 unless we accept it before code starts.

Three-valued logic: Denied / Unsupported / “CPU % on first snapshot” → unknown. `unknown && false` is false. `unknown || true` is true. A top-level unknown is not a match (fail closed for Search/Kill).

---

## 7. What this package does not do

- Read processes, services, or performance counters
- Schedule campaigns (that stays in Processes / Network)
- Speak full Kusto (`\| where`, `summarize`, `join`)
- `eval` a string against a live object graph

---

## 8. Acceptance gate (when we leave draft)

1. This file + Guide + Plan live under `src/Vestigium.Helpers.Kql/_Documentation/`.
2. APPID `Kql` registered in `HelperLog`.
3. Solution entries reserved (library + Demo skeleton) **after** Acceptance, not before.
4. Field table in §3 is reviewed: especially `CPU.PrivateBytes` → alias of `MEM.PrivateBytes`, and `CPU.IO` → `IO`.
5. No parser work until Status = Accepted.
