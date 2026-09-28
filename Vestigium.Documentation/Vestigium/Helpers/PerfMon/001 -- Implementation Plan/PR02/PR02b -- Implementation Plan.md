# PerfMon family — PR02b implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02B
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** PR02a generator
**Dump hits this paper uses**

| Probe | Category | Counters on USNCDTWIN01 |
|---|---|---|
| PageFile | Paging File | 2 |
| Memory | Memory | 36 |
| Memory | Cache | 29 |
| Memory | NUMA Node Memory | 4 |
| Memory | ReadyBoost Cache | 10 |
| Cpu | Processor | 15 |
| Disk | PhysicalDisk | 21 |
| Disk | LogicalDisk | 23 |

Hyper-V Dynamic Memory, Processor Information, Processor Performance, and the other 17 Disk objects are **not** in this dump. They stay name-only on `*Objects`. Do not invent counters.

---

## 1. Goal

Each row above becomes a typed class. `CounterSet` known lists for those categories are the class `Counters` arrays. Short jobs keep the same default counters; they start reading the constants instead of raw strings.

## 2. What this version is not

Network. Gpu. New Disk objects. Changing interval math.

## 3. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02b.001** | P0 | PageFile shard + `PagingFile` class. `PageFilePaths` uses `PagingFile.PercentUsage` / `PercentUsagePeak`. | `PF01_` still green. New `PR02b_001_*`. |
| **PR02b.002** | P0 | Memory four classes. `MemoryPaths.ShortCounters` uses `Memory.AvailableMBytes` and siblings. | `ME01_` green. |
| **PR02b.003** | P0 | Cpu `Processor` class. Processor Information / Performance stay name-only. | `PC01_` green. |
| **PR02b.004** | P0 | Disk `PhysicalDisk` + `LogicalDisk`. Other DiskObjects stay name-only. | `PD01_` green. |
| **PR02b.005** | P1 | Filter `PR02b_`. No CLR / SQL types in these projects. | Project ref scan. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02b_
```

## 4. Still parked

Cpu Information / Performance classes until a dump lists them. ReFS / Storport / Spaces / VHD until a dump lists them.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Client-SKU core probes. |
