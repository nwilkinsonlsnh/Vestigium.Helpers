# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 1.6  
**Status:** Accepted — complete numeric surface + stabilize pass  
**Date:** 19 September 2026  
**Target:** .NET 10 LTS / Visual Studio 2026 / `net10.0`  
**Companion:** `DevelopersGuide_v1.0.md`; `StabilizationPlan_v1.0.md`  
**Project:** `src/Vestigium.Helpers.Analytics/`

## How to read v1.6

v1.5 is the long-form contract (git blob `4dd84d76bebcd7729afdc19d61e7f213204c3405`, commit history before 19 Sep 2026). **v1.6 does not reopen math.** It amends logging, snapshot immutability, control-limit calling, outlier indexes, and time-series order. Where this page is silent, v1.5 §0–§16 still binds.

---

## Amendments (binding)

### A3 — logging

The library never calls `VestigiumLogger.Initialize`. It never chooses a log folder. Hosts that want a disk trace call `VestigiumLogger.Initialize` and `AnalyticsCatalog.Register`. Boundary calls write Debug enter and Information constructed/confidence/limits; rejects write Error then throw. Inner percentile / histogram loops do not log.

Do not treat HelperLog as the Analytics door.

### A5 — frozen snapshot

Values are normalized to `decimal` at construction. Encounter order is preserved. A sorted copy is kept. Published `Values` / `Sorted` / `Times` are frozen copies (`Array.AsReadOnly`). Hosts must not mutate them.

### A9 — MathNet

Inverse CDFs for t and χ² come from MathNet.Numerics. Call them only from internal `QuantileFunctions`. `Confidence.cs` must not name MathNet. Do not hand-roll those functions.

### §4 types added

`ControlLimits`, `ControlLimitMethod`, `ChartPoint`, `ParetoPoint`, `TimedValue`, `AnalyticsCatalog`, `AnalyticsEvents`.

### §8.2 Tukey indexes

`LowOutlierIndexes` / `HighOutlierIndexes` / `OutlierIndexes` are encounter indexes into **this slice’s** `Values`. On `Full` they are series indexes. Parallel to the value lists.

### §10.1 time series

`TimeSeriesPoints()` returns observations with non-null `At`, sorted by `At.UtcTicks` then original encounter index. Empty list when there are no timestamps (do not throw). Encounter order remains `SampleOrderPoints()` plus `Times`.

### §10.2 control limits

```csharp
ControlLimits ControlLimits(method = MeanPlusKSigma, k = 3, floor = null)
bool TryControlLimits(out ControlLimits? limits, method = MeanPlusKSigma, k = 3, floor = null)
ControlLimits.FromCaller(center, upper, lower)
```

Throwing methods stay fail-fast. `TryControlLimits` returns `false` and `limits = null` when the method cannot run (`n < 2`, missing mean, `s` not positive, MR̄ = 0, or MovingRange on a value band). Caller errors still throw from Try (`k <= 0`, `CallerSupplied`).

`ControlLimitMethod.MovingRange` is legal **only on `SliceKind.Full`**. A time-sliced series is Full of that window. Q1–Q4 / IQR throw / Try-false. Mean ± kσ remains legal on any band that has n ≥ 2 and s > 0.

### §11 packaging

| Item | Value |
|---|---|
| Description | Descriptive statistics, quartile bands, confidence intervals, and Shewhart control limits for a numeric series. |
| Tags | `analytics;statistics;quartile;confidence-interval;control-chart;shewhart;vestigium` |
| Logging | `Vestigium.Logging` + `AnalyticsCatalog.Register`. APPID `Analytics`. |

### §16.1 shipped now includes

TryControlLimits, MR-on-Full, Tukey indexes, frozen lists, clock-ordered time series, `QuantileFunctions`, `Vestigium.Logging` catalog. §16.2 items stay parked.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.5 | 8 Sep 2026 | Charts sibling; logging door documented as HelperLog (superseded). |
| 1.6 | 19 Sep 2026 | Stabilize amendments above. |
