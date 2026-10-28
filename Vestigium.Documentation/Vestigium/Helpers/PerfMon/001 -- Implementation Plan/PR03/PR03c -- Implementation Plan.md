# PerfMon family — PR03c implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR03C
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** PR03a
**Blocked by:** dump rows with non-empty `counters`

Generate typed classes for allow-listed objects that PR02 left name-only.

---

## 1. Still name-only after PR02

**Memory:** Hyper-V Dynamic Memory Integration Service

**Disk:** FileSystem Disk Activity, Ntfs Bucketized Performance, ReFS*, Storage Spaces*, Storport*, VHD Bucketized Performance

**Network:** the 53 objects not in the present-ten set (IPsec, WFP, WinNAT, SMB, HTTP, Teredo, PacketDirect, Bluetooth, …)

## 2. Goal

For each name above, if the probe shard now has counters, emit `Catalog/<Identifier>.cs` and set `CounterSet` known list to that `Counters` array. Do not add the name to a short job unless a later paper says so.

`ME01_`, `PD01_`, `PN01_` stay green. Short jobs stay PhysicalDisk / Network Interface / Memory / Paging File.

## 3. What this version is not

New allow-list entries (`Storage Spaces Write Cache`, `BranchCache`, `RDMA Activity`, `Process`). CLR / SQL. Helpers.Network.

## 4. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR03c.001** | P0 | Re-filter Memory / Disk / Network from the current dump. | Commit shards only if `categories` grew. |
| **PR03c.002** | P0 | Generate classes for new non-empty rows. | Agreement tests per new type. |
| **PR03c.003** | P0 | Objects still missing stay name-only. Assert no invented type. | `PR03c_003_*`. |
| **PR03c.004** | P1 | Shared-only project refs unchanged. | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03c_
```

## 5. Still parked

A second family paper if we ever want short jobs to sample WinNAT or ReFS by default.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Remainder objects, dump-gated. |
