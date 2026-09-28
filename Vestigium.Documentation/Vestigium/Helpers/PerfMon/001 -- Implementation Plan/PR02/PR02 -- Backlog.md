# PerfMon family — PR02 backlog

**Status:** Open.
**Date:** 28 September 2026
**Dump:** `USNCDTWIN01` `pdh-all.json` (113 categories, 28 Sep 2026). Filter only.
**Do not import:** `.NET CLR *`, `SQLServer:*`, `Thread`, `Process`, `Browser`, `Telephony`.

| Door | Item | Paper |
|---|---|---|
| In | `NetworkAdapter.BytesTotalPerSec` shape | PR02a |
| In | Filtered shards per probe (no instance lists) | PR02a |
| In | Generator: one small class per category that has counters | PR02a |
| In | PageFile, Memory, Processor, PhysicalDisk, LogicalDisk typed | PR02b |
| In | Ten Network categories present on this dump typed | PR02c |
| Out | CLR / SQL / Thread / Process / Browser types | — |
| Out | Empty-class generation for missing categories | PR02c |
| Out | New sample loop | — |
| Out | Helpers.Network merge | — |
| Out | NuGet publish | — |
