# Vestigium.PerfMon — PM01 implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PM01
**Version:** 1.0
**Status:** Open
**Date:** 27 September 2026
**Package:** `Vestigium.PerfMon` 0.1.0 (not published)
**Binding:** [`Requirements_v1.0.md`](../002%20--%20Requirements%20Document/Requirements_v1.0.md) wins on conflict.
**Companion:** [`Design_v1.0.md`](../003%20--%20Design%20Document/Design_v1.0.md)

Shared library only. One record. One source port. One clock. One logging door.

This version is not a probe. Cpu / Disk / Gpu / Memory / Network / PageFile do not land here.

---

## 1. Goal

A host or a later probe can:

1. Name a counter path.
2. Run a bounded job against `ICounterSource`.
3. Get frozen `SampleRecord` rows with UTC, status, and a numeric value or an explicit miss.
4. Register events inside `VestigiumLogger.Initialize`.

Done when the fixtures in §6 pass against a fake source and the project still builds with no probe reference.

## 2. What PM01 is not

- Any `Vestigium.PerfMon.*` façade.
- Live PDH as a CI gate.
- Analytics reduction or Charts.
- `IObservable`, dispatcher marshal, Data Collector Sets.
- Remote computer `\\machine\`.
- Publishing NuGet.

If a step needs one of those, it is the wrong step.

---

## 3. Shape

```
caller
  → SampleJob
       → ICounterSource.Read(CounterPath)
            ├ FakeCounterSource          tests
            └ PerformanceCounterSource   Windows, internal
       → SampleRecord[]   frozen
PerfMonCatalog.Register is host-only
```

`SampleJob` owns interval, duration, count, token, burst rules, and the prime tick.
`ICounterSource` owns one read.
Probes will own which paths to ask. Not in this plan.

---

## 4. Files

Keep them small. No hub file.

| File | Role |
|---|---|
| `SampleStatus.cs` | `Ok`, `Unavailable`, `Partial`, `Cancelled`, `Rejected` |
| `SampleRecord.cs` | Immutable record: Utc, Machine, Category, Counter, Instance, Value, Unit, Status |
| `CounterPath.cs` | Category + Counter + Instance. Empty category or counter → reject + throw |
| `SampleJobOptions.cs` | Interval, Duration, Count, AllowBurst, InstanceCap |
| `SampleJobResult.cs` | Terminal status + frozen records |
| `ICounterSource.cs` | `Read`, `ListInstances` |
| `FakeCounterSource.cs` | Internal. Tests drive values and misses |
| `PerformanceCounterSource.cs` | Internal. PDH / `PerformanceCounter` |
| `SampleJob.cs` | Async loop |
| `PerfMonEvents.cs` | EVENTID constants, count by 5 |
| `PerfMonLog.cs` | No-op until host initializes |
| `PerfMonCatalog.cs` | Replace skeleton `Register(object)` with `Register(VestigiumLoggerOptions)` |
| `EventCatalog/perfmon.json` | Shard packed with the nupkg later |

Tests: `src/Vestigium.Helpers.Tests/PerfMonPM01Tests.cs` plus a project reference to `Vestigium.PerfMon`.

Package: `System.Diagnostics.PerformanceCounter` 10.0.12 (same as Processes). Shared may reference it. Tests must not require a live category.

---

## 5. Steps

| Step | Priority | Work | Exit |
|---|---|---|---|
| **PM01.001** | P0 | `SampleStatus`, `SampleRecord`, `CounterPath`. Records frozen. `Value` is `double?`. `Unavailable` has null value, never `0` as a stand-in. | Path with empty category throws. Record with `Unavailable` + `0` is a failed fixture. |
| **PM01.002** | P0 | `ICounterSource` + `FakeCounterSource`. `ListInstances` honors `InstanceCap` default 256. | Fake returns programmed rows. Cap truncates and does not throw. |
| **PM01.003** | P0 | `SampleJobOptions` + guards. Default interval 1 s. Interval `< 200 ms` rejected unless `AllowBurst`. Burst floor 50 ms. Duration `> 24 h` rejected. Count `< 0` rejected. `Count == 0` and no duration and no token → reject (unbounded loop is a host campaign). | Each reject logs Failed then throws. |
| **PM01.004** | P0 | `SampleJob.RunAsync`. First limit wins (count, duration, token). Cancel → terminal `Cancelled`, keep samples already taken. Missing instance mid-job → `Partial` on that tick, job continues. | Fixtures in §6. |
| **PM01.005** | P0 | Prime tick. Rate counters need two PDH reads. Job reads once before the first emitted tick and drops that sample. Fake source can mark a path as `NeedsPrime`. | First emitted record is not the prime. |
| **PM01.006** | P1 | `PerformanceCounterSource`. Missing category or instance → `Unavailable`, no hang. Unexpected throw from PDH → log Error, then throw. Do not invent zero. No remote `\\server`. | Manual on a Windows box. Not a CI gate. |
| **PM01.007** | P1 | Logging door. `Register(VestigiumLoggerOptions)`. APPID stamp `PerfMon`. Folder follows the host. Tick events are Debug. Start / complete are Information. Reject is Error. Unavailable is Warning. | Writes no-op without Initialize. Tests use a temp `LogDirectory`. |
| **PM01.008** | P1 | EVENTID 17000–17045 used. Block reserved through 17499. See §7. | Constants + `perfmon.json` agree. |
| **PM01.009** | P2 | Wire `Vestigium.Helpers.Tests` project reference. Filter `FullyQualifiedName~PM01_`. | `dotnet test --filter FullyQualifiedName~PM01_` green without live counters. |

Do not start PM01.006 until 001–005 are green. Live PDH is not the proof of the clock.

---

## 6. Tests that close this plan

All against `FakeCounterSource` unless noted.

| Fixture | Covers |
|---|---|
| `PM01_001_empty_category_is_rejected` | Guard |
| `PM01_001_unavailable_value_is_null` | No fake zero |
| `PM01_002_list_instances_respects_cap` | Cap 256 |
| `PM01_003_interval_below_200ms_rejected` | Floor |
| `PM01_003_burst_below_50ms_rejected` | Burst cap |
| `PM01_003_duration_over_24h_rejected` | Cap |
| `PM01_003_unbounded_job_rejected` | Count 0 + no duration + no token |
| `PM01_004_count_stops_the_job` | First limit |
| `PM01_004_duration_stops_the_job` | First limit |
| `PM01_004_cancel_keeps_samples` | Cancel |
| `PM01_004_missing_instance_is_partial` | Mid-job miss |
| `PM01_005_prime_tick_is_not_emitted` | Prime |
| `PM01_007_register_is_host_only` | No Initialize inside the library |
| `PM01_007_writes_noop_until_host_starts` | Logging |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PM01_
```

