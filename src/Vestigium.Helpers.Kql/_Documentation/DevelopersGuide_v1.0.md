# Vestigium.Helpers.Kql — Developers Guide

**Document ID:** VEST-HLP-KQL-DEV-000  
**Version:** 1.2  
**Status:** Matches the shipped engine (PR02)  
**Date:** 11 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

TFM `net10.0`. Façade `KqlHelper`. APPID `Kql`. This package does not enumerate processes, services, or counters. It does not reference `Vestigium.Helpers.Processes`.

---

## 1. Session

```csharp
using var session = KqlHelper.Create(KqlPack.Process);
session.TryGetField("CPU.PrivateBytes", out var field);
// field.Canonical == "MEM.PrivateBytes"
// field.WatchOnly is true for CPU.Usage and *Delta fields
```

`Create` can take several packs or `KqlOptions` (`Packs`, `Groups`).  
`Groups = None` means the pack defaults.

| Pack | Default groups |
|---|---|
| Process | Proc, Cpu, Mem, Io, Gpu |
| Service | Svc |
| Thread | Thr, Cpu |
| System | Sys, Cpu, Mem, Gpu, Disk, Io, Net |
| Adapter | Gpu, Net |

Aliases: `PID` → `PROC.Pid`, `Name` → `PROC.Name` (Process) or `SVC.Name` (Service), `CPU.PrivateBytes` / `RAM.PrivateBytes` → `MEM.PrivateBytes`.

Unknown-field compile error:

```text
unknown field 'PID' on pack=Adapter. enabled (Gpu,Net): GPU.Usage, … +N
```

---

## 2. Parse vs compile

```csharp
var parsed = KqlHelper.Parse("(PID == 10 || Name LIKE '%edge%') && GPU.Usage GT 20");
var compiled = KqlHelper.Compile(text, session);
```

`A | where B` is a parse error (`pipe is not supported`).

---

## 3. Evaluate

Missing, denied, unsupported, or blank string values are **unknown** — never `""`.  
`unknown && false` = false. `unknown || true` = true. Top-level unknown is **not** a hit.

---

## 4. Operators

| Operator | Notes |
|---|---|
| `==` `!=` `<>` | exact; `*` `%` `?` are literals; warning if those chars appear |
| `LIKE` / `NOT LIKE` / `!LIKE` | wildcards |
| `IN ('a','b')` / `NOT IN (...)` | literals only |
| `BETWEEN 1 AND 3` | inclusive; `AND` not `&&` inside BETWEEN |
| `GT` `LT` `GE` `LE` `<` `>` | |

Type mismatch (no RHS text):

```text
field=PROC.Pid type=Integer op=== rhs=String
```

---

## 5. Processes host

`Vestigium.Helpers.Processes` references Kql. Kql does not reference Processes.

```csharp
ProcessHelper.Search("PID == " + Environment.ProcessId);
ProcessHelper.Watch("Name LIKE '%EDGE%'", TimeSpan.FromSeconds(1), ProcessWatchFields.All);
ProcessHelper.SearchThreads(pid, "TID == 12");
ProcessHelper.MatchSystem("SYS.ProcessCount GT 0");

var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
{
    Name = "edge-mornings",
    Query = "Name LIKE '%EDGE%'",   // Match is optional when Query is set
    Windows = [ new("midnight", new TimeOnly(0, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All) ]
});
```

`Search(term, StartsWith|EndsWith|Contains)` is unchanged.

---

## 6. Logging

HelperLog only. Never log RHS strings, command lines, or passwords.
