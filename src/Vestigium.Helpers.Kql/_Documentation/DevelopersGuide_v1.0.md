# Vestigium.Helpers.Kql — Developers Guide

**Document ID:** VEST-HLP-KQL-DEV-000  
**Version:** 1.0  
**Status:** Companion to the active implementation plan  
**Date:** 10 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)  
**Counters:** [`Counters_v1.0.md`](Counters_v1.0.md)

Open `Vestigium.Helpers.slnx`. Implementation will live in `src/Vestigium.Helpers.Kql/` after Phase 0.

Until Phase 0 Accept + scaffold, there is no `KqlHelper` type. Do not grow a parser before that gate.

## Intended call shapes (after Phase 3)

```csharp
using var session = KqlHelper.Create(KqlPack.Process);
var query = KqlHelper.Compile(
    "(PID == 45944 || Name LIKE 'CCleaner%') && GPU.Usage GT 20",
    session);

foreach (var row in rows)
{
    if (query.Matches(row))  // false when result is unknown
        hits.Add(row);
}
```

Processes (Plan Phase 5) wraps that as `ProcessHelper.Search(query)`.

## Packs vs groups

`KqlPack.Process` enables PROC + CPU + MEM + IO + GPU fields a process row can fill.  
`KqlPack.Service` enables the service table only.  
`KqlPack.System` enables machine counters (commit, paging, topology, system GPU).

`CPU.PrivateBytes` is an alias of `MEM.PrivateBytes`. `RAM` is an alias of `MEM`.

## Roadmap

Build mode is [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md). One phase per close gate.