A live PDH smoke test may exist as `PM01_006_live_processor_total` but it must skip or return when `Processor` / `Processor Information` is missing. It does not close the plan.

---

## 7. EVENTID (used this plan)

Count by 5. Block 17000–17499.

| Id | Name | Severity | When |
|---|---|---|---|
| 17000 | JobEnter | Debug | `RunAsync` entered |
| 17005 | JobStarted | Information | Guards passed, prime done |
| 17010 | JobTick | Debug | One emitted tick |
| 17015 | JobComplete | Information | Natural stop |
| 17020 | JobCancelled | Information | Token |
| 17025 | JobRejected | Error | Guard failed, then throw |
| 17030 | SourceUnavailable | Warning | Category or instance missing |
| 17035 | SourceThrown | Error | Unexpected PDH failure, then throw |
| 17040 | PathRejected | Error | Bad `CounterPath` |
| 17045 | JobFailed | Error | Job aborted after start |

Do not log the full sample vector on every tick. Identity + count + status only.

---

## 8. Job rules (so Alvin does not renegotiate mid-file)

| Rule | Lock |
|---|---|
| Default interval | 1 s |
| Fast interval | `< 200 ms` needs `AllowBurst`; floor 50 ms |
| Duration cap | 24 h |
| Unbounded | Forbidden in shared. Host campaign later. |
| First limit | Count, duration, or token — whichever hits first |
| Prime | Dropped. Never in the result list |
| Frozen result | Caller cannot mutate the list |
| Clock | `TimeProvider` injectable for tests. Default `TimeProvider.System` |
| Thread | `RunAsync` does not hop to a WPF dispatcher |

`TimeProvider` is allowed. It is not a new product. It is how duration fixtures stay off `Thread.Sleep(1000)`.

---

## 9. Still parked

| Item | Why |
|---|---|
| Probe packages | Next plans, one each |
| `*` expansion policy beyond `ListInstances` | Probe names the set |
| Per-process CPU | Processes / later Cpu plan |
| Vendor GPU | Gpu plan, and SRS already said no |
| P95 / UCL | Analytics |
| Plot | Charts |
| Linux | Different source |
| NuGet 0.1.0 publish | After a probe exists and a host can consume it |

---

## 10. Acceptance

PM01.001–005 and 007–009 green. PM01.006 exists and does not fail CI when the agent has no counters.

When this plan closes, move it under `000 -- Archived/001 -- Implementation Plan/PM01/` and leave a pointer README in the live folder.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 27 Sep 2026 | Open. Shared clock and source only. |
