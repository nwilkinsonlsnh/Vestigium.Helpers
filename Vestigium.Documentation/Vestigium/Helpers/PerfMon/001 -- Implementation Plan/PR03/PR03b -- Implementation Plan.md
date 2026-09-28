# PerfMon family — PR03b implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR03B
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** PR03a
**Blocked by:** a dump whose shard `categories` include counters for these objects

| Probe | Category | Today |
|---|---|---|
| Gpu | GPU Engine | empty shard |
| Gpu | GPU Process Memory | empty shard |
| Gpu | GPU Adapter Memory | empty shard |
| Gpu | GPU Local Adapter Memory | empty shard |
| Gpu | GPU Non Local Adapter Memory | empty shard |
| Cpu | Processor Information | name-only |
| Cpu | Processor Performance | name-only |

USNCDTWIN01 did not fill these. A later dump on a GPU / server SKU might.

---

## 1. Goal

When the Cpu or Gpu shard lists counters for a row above, generate the class (`GPUEngine`, `ProcessorInformation`, `ProcessorPerformance`) and point `CounterSet` known lists at `*.Counters`. Short jobs keep their current defaults. `PG01_` / `PC01_` stay green.

If the shard is still empty, this paper does not invent names.

## 2. What this version is not

Hyper-V Logical Processor. `System` as a Cpu catalog object. Changing Gpu short-job counters before types exist.

## 3. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR03b.001** | P0 | Re-dump + `-Probe Gpu`. If `categoryCount` is 0, stop and leave name-only. | Shard committed either empty or filled. |
| **PR03b.002** | P0 | If filled: generate five Gpu classes. Wire `GpuCounterCatalog`. | `PR03b_002_*`. `PG01_` green. |
| **PR03b.003** | P0 | Re-dump + `-Probe Cpu`. Generate Information / Performance only when counters exist. | `PC01_` green. |
| **PR03b.004** | P1 | No type named `HyperVHypervisorLogicalProcessor` in Cpu. | Name scan. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03b_
```

## 4. Still parked

ReFS / Storport / WinNAT / Hyper-V Dynamic Memory (PR03c).

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Gpu + extra Processor objects, dump-gated. |
