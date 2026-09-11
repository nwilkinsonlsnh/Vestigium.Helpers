# Vestigium.Helpers.Kql — Developers Guide

**Document ID:** VEST-HLP-KQL-DEV-000  
**Version:** 1.1  
**Status:** Matches the shipped engine  
**Date:** 10 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)  
**Counters:** [`Counters_v1.0.md`](Counters_v1.0.md)  
**Logging:** [`Logging_v1.0.md`](Logging_v1.0.md)

TFM `net10.0`. Façade `KqlHelper`. APPID `Kql`. This package does not enumerate processes, services, or counters.

---

## 1. Session

```csharp
using var session = KqlHelper.Create(KqlPack.Process);
session.TryGetField("CPU.PrivateBytes", out var field);
// field.Canonical == "MEM.PrivateBytes"
```

`Create` can take several packs or `KqlOptions` (`Packs`, `Groups`).  
`Groups = None` means the pack defaults. A query cannot see a disabled group.

| Pack | Default groups |
|---|---|
| Process | Proc, Cpu, Mem, Io, Gpu |
| Service | Svc |
| Thread | Thr, Cpu |
| System | Sys, Cpu, Mem, Gpu, Disk, Io, Net |
| Adapter | Gpu, Net |

Aliases: `PID` → `PROC.Pid`, `Name` → `PROC.Name` (Process) or `SVC.Name` (Service), `CPU.PrivateBytes` / `RAM.PrivateBytes` → `MEM.PrivateBytes`.

---

## 2. Parse vs compile

```csharp
var parsed = KqlHelper.Parse("(PID == 10 || Name LIKE '%edge%') && GPU.Usage GT 20");
// Ok even if GPU.Usage is not in this session.

var compiled = KqlHelper.Compile(text, session);
// Fails if a name is not enabled. Error lists enabled canonical fields.
```

`A | where B` is a parse error (`pipe is not supported`).

---

## 3. Evaluate

```csharp
var row = new KqlFixtureRow(session)
    .Set("Name", "msedge")
    .Set("PID", 10);

if (compiled.Query!.Matches(row))   // true only
    hits.Add(row);

var state = compiled.Query.Evaluate(row);  // True | False | Unknown
```

Missing, denied, or unsupported values are **unknown**.  
`unknown && false` = false. `unknown || true` = true. Top-level unknown is **not** a hit.

Hosts implement `IKqlRow.Get(canonical)` and return `KqlValue.Unknown` when the cell is empty.

---

## 4. LIKE vs exact

| Operator | Wildcards `*` `%` `?` |
|---|---|
| `LIKE` / `NOT LIKE` / `!LIKE` | honored (`*`/`%` = any run, `?` = one char) |
| `==` `!=` `<>` | **literals** |

`Name LIKE 'CCleaner%'` matches `CCleaner64.exe`.  
`Name == 'CCleaner%'` matches only the name `CCleaner%`.

If an exact-compare string contains `*` `%` `?`, compile still succeeds and HelperLog writes:

```text
exact compare treats wildcard chars as literals field=PROC.Name op=== chars=%
```

APPID `Kql`, subcategory `Query`. The RHS string is not logged.

String `==` is ordinal ignore case. Keywords are case-insensitive.

---

## 5. Processes host

`Vestigium.Helpers.Processes` references Kql. Kql does not reference Processes.

```csharp
ProcessHelper.Search("PID == " + Environment.ProcessId);
ProcessHelper.Watch("Name LIKE '%EDGE%'", TimeSpan.FromSeconds(1), ProcessWatchFields.All);

var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
{
    Name = "edge-mornings",
    Match = new ProcessSearchRequest { Term = "unused", Mode = ProcessSearchMode.Contains },
    Query = "Name LIKE '%EDGE%'",
    Windows = [ new("midnight", new TimeOnly(0, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All) ]
});
```

`Search(term, StartsWith|EndsWith|Contains)` is unchanged. Empty UI box = list-all, not a Kql parse.

---

## 6. Logging

HelperLog only. Never `VestigiumLogger.Initialize` from this library.  
Demo host: `HelperWpfHost.Start(this, HelperLog.AppIds.Kql)`.

Probe describes the catalog. It does not parse a user query box.
