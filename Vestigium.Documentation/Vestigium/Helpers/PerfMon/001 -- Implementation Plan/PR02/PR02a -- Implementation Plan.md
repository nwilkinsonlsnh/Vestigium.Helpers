# PerfMon family — PR02a implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02A
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Scope:** Family ingest. No short-job behavior change in this paper.
**Dump:** `USNCDTWIN01` 28 Sep 2026. Client SKU. Not a union of Windows.
**Siblings:** [`PR02b`](PR02b%20--%20Implementation%20Plan.md), [`PR02c`](PR02c%20--%20Implementation%20Plan.md)
**Backlog:** [`PR02 -- Backlog.md`](PR02%20--%20Backlog.md)

Build the ingest path so later papers emit `NetworkAdapter.BytesTotalPerSec` without hand-maintaining counter strings.

---

## 1. Goal

1. A filtered shard exists per probe: only categories from that probe’s `*Objects.All`.
2. A generator turns a shard into one C# class per category that has a non-empty counter list.
3. Shared `Vestigium.Helpers.PerfMon` still has no probe category types.

Done when a fixture generates `PagingFile` from a tiny shard and the class matches the JSON.

## 2. What this version is not

Generating all 63 Network classes. Sampling values. Adding CLR or SQL. Changing `SampleJob`. Committing raw `pdh-all.json`.

## 3. Locked rules

| # | Rule |
|---|---|
| 1 | Type name = dump `identifier` for the category (`NetworkAdapter`). |
| 2 | Member name = dump `identifier` for the counter (`BytesTotalPerSec`). |
| 3 | `public const string Category` is the exact PDH category string. |
| 4 | Each counter is `public const string BytesTotalPerSec = "Bytes Total/sec";` |
| 5 | `public static IReadOnlyList<string> Counters` lists those PDH spellings, dump order. |
| 6 | Empty `counters` or category missing from the dump: do **not** emit a class. The `*Objects` constant stays. |
| 7 | Do not commit raw `pdh-all.json` (instance names, SQL, CLR, process list). Commit filtered shards only. |
| 8 | Shard path: `src/Vestigium.Helpers.PerfMon.<Probe>/EventCatalog/pdh-categories.json` |
| 9 | One file per generated class under `src/Vestigium.Helpers.PerfMon.<Probe>/Catalog/<Identifier>.cs`. |
| 10 | Tools live in `tools/PerfMon/`. They do not reference Charts / Analytics / Helpers.Network. |
| 11 | Allow-list is `*Objects.All` for that probe. A dump category not on the list is ignored. |

Identifier rules already in `Dump-PdhCatalog.ps1`:

- `% Usage` → `PercentUsage`
- `Bytes Total/sec` → `BytesTotalPerSec`
- category `Network Adapter` → `NetworkAdapter`

## 4. Type fingerprint

```csharp
namespace Vestigium.Helpers.PerfMon.Network;

public static class NetworkAdapter
{
    public const string Category = "Network Adapter";
    public const string BytesTotalPerSec = "Bytes Total/sec";
    public static IReadOnlyList<string> Counters { get; } = [BytesTotalPerSec /* … */];
}
```

`CounterSet` known lists for a generated category **become** `NetworkAdapter.Counters`. No second vocabulary.

`NetworkObjects.NetworkAdapter` (the string `"Network Adapter"`) remains valid. New code prefers `NetworkAdapter.Category`.

## 5. Tools

| Script | Role |
|---|---|
| `tools/PerfMon/Dump-PdhCatalog.ps1` | Already landed. Machine dump. |
| `tools/PerfMon/Filter-PdhCatalog.ps1` | Dump JSON + allow-list → shard (category, identifier, type, counters only). |
| `tools/PerfMon/New-PdhCatalogClasses.ps1` | Shard → `Catalog/<Identifier>.cs`. |

Do not `cd tools/PerfMon` inside a script. Use `$PSScriptRoot`.

## 6. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02a.001** | P0 | `Filter-PdhCatalog.ps1`. Allow-list in, shard out. Drop instances, help optional. | Fixture: Paging File only. |
| **PR02a.002** | P0 | `New-PdhCatalogClasses.ps1`. Shard → `Catalog/PagingFile.cs`. | `PagingFile.PercentUsage == "% Usage"`. |
| **PR02a.003** | P0 | Agreement helper in tests: shard JSON ↔ generated class ↔ `CounterSet` known list. | `PR02a_003_*`. |
| **PR02a.004** | P1 | `tools/PerfMon/README.md` documents dump → filter → generate. | No nested `cd`. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02a_
```

## 7. Still parked

Full probe emission (PR02b / PR02c). Second dump from a GPU / Hyper-V box.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Fingerprint + generator. |
