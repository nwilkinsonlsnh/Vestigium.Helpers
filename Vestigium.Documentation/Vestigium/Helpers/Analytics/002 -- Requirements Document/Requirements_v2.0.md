# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 2.0  
**Status:** Accepted — single binding contract  
**Date:** 19 September 2026  
**Target:** .NET 10 LTS / `net10.0`  
**Companions:** `Design_v2.0.md`, `DevelopersGuide_v2.0.md`  
**Project:** `src/Vestigium.Helpers.Analytics/`  
**Predecessor:** SRS v1.5–1.6 plus closed stabilize / S5 / PR02–PR04

This page is complete. It does not defer to a prior blob.

---

## 0. Purpose

One in-process snapshot for a finite batch of numbers. Hosts accumulate observations, then construct a `NumericSeries`. The library answers:

- what this batch looks like (descriptors, bands, frequency);
- where the slow tail is (Q4, percentiles, Tukey outliers);
- how uncertain a parameter is (confidence, percentile interval);
- whether the batch sits inside process fences and specification fences;
- whether two batches differ (Welch, optional paired delta).

It does not draw. It does not persist. It does not talk to a host product.

```csharp
var series = NumericSeries.From(rtts, "rtt-ms");
var p95 = series.Full.Percentile(0.95);
var ci = series.Confidence(0.95);
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
var cap = series.Capability(SpecLimits.From(0, 30));
var rules = series.RunRules();
```

Pass `limits`, `rules`, and `SpecLimits` to `Vestigium.Helpers.Charts`. Charts only paints.

---

## 1. Binding language

| Term | Meaning here |
|---|---|
| **P95 / percentile p** | Rank cut on this snapshot. Excel PERCENTILE.INC / Hyndman–Fan type 7. Not a confidence level. |
| **γ / confidence level** | Coverage target in (0, 1). An input. Default 0.95. Never an estimate. |
| **Full** | Encounter-order snapshot. Process methods that need neighbors run here. |
| **Q1–Q4 / IQR** | Value filters from Full fences. Not a process. |
| **Outside control** | Beyond UCL/LCL of a `ControlLimits`. |
| **Outside spec** | Strictly beyond LSL/USL of a `SpecLimits`. On-spec is in spec. |
| **Frozen** | Published lists are `Array.AsReadOnly` copies. Hosts must not mutate them. |

P95 and γ are not interchangeable.

---

## 2. Construction

| Door | Behavior |
|---|---|
| `NumericSeries(IEnumerable<decimal>, name?)` | Encounter order kept. |
| `From<T>(IEnumerable<T>, name?)` where `T : INumber<T>` | Converted to finite `decimal`. |
| `FromDecimal` | Same as the constructor. |
| `FromObservations` | Values + aligned `Times`. Null `At` is a gap. |
| `FromObservations(..., start, end)` | Keep `At` in `[start, end)`. |
| `Slice(start, end)` | New Full of that window. Requires timestamps. |

Rejects:

- empty input → `ArgumentException`;
- non-finite float/double/Half → `ArgumentOutOfRangeException`;
- value or descriptor that cannot live in `decimal` → `ArgumentOutOfRangeException` with `OverflowException` inner;
- inverted or empty time window → `ArgumentException`;
- `Slice` with no timestamps → `InvalidOperationException`.

`Name` on the snapshot is `Trim()` of the caller label (or null). Log property `name` is sanitized (controls stripped, max 64). Those two are not the same string contract.

---

## 3. Snapshot

Published on `NumericSeries`:

`SeriesId`, `Name`, `Count`, `Values`, `Sorted`, `Times`, `HasTimestamps`, `FirstAt`, `LastAt`, `Window`, `Full`, `Q1`, `Q2`, `Q3`, `Q4`, `Iqr`, `Bands`.

`Values` / `Sorted` / `Times` are frozen. Encounter order is `Values` + `Times`. `Sorted` is ascending. `TimeSeriesPoints()` returns non-null `At` sorted by `At.UtcTicks` then encounter index. Empty list when there is no time — do not throw.

Six slices use Full fences:

| Slice | Membership |
|---|---|
| Full | Every value |
| Q1 | x ≤ Full.Q1 |
| Q2 | Full.Q1 < x ≤ Full.Median |
| Q3 | Full.Median < x ≤ Full.Q3 |
| Q4 | x > Full.Q3 |
| IQR | Full.Q1 ≤ x ≤ Full.Q3 |

Empty bands exist (`IsEmpty`, count 0). Property reads return null descriptors. Rank methods on an empty band throw one `InvalidOperationException`: `Cannot compute a percentile of an empty slice.`

---

## 4. Descriptors on a slice

When the band is non-empty:

