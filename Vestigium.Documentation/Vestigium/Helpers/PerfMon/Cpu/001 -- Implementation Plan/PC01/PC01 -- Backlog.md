# Vestigium.Helpers.PerfMon.Cpu — PC01 backlog

**Status:** Open
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Cpu` 0.1.0

| Door | Item | Note |
|---|---|---|
| In | `CpuPerf` façade | Hosts do not construct PDH objects |
| Landed | `CpuCounterCatalog` | Processor / Processor Information / Processor Performance |
| In | Default job path set | Processor Time, Privileged, User, Queue, Parking when present |
| In | Default instance `_Total` | Per-core is opt-in |
| In | Category fallback | `Processor Information` then `Processor` |
| In | Logging door | `CpuPerfCatalog.Register(VestigiumLoggerOptions)` |
| In | EVENTID 18000–18045 | Block through 18499 |
| In | Tests `PC01_*` | Fake `ICounterSource` only closes the plan |
| Out | PID / per-process CPU | `Helpers.Processes` |
| Out | Affinity or frequency writes | Not a probe job |
| Out | Charts / Analytics | Neighbors |
| Out | NuGet publish | After a host consumes this |
