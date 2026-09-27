# Vestigium.Helpers.PerfMon — PM02 catalog fingerprint

**Document ID:** VEST-HLP-PERFMON-PLAN-PM02
**Version:** 1.0
**Status:** Locked
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon` 0.1.0
**First consumer:** `Vestigium.Helpers.PerfMon.Cpu` (PC01)

This is the pattern Disk / Gpu / Memory / Network / PageFile copy.
Do not invent a second inventory, a second watch, or a live list that lies with the known list.

---

## 1. Split

| Lives in | Owns |
|---|---|
| Shared `Vestigium.Helpers.PerfMon` | `ICounterInventory`, `PdhCounterInventory`, `CatalogSnapshot`, `CatalogWatchOptions`, `CounterSet` |
| Probe (`*.Cpu`, later `*.Disk`, …) | Object names + known counter vocabulary + thin façade + units |

Shared does not know `Processor Information`. Cpu does not talk to PDH directly.

---

## 2. Shared surface

```csharp
public interface ICounterInventory
{
    bool CategoryPresent(string category);
    bool InstancePresent(string category, string instance);
    IReadOnlyList<string> LiveCounters(string category, string instance, int cap);
    IReadOnlyList<string> LiveInstances(string category, int cap);
}

public sealed class CounterSet
{
    public CounterSet(
        IReadOnlyDictionary<string, string[]> known,
        string rejectMessage,
        Func<string, string>? unitOf = null,
        ICounterInventory? inventory = null);

    bool IsKnownCategory(string category);
    bool IsKnownCounter(string category, string counter);
    IReadOnlyList<string> KnownCounters(string category);

    bool CategoryPresent(...);
    bool HasCounter(...);
    bool HasInstance(...);

    IReadOnlyList<string> LiveCounters(...);   // empty if absent
    IReadOnlyList<string> LiveInstances(...);  // empty if absent
    CatalogSnapshot Snapshot(...);
    Task WatchAsync(CatalogWatchOptions options, Action<CatalogSnapshot> onSnapshot, ...);
    IReadOnlyList<CounterPath> Paths(...);
}
```

Default inventory is `PdhCounterInventory.Shared`. Tests inject `ICounterInventory`.

---

## 3. Rules that do not move

1. Known is vocabulary. Live is the box. Live never returns Known when the object is missing.
2. Check is `CategoryPresent` / `HasCounter` / `HasInstance`. Miss is `false`, not throw.
3. Unknown object name (not in the probe map) throws `ArgumentException`.
4. Subscribe is `WatchAsync`. Bounded: count, duration, or token. Same interval floors as `SampleJob` (1 s default, burst 50–200 ms, 24 h cap).
5. Default watch emits only when counters or instances change.
6. Cap default 256. Cap `<= 0` returns empty.
7. No `IObservable`. No dispatcher hop. No remote `\\server`.
8. Snapshot lists are frozen.

---

## 4. Probe façade fingerprint (Cpu is the first)

```csharp
public static class CpuObjects { /* PDH object names only */ }

public static class CpuCounterCatalog
{
    static readonly CounterSet Set = new(knownMap, rejectMessage, UnitOf);
    // every public method delegates to Set
}
```

Next probe: same façade name shape `{Probe}Objects` + `{Probe}CounterCatalog` + a `CounterSet` seeded with that probe's objects.

| Probe | Objects in the map |
|---|---|
| Cpu | `Processor`, `Processor Information`, `Processor Performance` |
| Disk | later plan — PhysicalDisk / LogicalDisk |
| Gpu | later plan — GPU Engine / GPU Adapter Memory when present |
| Memory | later plan — Memory |
| PageFile | later plan — Paging File |
| Network | later plan — Network Interface / TCPv4 — not `Helpers.Network` ICMP |

---

## 5. First catalog objects (Cpu)

| Object | Role |
|---|---|
| `Processor` | Classic per-instance + `_Total` |
| `Processor Information` | Preferred. >64 CPU, parking, frequency |
| `Processor Performance` | Frequency, utility, limit flags |

`System\Processor Queue Length` is a default-job path, not a third catalog object. It stays on the short `CpuPerf` job.

---

## 6. Tests that close the fingerprint

Already green under `PC01_001_*` against a scripted `ICounterInventory`:

| Fixture | Lock |
|---|---|
| three objects only | map is closed |
| known ≠ live | missing object does not return known |
| check + snapshot | `HasCounter` / `HasInstance` / `CategoryPresent` |
| watch | two snapshots of counters and instances |

Disk and later probes add `{XX}01_001_*` with the same four locks and their own object names.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PC01_001
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Locked. Shared CounterSet. Cpu is the first consumer. |
