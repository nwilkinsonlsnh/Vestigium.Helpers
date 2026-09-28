# PerfMon family — PR02a implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR02A
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Scope:** Family. No probe short-job changes in this paper.
**Dump:** `USNCDTWIN01` 28 Sep 2026. Client SKU. Not a union of Windows.

Build the ingest path so later papers can emit `NetworkAdapter.BytesTotalPerSec` without hand-maintaining counter strings.

---

## 1. Goal

1. A filtered shard exists per probe: only categories from that probe’s `*Objects.All`.
2. A generator turns a shard into one C# class per category.
3. Shared `Vestigium.Helpers.PerfMon` still has no probe category types.

Done when a fixture can generate `PagingFile` from a tiny shard and the class matches the JSON.

## 2. What this version is not

Generating all 63 Network classes. Sampling values. Adding CLR or SQL. Changing `SampleJob`.

## 3. Locked rules

| # | Rule |
|---|---|
| 1 | Type name = dump `identifier` for the category (`NetworkAdapter`). |
| 2 | Member name = dump `identifier` for the counter (`BytesTotalPerSec`). |
| 3 | `public const string Category` is the exact PDH category string. |
| 4 | Each counter is `public const string BytesTotalPerSec = "Bytes Total/sec";` |
| 5 | `public static IReadOnlyList<string> Counters` lists those PDH spellings, dump order. |
| 6 | Empty `counters` in the dump: do **not** emit a class. The `*Objects` constant stays. |
| 7 | Do not commit raw `pdh-all.json` (instance names, SQL, CLR). Commit filtered shards. |
| 8 | Shard path: `src/Vestigium.Helpers.PerfMon.<Probe>/EventCatalog/pdh-categories.json` |
| 9 | One file per generated class under `src/.../Catalog/` (small files). |
| 10 | Generator lives in `tools/PerfMon/`. It does not reference Charts / Analytics / Helpers.Network. |

Identifier rules already in `Dump-PdhCatalog.ps1`:

- `% Usage` → `PercentUsage`
- `Bytes Total/sec` → `BytesTotalPerSec`
- leading `# ` dropped or becomes `Of…` as the dump already did
- category `Network Adapter` → `NetworkAdapter`

## 4. Type fingerprint

```csharp
namespace Vestigium.Helpers.PerfMon.Network;

public static class NetworkAdapter
{
    public const string Category = "Network Adapter";
    public const string BytesTotalPerSec = "Bytes Total/sec";
    public static IReadOnlyList<string> Counters { get; }
}
```

`CounterSet` known lists for a generated category **become** `NetworkAdapter.Counters`. No second vocabulary.

## 5. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR02a.001** | P0 | Filter tool: dump JSON + allow-list → shard (category, identifier, type, counters only). | Fixture on Paging File. |
| **PR02a.002** | P0 | Generator: shard → `Catalog/<Identifier>.cs`. | `PagingFile.PercentUsage == "% Usage"`. |
| **PR02a.003** | P0 | Agreement test helper used by later papers: JSON ↔ class ↔ `CounterSet`. | One example test. |
| **PR02a.004** | P1 | README in `tools/PerfMon` for filter + generate. | No `cd tools/PerfMon` inside the script. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02a_
```

## 6. Still parked

Full probe emission (PR02b / PR02c). Second dump from a GPU / Hyper-V box.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Fingerprint + generator. |
