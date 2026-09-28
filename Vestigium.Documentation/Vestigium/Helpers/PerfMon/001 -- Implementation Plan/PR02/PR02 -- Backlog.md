# PerfMon family — PR02 backlog

**Status:** Open.
**Date:** 28 September 2026
**Dump:** `USNCDTWIN01` `pdh-all.json` (113 categories). Filter only. Do not import CLR / SQL / Thread / Browser.

| Door | Item | Paper |
|---|---|---|
| In | Identifier `NetworkAdapter.BytesTotalPerSec` | PR02a |
| In | Filtered JSON shards per probe, no instance lists | PR02a |
| In | Generator emits one small class per category | PR02a |
| In | PageFile / Memory / Processor / PhysicalDisk / LogicalDisk typed | PR02b |
| In | Network categories present on the dump (10) typed | PR02c |
| Out | `.NET CLR *`, `SQLServer:*`, `Thread`, `Process`, `Browser` | — |
| Out | Categories missing on this SKU stay name-only | PR02c |
| Out | New sample loop | — |
| Out | Helpers.Network merge | — |
| Out | NuGet publish | — |
