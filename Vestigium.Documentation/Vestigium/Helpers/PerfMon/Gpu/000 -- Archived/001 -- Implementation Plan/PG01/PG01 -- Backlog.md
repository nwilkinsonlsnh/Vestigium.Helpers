# Vestigium.Helpers.PerfMon.Gpu — PG01 backlog

**Status:** Open.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Gpu` 0.1.0

| Door | Item | Note |
|---|---|---|
| In | Five-object catalog | Engine, Process Memory, Adapter Memory, Local, Non Local |
| In | Short job | Engine utilization + adapter dedicated |
| In | Live instances | Opaque strings, cap 256, no invented `_Total` |
| In | Logging door | APPID `PerfMon.Gpu` |
| In | EVENTID 18500–18545 | Block through 18999 |
| Out | NVML / ADL / DXGI public API | Non-goal |
| Out | Per-PID filter helper | Later if a host asks |
| Out | Charts / Analytics | Neighbors |
| Out | NuGet publish | After a host consumes this |
