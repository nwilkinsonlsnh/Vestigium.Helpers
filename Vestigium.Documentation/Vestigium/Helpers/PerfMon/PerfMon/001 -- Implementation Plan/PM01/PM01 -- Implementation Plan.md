# Vestigium.Helpers.PerfMon — PM01 implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PM01
**Version:** 1.1
**Status:** Open
**Date:** 27 September 2026
**Package:** `Vestigium.Helpers.PerfMon` 0.1.0 (not published)
**Project:** `src/Vestigium.Helpers.PerfMon/`
**TFM:** `net10.0-windows`
**Binding:** [`Requirements_v1.0.md`](../../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../../003%20--%20Design%20Document/Design_v1.0.md)
**Backlog:** [`PM01 -- Backlog.md`](PM01%20--%20Backlog.md)

Shared library only. One record. One source port. One clock. One logging door.

Cpu / Disk / Gpu / Memory / Network / PageFile do not land in this plan.

---

## 1. Goal

A later probe or a host can name paths, run a bounded job, and receive frozen `SampleRecord` rows. Missing counters are `Unavailable` with a null value. The library never calls `VestigiumLogger.Initialize`.

Done when §7 fixtures pass against `FakeCounterSource` and this project still has no probe reference.

## 2. What this version is not

- A probe façade
- A dashboard
- Analytics or Charts
- `IObservable` / dispatcher hop
- Remote `\\machine\` PDH
- Unbounded sampling
- NuGet publish

---

## 3. Shape

```
host or later probe
  → new SampleJob(paths, options)
       → options.Source ?? PerformanceCounterSource
       → prime read (dropped)
       → loop until count | duration | token
            → ICounterSource.Read(path) → SampleRecord
       → SampleJobResult (frozen)
PerfMonCatalog.Register is host-only
```

---

## 4. Public surface

Namespace: `Vestigium.Helpers.PerfMon`.

```csharp
public enum SampleStatus
{
    Ok = 0,
    Unavailable = 1,
    Partial = 2,
    Cancelled = 3,
    Rejected = 4
}

public sealed record CounterPath(
    string Category,
    string Counter,
    string Instance = "",
    string Unit = "");

public sealed record SampleRecord(
    DateTimeOffset Utc,
    string Machine,
    string Category,
    string Counter,
    string Instance,
    double? Value,
    string Unit,
    SampleStatus Status);

public sealed class SampleJobOptions
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public int InstanceCap { get; init; } = 256;
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
}

public interface ICounterSource
{
    SampleRecord Read(CounterPath path);
    IReadOnlyList<string> ListInstances(string category, int cap);
}

