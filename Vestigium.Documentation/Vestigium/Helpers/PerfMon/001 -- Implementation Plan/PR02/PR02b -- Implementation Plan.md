# PerfMon family — PR02b implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02B
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** [`PR02a`](PR02a%20--%20Implementation%20Plan.md)
**Dump hits this paper uses** (`USNCDTWIN01`)

| Probe | Category | Identifier | Counters |
|---|---|---|---|
| PageFile | Paging File | `PagingFile` | 2 |
| Memory | Memory | `Memory` | 36 |
| Memory | Cache | `Cache` | 29 |
| Memory | NUMA Node Memory | `NUMANodeMemory` | 4 |
| Memory | ReadyBoost Cache | `ReadyBoostCache` | 10 |
| Cpu | Processor | `Processor` | 15 |
| Disk | PhysicalDisk | `PhysicalDisk` | 21 |
| Disk | LogicalDisk | `LogicalDisk` | 23 |

Not in this dump — stay name-only on `*Objects`:

- Memory: Hyper-V Dynamic Memory Integration Service
- Cpu: Processor Information, Processor Performance
- Disk: the other 17 DiskObjects (ReFS, Storport, Spaces, VHD, NTFS bucketized, …)

Do not invent counters for a miss.

---

## 1. Goal

Each hit row becomes a typed class under that probe’s `Catalog/` folder. `CounterSet` known lists for those categories are the class `Counters` arrays. Short jobs keep the same default counters; they read the constants instead of raw strings.

`PF01_`, `ME01_`, `PC01_`, `PD01_` stay green.

## 2. What this version is not

Network. Gpu. New Disk objects. Changing interval math. Importing CLR Memory into the Memory probe.

## 3. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02b.001** | P0 | PageFile shard + `Catalog/PagingFile.cs`. `PageFilePaths` uses `PagingFile.PercentUsage` / `PercentUsagePeak`. | `PF01_` green. `PR02b_001_*`. |
| **PR02b.002** | P0 | Memory four classes. `MemoryPaths.ShortCounters` uses `Memory.AvailableMBytes` and siblings. | `ME01_` green. |
| **PR02b.003** | P0 | Cpu `Catalog/Processor.cs`. Processor Information / Performance stay name-only. | `PC01_` green. |
| **PR02b.004** | P0 | Disk `PhysicalDisk` + `LogicalDisk`. Other DiskObjects stay name-only. | `PD01_` green. |
| **PR02b.005** | P1 | Filter `PR02b_`. No CLR / SQL type names in these projects. | Project scan. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02b_
```

## 4. Still parked

Cpu Information / Performance until a dump lists them. ReFS / Storport / Spaces / VHD until a dump lists them. Hyper-V Dynamic Memory until a guest/HV dump lists it.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Client-SKU core probes. |