Min, Q1, Median, Q3, Max, Range, IQR, Midrange, Midhinge, Trimean, Tukey fences, Tukey outlier values and **encounter indexes into this slice’s `Values`**, Sum, Mean, SSD, sample and population variance / s, SEM, CV, MAD, median AD, Excel SKEW (n ≥ 3), Excel KURT (n ≥ 4), geometric and harmonic means when every value is positive, `Frequency`.

Moments that need a divisor stay `double`. Rank cuts stay `decimal`.

`Percentile(p)` for p in [0, 1]. p outside that range → `ArgumentOutOfRangeException`.

`NamedPercentiles()` is P01, P05, P10, Q1, median, Q3, P90, P95, P99.

`PercentileRank(x)` is count(v ≤ x) / n on this band. Same empty throw as `Percentile`.

---

## 5. Frequency

`FrequencyTable` on each slice:

- exact multiplicities (`Frequencies`, `Modes`, unique `Mode` when the top count is ≥ 2 and unique);
- Shannon entropy in nats;
- Freedman–Diaconis histogram on the **value** axis. Last bin closed so Max is included. Not a time histogram;
- `HistogramTrend()` OLS through (midpoint, count);
- `Pareto()` bins by count descending with running share of n.

Do not bin the value histogram by clock time.

---

## 6. Confidence

`Confidence(level = 0.95)` and `Confidence(level, populationSize)` on series and slice.

| Interval | Method | Undefined when |
|---|---|---|
| Mean | Student t; optional FPC when N is known | n < 2 or s missing |
| Median | Order-statistic normal approximation | n = 0 |
| Variance | χ² on SSD | n < 2 |
| StdDev | sqrt of variance interval | variance undefined |

γ not in (0, 1) → `ArgumentOutOfRangeException`. N < 1 or n > N → `ArgumentOutOfRangeException`. Undefined intervals set `IsDefined = false` and do not throw.

Also required:

- `MeanPValue(hypothesizedMean)` two-sided t; null when undefined;
- `MeanConfidenceLevelContaining` = 1 − p; not “sample confidence”;
- `SampleSizeForMeanMargin(targetMargin, level)` planned n; null when s ≤ 0; margin ≤ 0 throws;
- `ProportionAbove` / `ProportionAtLeast` Wilson score.

Inverse CDF for t, χ², Normal, and Binomial CDF live only in internal `QuantileFunctions` (MathNet). `Confidence.cs` must not name MathNet. Do not hand-roll those functions.

---

## 7. Percentile interval

`series.PercentileInterval(p, level)` / `slice.PercentileInterval(p, level)`.

Two-sided interval for the sample percentile from order statistics. Coverage of ranks (j, k) is the binomial sum Σ C(n,i) p^i (1−p)^{n−i} from i = j to k (1-based).

Pick the tightest (j, k) whose coverage is at least γ (fewest ranks, then narrowest width, then smaller j). If no pair reaches γ, publish the sample range and `ReachedCoverage = false`, `Method = SampleRange`. Otherwise `Method = OrderStatistic`.

Not a mean interval. Not an SLA. Empty band uses the same empty-percentile throw.

---

## 8. Kernel density

`series.PdfPoints()` / `KernelDensity.PdfPoints(slice)`.

Gaussian kernel, Silverman bandwidth h = 1.06 s n^{-⅕}. Default 64 evaluation points on a padded range. Returns an empty list when s is not positive (constant series). Points are density, not counts. Charts scales by n × bin width when overlaying a histogram.

---

## 9. Control limits

```csharp
ControlLimits ControlLimits(method = MeanPlusKSigma, k = 3, floor = null)
bool TryControlLimits(out ControlLimits? limits, ...)
ControlLimits.FromCaller(center, upper, lower)
ControlLimits Against(IEnumerable<decimal>? values)
```

| Method | Rule |
|---|---|
| MeanPlusKSigma | µ ± k s. Legal on any band with n ≥ 2 and s > 0. |
| MovingRange | CL = µ, UCL/LCL = µ ± 3 MR̄ / d2, d2(2) = 1.128. **Full only.** |
| CallerSupplied | `FromCaller` only. Computing doors reject this method. |

Throwing doors fail fast when the method cannot run. `Try*` returns false and `limits = null` for those cases (`n < 2`, missing mean, s not positive, MR̄ = 0, MovingRange on a value band). Caller errors still throw from Try (`k ≤ 0`, `CallerSupplied` on Compute).

`Against` scores values against existing fences. Fences do not change. Null or empty input → count 0, no throw. `OutOfControlIndexes` and `MovingRanges` are frozen.

Optional `floor` raises LCL when the computed lower fence is below it (latency ≥ 0).

Log properties that list out-of-control indexes join at most 32 indexes; past that they write `n=N (truncated)`.

---

## 10. Specification and capability

`SpecLimits.From(lower?, upper?)` does not score. At least one side required. Lower < Upper when both present. Non-finite → `ArgumentOutOfRangeException`.