public sealed class SampleJob
{
    public SampleJob(IReadOnlyList<CounterPath> paths, SampleJobOptions? options = null);
    public Task<SampleJobResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed class SampleJobResult
{
    public SampleStatus Status { get; }
    public IReadOnlyList<SampleRecord> Samples { get; }
}

public static class PerfMonCatalog
{
    public const string AppId = "PerfMon";
    public const string Category = "PerfMon";
    public static void Register(VestigiumLoggerOptions cfg);
}
```

`FakeCounterSource` and `PerformanceCounterSource` stay internal. Tests reach the fake through `InternalsVisibleTo`.

`CounterPath` constructor trims. Empty category or empty counter → log `PathRejected`, then throw. Instance may be empty (Memory object). Unit may be empty in shared; probes fill it later.

`SampleRecord.Value` is null when status is `Unavailable`. A record with `Unavailable` and `0` is a defect.

---

## 5. Files

All under `src/Vestigium.Helpers.PerfMon/`. Small files. No hub.

| File | Role |
|---|---|
| `SampleStatus.cs` | Enum |
| `CounterPath.cs` | Value + guard |
| `SampleRecord.cs` | Immutable record |
| `SampleJobOptions.cs` | Options |
| `SampleJobResult.cs` | Terminal status + frozen list |
| `ICounterSource.cs` | Port |
| `FakeCounterSource.cs` | Internal test double |
| `PerformanceCounterSource.cs` | Internal PDH adapter |
| `SampleJob.cs` | Clock |
| `PerfMonEvents.cs` | Constants |
| `PerfMonLog.cs` | Internal. Same habit as `AnalyticsLog` |
| `PerfMonCatalog.cs` | Replace skeleton `Register(object)` |
| `EventCatalog/perfmon.json` | Shard |

Tests: `src/Vestigium.Helpers.Tests/PerfMonPM01Tests.cs`.
Add a project reference from Tests to `Vestigium.Helpers.PerfMon`.

Package on the shared csproj: `System.Diagnostics.PerformanceCounter` 10.0.12.

---

## 6. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PM01.001** | P0 | Status, path, record. Frozen. | Empty category throws. Unavailable value is null. |
| **PM01.002** | P0 | `ICounterSource` + fake. `ListInstances` cap default 256, truncate, no throw. | Fake returns programmed rows and misses. |
| **PM01.003** | P0 | Options guards. Interval default 1 s. `< 200 ms` needs `AllowBurst`. Burst floor 50 ms. Duration `> 24 h` rejected. Count `< 0` rejected. `Count` missing/`0` and no duration and no token → reject. Empty path list → reject. | Each reject logs `JobRejected` / `PathRejected`, then throws. |
| **PM01.004** | P0 | `RunAsync`. First limit wins. Cancel keeps samples already taken, terminal `Cancelled`. One missing instance on a tick: that row is `Unavailable`; result status becomes `Partial` if any other row succeeded. All rows unavailable on a tick: still continue; do not abort. | Fixtures in §7. |
| **PM01.005** | P0 | Prime. Job reads every path once before the first emitted tick and drops those rows. Fake may flag a path `NeedsPrime`. | First emitted row is not the prime. |
| **PM01.006** | P1 | `PerformanceCounterSource`. Local machine only. Missing category/instance → `Unavailable`. Unexpected PDH throw → `SourceThrown`, then throw. No invented zero. | Manual Windows check. Not a CI gate. |
| **PM01.007** | P1 | `PerfMonLog` + `Register(VestigiumLoggerOptions)`. APPID stamp `PerfMon`. Folder follows the host. | No-op until Initialize. Temp `LogDirectory` in tests. |
| **PM01.008** | P1 | EVENTID table in §8. Constants match `perfmon.json`. | Count by 5. |
| **PM01.009** | P2 | Tests project reference. Filter `PM01_`. | Green with no live counters. |

Do not start 006 until 001–005 are green.

---

## 7. Tests that close this plan

All against `FakeCounterSource` unless noted.

| Fixture | Covers |
|---|---|
| `PM01_001_empty_category_is_rejected` | Path guard |
| `PM01_001_empty_counter_is_rejected` | Path guard |
| `PM01_001_unavailable_value_is_null` | No fake zero |
| `PM01_002_list_instances_respects_cap` | Cap |
| `PM01_003_interval_below_200ms_rejected` | Floor |
| `PM01_003_burst_below_50ms_rejected` | Burst floor |
| `PM01_003_duration_over_24h_rejected` | Cap |
| `PM01_003_unbounded_job_rejected` | No limit |
| `PM01_003_empty_path_list_rejected` | Guard |
| `PM01_004_count_stops_the_job` | First limit |
| `PM01_004_duration_stops_the_job` | `TimeProvider` |
| `PM01_004_cancel_keeps_samples` | Token |
| `PM01_004_missing_instance_is_partial` | Mid-job miss |
| `PM01_005_prime_tick_is_not_emitted` | Prime |
| `PM01_007_register_is_host_only` | No Initialize in library |
| `PM01_007_writes_noop_until_host_starts` | Logging |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PM01_
```

`PM01_006_live_processor_total` may exist. It skips when the category is missing. It does not close the plan.

---

## 8. EVENTID

Count by 5. Reserved 17000–17499. Used this plan: 17000–17045.

| Id | Name | Severity | When |
|---|---|---|---|
| 17000 | JobEnter | Debug | `RunAsync` entered |
| 17005 | JobStarted | Information | Guards passed, prime done |
| 17010 | JobTick | Debug | One emitted tick (identity + count + status only) |
| 17015 | JobComplete | Information | Natural stop |
| 17020 | JobCancelled | Information | Token |
| 17025 | JobRejected | Error | Guard failed, then throw |
| 17030 | SourceUnavailable | Warning | Category or instance missing |
| 17035 | SourceThrown | Error | Unexpected PDH failure, then throw |
| 17040 | PathRejected | Error | Bad `CounterPath` |
| 17045 | JobFailed | Error | Job aborted after start |

Do not log the sample vector.

`PerfMonLog` follows `AnalyticsLog`: no-op when `!VestigiumLogger.IsInitialized`. APPID field is library identity `PerfMon`. Folder follows the host.

---

## 9. Job rules

| Rule | Lock |
|---|---|
| Default interval | 1 s |
| Fast interval | `< 200 ms` requires `AllowBurst`; floor 50 ms |
| Duration cap | 24 h |
| Unbounded | Rejected in shared |
| First limit | Count, duration, or token |
| Prime | Dropped |
| Result list | Frozen |
| Clock | `SampleJobOptions.Clock` (`TimeProvider`) |
| Thread | No dispatcher hop |
| Machine | `Environment.MachineName` on the record |
| Remote PDH | Not in this package |

Duration fixtures use `TimeProvider`. No `Thread.Sleep(1000)` to prove the clock.

---

## 10. Still parked

| Item | Why |
|---|---|
| Probe packages | Own plans |
| `*` expansion beyond `ListInstances` | Probe names the set |
| Unit catalogs | Probe vocabulary |
| Per-process CPU | Processes / Cpu plan |
| Vendor GPU | Gpu SRS already said no |
| P95 / UCL | Analytics |
| Plot | Charts |
| Linux | Different source |
| Publish 0.1.0 | After a probe consumes this |

---

## 11. Acceptance

PM01.001–005 and 007–009 green. PM01.006 present and silent on a box with no counters.

When this plan closes, move `PM01/` under `000 -- Archived/001 -- Implementation Plan/` and leave a pointer README in the live folder.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Sketch. Archived. |
| 1.1 | 27 Sep 2026 | Live paper. Public surface locked. `Vestigium.Helpers.PerfMon`. |
