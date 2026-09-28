# Vestigium.Helpers.PerfMon.Disk — PD01 backlog

**Status:** Open. PhysicalDisk job.
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon.Disk` 0.1.0

| Door | Item | Note |
|---|---|---|
| Landed | `DiskCounterCatalog` | 19 objects. PM02 fingerprint |
| In | PhysicalDisk short job | Rates, latency, queue on `_Total` |
| In | Named instance | `0 C:` kept as-is |
| In | `IncludeDisks` | Live instances minus `_Total`, cap 256 |
| In | Logging door | APPID `PerfMon.Disk` |
| In | EVENTID 19500–19545 | Block through 19999 |
| Out | LogicalDisk free-space job | Later plan |
| Out | ReFS / Storage Spaces / Storport / VHD jobs | Catalog only this plan |
| Out | SMART / FileIo | Neighbors |
| Out | Charts / Analytics | Neighbors |
| Out | NuGet publish | After a host consumes this |