`Against(values)` flags **strict** outside. 0 and USL are in spec. Indexes frozen. Null/empty → count 0.

`series.Capability(spec)` / `slice.Capability(spec)`:

| Index | Sigma |
|---|---|
| Pp, Ppl, Ppu, Ppk | sample s |
| Cp, Cpl, Cpu, Cpk | σ̂_w = MR̄ / d2 on **Full** only |

Missing side or non-positive sigma → that index is null, not a throw. Constant series: Pp/Ppk/Cp/Cpk null; Mean still published. The report carries the scored `Spec`.

---

## 11. Run rules

`series.RunRules(method = MeanPlusKSigma)` / `slice.RunRules(...)` on **Full only**. Value bands throw `InvalidOperationException` (`RunRuleReport.RequiresFull`). `CallerSupplied` rejected. Constant / insufficient series refuse like `ControlLimits`.

Western Electric 1–4 and Nelson 5–8. Hits publish frozen index lists. `AllIndexes` is the union, frozen.

| Rule | Meaning |
|---|---|
| 1 PointBeyondThreeSigma | One point beyond ±3σ |
| 2 TwoOfThreeBeyondTwoSigma | 2 of 3 beyond ±2σ, same side |
| 3 FourOfFiveBeyondOneSigma | 4 of 5 beyond ±1σ, same side |
| 4 EightOnOneSideOfCenter | 8 in a row on one side of CL |
| 5 SixRisingOrFalling | 6 strict monotone (ties break the run) |
| 6 FifteenInZoneC | 15 in a row inside ±1σ |
| 7 FourteenAlternating | 14 strict alternating steps |
| 8 EightOutsideZoneC | 8 in a row outside ±1σ |

A lone spike on small n often sits inside 3σ because s inflates. That is correct. Use more quiet points, MovingRange, or `FromCaller` — do not invent fences in Charts.

---

## 12. Two series

`a.Compare(b)`:

- Welch two-sample t always attempted (n ≥ 2 and s defined on both);
- `Paired` is a new `NumericSeries` of encounter-order differences when counts match; otherwise null;
- no host names on the type.

---

## 13. Chart-facing points

`ChartPoint`, `ParetoPoint`, `TimedValue` are numbers only.

Required views: `EcdfPoints`, `SampleOrderPoints`, `SortedPoints`, `HistogramRelativePoints`, `ParetoPoints`, `TimeSeriesPoints`, `PdfPoints`.

---

## 14. Logging

The library never calls `VestigiumLogger.Initialize` and never chooses a log folder. Hosts that want a disk trace call `Initialize` and `AnalyticsCatalog.Register`.

- APPID row is library identity `Analytics`. Folder follows the host process APPID.
- EventIds 10500–10615 (block reserved through 10999).
- Constants on `AnalyticsEvents`. Shard `EventCatalog/analytics.json`.
- Writes no-op when Logging is not started; math still runs.
- Boundary: Debug enter, Information constructed / computed, Error then throw on reject.
- Inner percentile / histogram / KDE loops do not log.
- Do not treat `HelperLog` as the Analytics door.
- Stable MESSAGE; varying values in PROPERTIES. Property `name` is sanitized.

---

## 15. Packaging

| Item | Value |
|---|---|
| TFM | `net10.0` |
| Description | Descriptive statistics, quartile bands, confidence intervals, and Shewhart control limits for a numeric series. |
| Tags | `analytics;statistics;quartile;confidence-interval;control-chart;shewhart;vestigium` |
| MathNet.Numerics | 5.x, transitive to package consumers |
| InternalsVisibleTo | `Vestigium.Helpers.Tests` |

No charting NuGet. No ClosedXML. No host project reference.

---

## 16. Tests

Required nets: `NumericSeriesTests`, `ControlLimitsTests`, `AnalyticsLoggingTests`, `AnalyticsCoverageTests`, `AnalyticsPR02Tests`, `AnalyticsPR03Tests`, `AnalyticsPR03CompareTests`, `AnalyticsPR04Tests`, `AnalyticsPR04NelsonTests`, `AnalyticsPR05Tests` / `AnalyticsPR05CloseTests` where they pin Analytics doors.

---

## 17. Non-goals

Computing UCL in Charts. Drawing. Streaming sketches. Time-bucket histograms. Bayesian. OTel. Bootstrap percentile CI. Other KDE kernels. EWMA / CUSUM. Host or PingIQ adapters. Public MathNet types. Mutating snapshot lists.

---

## 18. Document control

| Version | Date | Change |
|---|---|---|
| 1.5 | 8 Sep 2026 | Numeric surface + Charts sibling. |
| 1.6 | 19 Sep 2026 | Stabilize amendments (Try, MR-on-Full, freeze, logging). |
| 2.0 | 19 Sep 2026 | Lossless single contract: v1.6 + S5 + PR02–PR04. |
