# Vestigium.Helpers.PerfMon — PM01 backlog

**Status:** 001–009 landed. Close after local PM01_ filter is green.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon` 0.1.0

| Door | Item | Note |
|---|---|---|
| Landed | `SampleRecord`, `CounterPath`, `SampleStatus` | PM01.001 |
| Landed | `ICounterSource` + `FakeCounterSource` | PM01.002 |
| Landed | `SampleJobOptions` guards | PM01.003. Loop is still PM01.004. |
| In | Logging door | `PerfMonCatalog.Register(VestigiumLoggerOptions)` |
| In | EVENTID 17000–17045 | Block through 17499 |
| Landed | Tests `PM01_*` | PM01.009. Filter `FullyQualifiedName~PM01_` |
| Out | Any `Vestigium.Helpers.PerfMon.*` probe | Next plans |
| Out | Live PDH as CI gate | `PerformanceCounterSource` exists; missing category is skip |
| Out | Analytics / Charts | Neighbors |
| Out | NuGet publish | After a probe can consume this |
