# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 1.2  
**Status:** Accepted — complete numeric surface (implementation follows this document)  
**Date:** 7 September 2026  
**Target:** .NET 10 LTS / Visual Studio 2026 / `net10.0`  
**Companion:** `DevelopersGuide_v1.0.md`  
**Project:** `src/Vestigium.Helpers.Analytics/`

---

## 0. How to read this document

This is the lossless contract for `Vestigium.Helpers.Analytics`. It replaces the v1.1 draft and the v1.0 skeleton. If implementation and this file disagree, this file wins.

It records every decision from the Analytics design conversation:

- instance `NumericSeries` over any numeric feed
- six slices (Full, Q1–Q4, IQR) each with the same descriptor set
- five-number summary, range family, frequency, moments, skewness, kurtosis
- confidence **level** vs confidence **interval** vs **percentile** (P90 / P95 / P99)
- right-tail reading of latency
- optional per-observation `DateTimeOffset` and series window
- host feed pattern (collect → window → describe)
- charting stays **out** of this library
- the small Analytics additions required so later charting / PingIQ views have numbers, not pictures

---

## 1. Purpose

Give Vestigium hosts (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost, WPF galleries, later services) one in-process type that turns a finite batch of numbers into **meaningful descriptors**.

Typical feeds: echo RTTs, hop counts, payload sizes, probe durations, HTTP status-to-ms mappings, any other homogeneous numeric column.

The library answers questions of the form:

- What does this batch look like? (five-number, range, frequency, shape)
- Where is the slow tail? (P90 / P95 / P99, Q4, high outliers, max)
- How uncertain is the average? (confidence interval at a caller-chosen level)
- Do I have enough samples to decide against an SLA? (interval width, planned n)

It does not answer “draw this.” It does not persist. It does not log.

---

## 2. Architectural constraints (binding)

| ID | Constraint |
|---|---|
| A1 | Class library `net10.0`. No WPF, no Themes, no Controls, no charting NuGet. |
| A2 | Independently referenced. Core `Vestigium.Helpers` may be used for guards only. |
| A3 | The library never calls `VestigiumLogger.Initialize`. It never writes `%ProgramData%` logs. Hosts that want a disk trace call `Vestigium.Logging` themselves. |
| A4 | Public work lives on an instance (`NumericSeries` / `SeriesSlice`), not a static bag of math functions. `AnalyticsHelper` is a façade for `Identity` and factories. |
| A5 | Values are normalized to `decimal` at construction. Encounter order is preserved. A sorted copy is kept. |
| A6 | A series is a **snapshot**, not a stream. Rolling windows, incremental sketches, and “append one ping” are host concerns. The host accumulates, then constructs a new series. |
| A7 | Time is optional metadata for windowing and later line charts. Time is **not** an axis of the value histogram. |
| A8 | Charting is out of this project. A future `Vestigium.Helpers.Charts` (or a host view) may consume the numbers this library publishes. This library may publish chart-*ready sequences of numbers*. It must not reference OxyPlot, ScottPlot, LiveCharts, or any drawing surface. |
| A9 | Inverse CDFs for t and χ² come from MathNet.Numerics (`StudentT.InvCDF`, `ChiSquared.InvCDF`). Do not hand-roll those two functions. |
| A10 | Undefined statistics are `null`, never `NaN`, never magic sentinels (`-1`, `decimal.MinValue`). |
| A11 | Empty input throws. Null input throws. Non-finite input (`NaN`, `±∞`) throws. |
| A12 | Windows-only APIs are forbidden here. `WinReg` stays in its own project. |

---

## 3. Glossary (binding language)

These words are not interchangeable. Host UI copy and XML docs must use them as defined here.

| Term | Meaning |
|---|---|
| **Population** | The unknown complete set of values the sample stands in for (every future RTT on that path, every host in a scan, …). Vestigium almost never holds the population. |
| **Sample / series / batch / snapshot** | The finite values handed to `NumericSeries`. |
| **n** | `Count` of the current series or slice. |
| **N** | Finite population size, if and only if the caller knows it. Optional input to a confidence overload. Not inferred. |
| **Observation** | One value plus optional timestamp. |
| **Value axis** | The measured quantity (ms, hops, bytes). Histogram, percentiles, and most descriptors live here. |
| **Time axis** | When the observation was taken. Used to *select* a batch or to plot a line. Not used to bin an RTT histogram. |
| **Percentile / Px / P90 / P95 / P99** | A cut on the **sorted sample**. P95 is the value at rank 95 %. “P” means **percentile**, not probability-as-confidence and not “population fraction sampled.” |
| **Right tail** | The high side of the value axis. For latency, the slow probes. Read with P90 → P95 → P99 → Max, Q4, and high Tukey outliers. |
| **Left tail** | The low side. Rarely the operational story for RTT. |
| **Confidence level γ** | A **chosen** coverage target in `(0, 1)`, commonly 0.90 / 0.95 / 0.99. Equal to `1 − α`. **Not calculated from the sample.** |
| **Significance α** | `1 − γ`. |
| **Confidence interval** | A range computed from *this* sample by a procedure that would cover the unknown parameter in about `γ` of repeated samples. After the data exist, the true parameter is either inside or not (0 or 1). The `γ` belongs to the procedure. |
| **Just-covering level** | For a hypothesized mean `μ₀`, the smallest `γ` whose two-sided mean interval still contains `μ₀`. Equals `1 − p` of the two-sided t-test. **Not** “how confident the sample is.” |
| **Margin of error** | Half-width of the mean interval at `γ`. |
| **ECDF** | Empirical cumulative distribution: for each sorted value, the fraction of the sample at or below it. The usual “curve” for latency. Published as numbers, not as a chart. |
| **Value histogram** | Bins on the measured quantity. Freedman–Diaconis on the current snapshot. |
| **Time histogram** | Bins on the clock (“probes per minute”). A different object. Out of scope for this library. |

### 3.1 Explicit non-meanings

The following sentences are **false** and must not appear in API names, XML docs, or host copy as if they were true:

- “I measured 95 % of the population, so I have 95 % confidence.”
- “P95 means 95 % confidence.”
- “The confidence interval is the right tail.”
- “You need the entire population to compute a confidence interval.”
- “This sample is 97 % confident.”
- “P90 is a bucket; P89 and below are a different set of leftover points.”

P90, P95, P99 are different cut-marks on **one** sorted list.

---

## 4. Public types

| Type | Role |
|---|---|
| `AnalyticsHelper` | Stable façade. `Identity == "Vestigium.Helpers.Analytics"`. Forwards `From` / `FromDecimal` / `FromObservations`. |
| `NumericSeries` | Immutable snapshot. Owns values, optional times, optional window, six slices, series-level confidence helpers. |
| `SeriesSlice` | One band (Full / Q1 / Q2 / Q3 / Q4 / Iqr) plus the full descriptor set. |
| `SliceKind` | Enum of those six bands. |
| `Observation` | `decimal Value` + `DateTimeOffset? At`. Time optional. |
| `SeriesWindow` | Inclusive-start / exclusive-end window in UTC, plus a flag for whether it was caller-supplied or inferred from timestamps. |
| `FrequencyTable` | Exact multiplicities, modes, entropy, value histogram. |
| `FrequencyBin` | `(Value, Count, RelativeFrequency)` |
| `HistogramBin` | `(LowerInclusive, UpperInclusive, UpperIsClosed, Count, RelativeFrequency)` |
| `ConfidenceLevel` | Validated `γ ∈ (0, 1)`. Default `0.95`. `Alpha => 1 - Value`. |
| `ConfidenceInterval` | One parameter fence: `Parameter`, `Estimate`, `Lower`, `Upper`, `Level`, `Method`, `IsDefined`, `Width`. |
| `ConfidenceReport` | Mean, median, variance, stddev intervals at one level. |
| `ChartPoints` (numeric only) | Sequences of `(X, Y)` doubles derived from the series (ECDF, histogram midpoints, sample order). No drawing types. |

No chart control, no `WriteableBitmap`, no color, no WPF `Polyline`.

---

## 5. Construction and validation

### 5.1 Factories (all required)

```text
NumericSeries.From<T>(IEnumerable<T> values, string? name = null)
    where T : INumber<T>

NumericSeries.FromDecimal(IEnumerable<decimal> values, string? name = null)

NumericSeries.FromObservations(IEnumerable<Observation> observations, string? name = null)

new NumericSeries(IEnumerable<decimal> values, string? name = null)

AnalyticsHelper.From(...) / FromDecimal(...) / FromObservations(...)
    — same as the NumericSeries factories
```

`T` includes at least: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `Half`. Conversion is lossless where the decimal range allows; overflow throws.

### 5.2 Rejection rules

| Input | Exception |
|---|---|
| `null` sequence | `ArgumentNullException` |
| zero observations after conversion | `ArgumentException` (“Series requires at least one finite value.”) |
| `double.NaN`, `±∞`, same for `float` / `Half` | `ArgumentOutOfRangeException` |
| `name` blank | stored as `null` (do not throw) |
| `Observation` with non-finite value | same as above |
| `Slice(from, to)` when **no** observation has `At` | `InvalidOperationException` |
| `Slice` with `from >= to` | `ArgumentException` |
| `ConfidenceLevel` / `Confidence(γ)` with `γ ≤ 0` or `γ ≥ 1` | `ArgumentOutOfRangeException` |
| `Percentile(p)` with `p` outside `[0, 1]` | `ArgumentOutOfRangeException` |
| `Percentile` / `NamedPercentiles` on an empty slice | `InvalidOperationException` |

A constructed `NumericSeries` itself is never empty. A **slice** of it may be empty (degenerate series where all values are equal: Q2–Q4 empty).

### 5.3 Storage

| Member | Rule |
|---|---|
| `Name` | Optional trimmed label (`"rtt-ms"`, `"hops"`). |
| `Values` | `IReadOnlyList<decimal>`, encounter order. |
| `Sorted` | `IReadOnlyList<decimal>`, non-descending. |
| `Times` | `IReadOnlyList<DateTimeOffset?>`, same length as `Values`, or empty list when the caller used a values-only factory. Empty list means “no timestamps,” not “all null times.” |
| `HasTimestamps` | `true` when at least one `Times[i]` is non-null. |
| `FirstAt` / `LastAt` | Min / max of the non-null timestamps; null if none. |
| `Window` | See §6. |
| `Count` | `Values.Count`. |

Values-only construction must keep working. Requiring a timestamp on every point is forbidden.

---

## 6. Time model

### 6.1 Decision

Timestamps are **optional**. They do not change Min, P95, skewness, histogram bins, or confidence intervals. Those are value-axis statistics.

Time exists so a host can:

1. Record when each probe ran (`Observation.At`).
2. Label the snapshot (`SeriesWindow`).
3. Cut a new snapshot (`Slice(from, to)`).
4. Later plot a `(time, value)` line in a **different** library or view.

### 6.2 `Observation`

```text
readonly record struct Observation(
    decimal Value,
    DateTimeOffset? At = null)
```

- `At` should be UTC. If a caller passes a non-UTC offset, store it as given; do not silently convert on read, but `Slice` compares `At.UtcDateTime` (or `At.UtcTicks`) so windows are well-defined.
- Missing `At` is allowed even inside an observation list. Those points participate in every value statistic and are **excluded** from time slices (they have no time to test).
- Do not invent timestamps.

### 6.3 `SeriesWindow`

```text
readonly record struct SeriesWindow(
    DateTimeOffset? StartInclusive,
    DateTimeOffset? EndExclusive,
    SeriesWindowKind Kind)

enum SeriesWindowKind { None, InferredFromTimestamps, CallerSupplied }
```

| Kind | When |
|---|---|
| `None` | Values-only series, or observations with no usable times. |
| `InferredFromTimestamps` | `HasTimestamps` and the caller did not pass a window. `StartInclusive = FirstAt`, `EndExclusive = LastAt + ε` or simply publish First/Last and Kind = Inferred. Implementation: expose FirstAt/LastAt; `Window.Kind = InferredFromTimestamps`; Start = FirstAt; End = LastAt (inclusive description is acceptable if documented — see below). |
| `CallerSupplied` | Host passed an explicit window at construction or the series is the result of `Slice`. |

**Window comparison rule for `Slice`:** keep observation `i` when `At` is non-null and `StartInclusive <= At < EndExclusive`.

Document that convention in XML docs. Do not mix inclusive-end on one API and exclusive-end on another.

Optional factory overload, accepted:

```text
NumericSeries.FromObservations(
    IEnumerable<Observation> observations,
    DateTimeOffset startInclusive,
    DateTimeOffset endExclusive,
    string? name = null)
```

This filters first, then constructs. Result `Window.Kind = CallerSupplied`. If the filter leaves zero points, throw `ArgumentException` (same as empty series).

### 6.4 `Slice`

```text
NumericSeries Slice(DateTimeOffset startInclusive, DateTimeOffset endExclusive, string? name = null)
```

- Requires `HasTimestamps`; otherwise `InvalidOperationException`.
- Applies the window rule above.
- Preserves relative encounter order.
- Recomputes every descriptor from scratch on the surviving values. Slices of slices of *values* (Q1–Q4) are always rebuilt from the new full sample.
- Does not mutate the source series.

### 6.5 What time is not

- Not an extra dimension on `Frequency.Histogram`. Those bins are milliseconds (or hops, or bytes), never minutes-of-day.
- Not a substitute for `n`. A two-hour window with 8 probes is still `n = 8`.
- Not required for P95, Q4, or a 95 % mean interval.
- Not a time-bucket histogram. “Probes per minute” is out of scope here.

### 6.6 Host feed pattern (normative guidance)

```text
measure → store Observation { At = utcNow, Value = rttMs }
       → pick a window (last 5 min, this run, since the route change)
       → NumericSeries.FromObservations(windowed)   // or From(values) if clocks were never kept
       → read descriptors / ECDF points / histogram bins
       → (optional) host draws those numbers
```

Analytics never owns the accumulating buffer.

---

## 7. Slices (value bands)

Every series exposes six `SeriesSlice` instances. Membership uses fences from the **full** series, then each band computes its **own** descriptors from its own values.

| Kind | Membership |
|---|---|
| `Full` | every value, original multiset |
| `Q1` | `x <= Q1` of the full series |
| `Q2` | `Q1 < x <= Median` of the full series |
| `Q3` | `Median < x <= Q3` of the full series |
| `Q4` | `x > Q3` of the full series |
| `Iqr` | `Q1 <= x <= Q3` of the full series |

Ties sit on the lower fence of the next band except `Q1` and `Iqr`, which are closed on the published fences.

Empty bands: `Count = 0`, moments / quantiles `null`, `Frequency` empty, `Confidence(...).*.IsDefined = false`. Do not throw when *reading* properties of an empty band.

`NumericSeries.Bands` returns `[Full, Q1, Q2, Q3, Q4, Iqr]` in that order.

### 7.1 Quartile algorithm (binding)

Excel `PERCENTILE.INC` / Hyndman–Fan type 7 on the sorted **full** series:

```text
position = 1 + p * (n - 1)     // 1-based
```

Linear interpolate between adjacent order statistics.

| Quartile | p |
|---|---|
| Q1 | 0.25 |
| Median | 0.50 |
| Q3 | 0.75 |

Worked acceptance values:

| Sample | Min | Q1 | Median | Q3 | Max |
|---|---|---|---|---|---|
| `{1,2,3,4,5,6,7,8,9}` | 1 | 3 | 5 | 7 | 9 |
| `{1,2,3,4}` | 1 | 1.75 | 2.5 | 3.25 | 4 |

Descriptors **inside** a band use that band’s values and the same percentile rule. A band does not inherit parent fences.

### 7.2 Right tail (operational reading)

For response times the slow probes are the **right tail** of the value axis.

| Read this | To see |
|---|---|
| P90 | where the slowest 10 % begin |
| P95 | where the slowest 5 % begin (common SLA cut) |
| P99 / P99.9 | rare-but-ugly; unstable until n is large |
| Max | worst point in *this* snapshot (one fluke owns it) |
| `Q4` | the upper-quartile **group**, with its own range / frequency / skew / kurtosis |
| `HighOutliers` | Tukey right-tail flags (`x > Q3 + 1.5 IQR`) |
| `Skewness > 0` | mass on the left, tail on the right (typical RTT) |

Normative host reading:

- P95 stable, Max jumping → one-off; do not declare the path dead.
- P95 and P99 both climbing → the tail itself moved.
- Mean up, P50 flat, P95 up → the average is being dragged by the right tail.

Q4 is how a caller describes “just the slow group” without inventing a second series type.

---

## 8. Required descriptors (every non-empty slice)

All of the following are in scope. They are the “get all of the numbers” surface.

### 8.1 Five-number summary

`Min`, `Q1`, `Median`, `Q3`, `Max`.

### 8.2 Range family and fences

| Name | Definition |
|---|---|
| `Range` | `Max − Min` |
| `Iqr` | `Q3 − Q1` |
| `Midrange` | `(Min + Max) / 2` |
| `Midhinge` | `(Q1 + Q3) / 2` |
| `Trimean` | `(Q1 + 2 × Median + Q3) / 4` |
| `TukeyLowerFence` | `Q1 − 1.5 × Iqr` |
| `TukeyUpperFence` | `Q3 + 1.5 × Iqr` |
| `LowOutliers` | values `< TukeyLowerFence`, encounter order |
| `HighOutliers` | values `> TukeyUpperFence`, encounter order |
| `Outliers` | `LowOutliers` then `HighOutliers` is **not** required; publish `Outliers` as the concatenation in **encounter order** of every value outside either fence |

### 8.3 Frequency (exact) and value histogram

Exact multiplicity on stored `decimal` values. No implicit rounding.

| Name | Definition |
|---|---|
| `DistinctCount` | unique values |
| `Frequencies` | `(Value, Count, RelativeFrequency)` ordered by count desc, then value asc |
| `Modes` | every value whose count equals the maximum count |
| `Mode` | that value when `HasUniqueMode`, else null |
| `HasUniqueMode` | max count ≥ 2 **and** exactly one such value. If every value appears once, there is no unique mode. |
| `EntropyNats` | `−Σ pᵢ ln pᵢ` over relative frequencies |

**Value histogram** (`Frequency.Histogram`), Freedman–Diaconis:

```text
width = 2 × IQR × n^(−1/3)
```

| Degenerate case | Behaviour |
|---|---|
| `IQR = 0` or `Min = Max` | one bin spanning `[Min, Max]`, count = n |
| width rounds to 0 | treat as a single-span bin |
| last bin | closed on the upper edge so `Max` is included |

Histogram X is the **value** axis. Do not bin by timestamp here.

### 8.4 Moments and shape

Sample moments unless the name says `Population`.

| Name | Rule | Minimum n / extra |
|---|---|---|
| `Count` | band size | 0 |
| `Sum` | Σx | 1 |
| `Mean` | Σx / n | 1 |
| `SumOfSquaredDeviations` | Σ(x − mean)² | 1 |
| `Variance` | SSD / (n − 1) | 2 |
| `PopulationVariance` | SSD / n | 1 |
| `StdDev` | √Variance | 2 |
| `PopulationStdDev` | √PopulationVariance | 1 |
| `StandardErrorOfMean` | StdDev / √n | 2 |
| `CoefficientOfVariation` | StdDev / Mean when Mean ≠ 0 | 2 |
| `MeanAbsoluteDeviation` | mean of \|x − Mean\| | 1 |
| `MedianAbsoluteDeviation` | median of \|x − Median\| | 1 |
| `Skewness` | Excel `SKEW` (bias-adjusted G1) | 3 and StdDev > 0 |
| `ExcessKurtosis` | Excel `KURT` (bias-adjusted G2) | 4 and StdDev > 0 |
| `Kurtosis` | `ExcessKurtosis + 3` | 4 and StdDev > 0 |
| `GeometricMean` | exp(mean ln x) | all x > 0 |
| `HarmonicMean` | n / Σ(1/x) | all x > 0 |

Excel `SKEW` / `KURT` formulae are binding so hosts can cross-check a column in Excel.

### 8.5 Percentiles

| API | Rule |
|---|---|
| `Percentile(p)` | `PERCENTILE.INC` for any `p ∈ [0, 1]` |
| `NamedPercentiles()` | dictionary of p → value for `{0.01, 0.05, 0.10, 0.25, 0.50, 0.75, 0.90, 0.95, 0.99}` |

P50 is the median. P25 is Q1. P75 is Q3. P90 / P95 / P99 are tail cuts, **not** confidence levels.

---

## 9. Confidence

### 9.1 What we are not doing

A confidence level is an **input**. The helper does not look at a sample and return “this batch is 95 % confident.”

You do not need the entire population. That is the reason the interval exists. If the host *does* hold every member of a finite population (`n = N`), sampling uncertainty is zero and the interval should collapse (see §9.4).

Surveying 95 % of endpoints is a **sample fraction**. It is not a 95 % confidence level.

### 9.2 Level object

```text
ConfidenceLevel.Default        // 0.95
ConfidenceLevel.Of(0.99)
γ ∈ (0, 1)                     // otherwise throw
Alpha = 1 - γ
```

Recommended advertised levels: 0.90, 0.95, 0.99. Others in `(0, 1)` are legal.

### 9.3 Intervals (two-sided)

`NumericSeries.Confidence(γ = 0.95)` and `SeriesSlice.Confidence(γ)` return `ConfidenceReport` for that series / band.

| Parameter | Method | Min n | Notes |
|---|---|---|---|
| Mean | Student t: `x̄ ± t_{α/2, n−1} · s/√n` | 2 | Exact under i.i.d. normal; large-n via CLT |
| Median | Order-statistic normal approximation: ranks `⌊(n − z√n)/2⌋` and `⌈1 + (n + z√n)/2⌉`, clamped to `[1, n]` | 1 (degenerate n = 1) | Distribution-free large-sample |
| Variance | χ²: `((n−1)s² / χ²_{α/2, n−1}, (n−1)s² / χ²_{1−α/2, n−1})` | 2 | Assumes normality |
| StdDev | Square root of the variance interval | 2 | Same assumption |

`ConfidenceInterval.IsDefined = false` and null bounds when the method cannot run. Never throw from `Confidence` because n is small.

Inverse t and χ²: MathNet.Numerics only.

### 9.4 Finite population correction (accepted)

When the caller **knows** `N` and `1 ≤ n ≤ N`, the mean standard error becomes:

```text
SE_fpc = (s / √n) × √((N − n) / (N − 1))
```

- Overload: `Confidence(double level, int populationSize)`.
- When `n = N`, `SE_fpc = 0` and the mean interval is a point at the sample mean (`Lower = Upper = Estimate`). That is the census case: no interval is needed; the helper still returns a defined degenerate interval rather than throwing.
- When `n > N`, throw `ArgumentOutOfRangeException`.
- When `populationSize` is omitted, treat the source as conceptually infinite (current v1.1 behaviour). That is the default for ping / HTTP durations.
- Median / variance / stddev intervals stay uncorrected in this version unless implementation can apply a documented analogue without new dependencies. Mean is the required FPC target.

### 9.5 Related numbers (accepted)

| API | Meaning |
|---|---|
| `Mean.Width` / margin | `t × SEM` (half-width) at `γ` |
| `MeanPValue(μ₀)` | two-sided Student t p-value of `H₀: μ = μ₀` |
| `MeanConfidenceLevelContaining(μ₀)` | just-covering level `1 − p`. Dual of the t-test. XML docs must say it is **not** “the confidence of the sample.” |
| `SampleSizeForMeanMargin(margin, γ = 0.95)` | smallest n such that planned mean margin ≤ `margin`, using current sample `s` as the planning SD; iterate because t depends on df. Null if `s` is unavailable or `margin ≤ 0` throws. |
| `ProportionAbove(threshold, γ = 0.95)` | Wilson score interval of `k/n` where `k = count of x > threshold` |
| `ProportionAtLeast(threshold, γ = 0.95)` | Wilson score of `x ≥ threshold` |

Wilson is not implicit. Callers who want “share of probes slower than 50 ms” pass `50`.

### 9.6 How hosts should use the mean interval

Normative decision sketch (documentation, not code in this library):

```text
ci = series.Confidence(0.95)
if ci.Mean.Upper < sla        → pass (high end still under the line)
if ci.Mean.Lower > sla        → fail
else                          → not enough information; collect more samples
                                (do not raise γ to “feel more sure”)
```

Raising `γ` from 0.95 to 0.99 **widens** the interval. More samples **narrow** it.

---

## 10. Chart-ready numbers (no charting)

This library publishes sequences a later chart helper or WPF view can bind. It does not draw.

### 10.1 Required numeric views

On `NumericSeries` (and available from `Full` where that is equivalent):

| View | Points | X | Y |
|---|---|---|---|
| `SampleOrderPoints()` | n | index `0 .. n-1` (encounter order) | `Values[i]` |
| `SortedPoints()` | n | index `0 .. n-1` | `Sorted[i]` |
| `EcdfPoints()` | n | `Sorted[i]` (value axis) | `(i + 1) / n` |
| `HistogramPoints()` | bin count | bin midpoint `(Lower + Upper) / 2` | `Count` |
| `HistogramRelativePoints()` | bin count | same midpoint | `RelativeFrequency` |
| `HistogramTrendPoints()` | bin count | same midpoint | OLS fitted count (trend line) |
| `ParetoPoints()` | bin count | rank `1 .. k` (count descending) | `ParetoPoint` (count + cumulative share) |
| `TimeSeriesPoints()` | only observations with non-null `At` | `At.UtcDateTime` as `DateTime` **or** OA date / Unix ms — pick one and document it. Recommendation: return `IReadOnlyList<(DateTimeOffset At, decimal Value)>` and let the chart helper convert. | value |

Preferred time-series API (binding):

```text
IReadOnlyList<TimedValue> TimeSeriesPoints()

readonly record struct TimedValue(DateTimeOffset At, decimal Value)
readonly record struct ChartPoint(double X, double Y)
```

`SampleOrderPoints`, `SortedPoints`, `EcdfPoints`, `HistogramPoints`, `HistogramRelativePoints`, `HistogramTrendPoints` return `IReadOnlyList<ChartPoint>`.

`ParetoPoints()` returns `IReadOnlyList<ParetoPoint>` (`Rank`, `Midpoint`, `Count`, `CumulativeShare`). The cumulative share is the Pareto line. The gallery draws it; this library does not.

`TimeSeriesPoints()` omits observations with null `At`. If none have times, return an empty list (do not throw).

### 10.2 Overlay numbers, not series

Callers who want a normal-curve overlay compute it themselves from `Mean` and `StdDev`. This library does not emit a discretized Gaussian in v1.2 (allowed later; not required).

Box-plot whiskers are the five-number summary plus `LowOutliers` / `HighOutliers`. No extra type is required.

### 10.3 Future charting library (out of this project)

A later helper (name TBD, e.g. `Vestigium.Helpers.Charts`) may:

- reference ScottPlot / OxyPlot / LiveCharts in **that** project
- consume `ChartPoint` / `TimedValue` / descriptors
- live under the Helpers solution or a host

Until that project exists, demos and PingIQ bind the numbers directly. **Do not** add a chart NuGet to `Vestigium.Helpers.Analytics.csproj`.

---

## 11. Logging, identity, packaging

| Item | Value |
|---|---|
| TFM | `net10.0` |
| `Identity` | `"Vestigium.Helpers.Analytics"` |
| Package description | Descriptive statistics, quartile bands, frequency, and confidence intervals for finite numeric series. |
| Package tags | `analytics;statistics;percentile;confidence;vestigium` |
| MathNet.Numerics | 5.0.0 (inverse CDF only) |
| Logging | none |
| Packable | yes |

`AnalyticsHelper.Identity` remains so suite smoke tests stay green.

---

## 12. Non-goals (this version)

- Chart controls, colors, palettes, WPF views
- Kernel density / smoothed PDF
- Time-bucket histograms (“probes per minute”)
- Streaming / rolling / exponential moving statistics
- Weighted observations
- Missing-value imputation (reject or, for time, skip that point in `Slice`)
- Autocorrelation, FFT, regression, ANOVA
- Shapiro–Wilk or other formal normality tests
- Bootstrap / BCa intervals
- Bayesian credible intervals
- Confidence interval **for P95** (may be a later version; not required now)
- Application Insights / OpenTelemetry metrics export
- Writing Vestigium log files
- Multi-series compare type (`NumericSeries.Compare(a, b)`). Hosts hold two series and compare descriptors themselves.

---

## 13. Acceptance criteria

Construction and fences

1. `From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })` → Min 1, Q1 3, Median 5, Q3 7, Max 9, Range 8.
2. `From(new[] { 1, 2, 3, 4 })` → Median 2.5, Q1 1.75, Q3 3.25.
3. IQR band of `{1..9}` is `{3,4,5,6,7}` in encounter order.
4. Q1 band of `{1..9}` is `{1,2,3}`; Q4 band is `{8,9}`.
5. Empty sequence throws. `double.NaN` throws. `null` sequence throws.
6. All-equal `{5,5,5}`: Range 0, IQR 0, Skewness and kurtosis null, Q2–Q4 empty, Q1 / Iqr / Full populated.

Shape and tail

7. Symmetric `{1..9}`: `|Skewness|` below a tight tolerance (implementation test uses `1e-12` or equivalent).
8. Right-skew sample `{10,11,11,12,12,12,13,13,14,40}`: `Skewness > 0`, `Max = 40`, `HighOutliers` contains `40`, P95 > P50.
9. `NamedPercentiles()` contains 0.90, 0.95, 0.99 and those values equal `Percentile` at the same p.

Confidence

10. `Confidence(0.95)` on `{1..9}` mean interval matches the published Student-t interval to `1e-9`.
11. `Confidence(0)` and `Confidence(1)` throw.
12. `MeanPValue(5)` on `{1..9}` is ~1; `MeanConfidenceLevelContaining(5)` is ~0 (just-covering of the sample mean).
13. `Confidence(0.95, populationSize: 9)` on `{1..9}` mean interval is degenerate (`Lower ≈ Upper ≈ 5`).
14. `ProportionAbove(7)` on `{1..9}` uses k = 2 (values 8, 9) and returns a defined Wilson interval.

Time

15. `From(new[] { 1.0, 2.0, 3.0 })` has `HasTimestamps == false`, `TimeSeriesPoints()` empty, `Slice(...)` throws.
16. Three observations at `t0`, `t0+1s`, `t0+2s` with values `10,20,30`: `Slice(t0+0.5s, t0+1.5s)` keeps only `20`, `Window.Kind == CallerSupplied`.
17. Observation with `At = null` inside a mixed list is kept in value stats and dropped from `Slice` / `TimeSeriesPoints`.

Façade and packaging

18. `AnalyticsHelper.Identity == "Vestigium.Helpers.Analytics"`.
19. Project file has no charting package reference.
20. `EcdfPoints()` for `{1,2,3,4}` has four points, last Y = 1.0, X values equal the sorted sample.

---

## 14. Test map

| Area | Home |
|---|---|
| Identity smoke | existing `SkeletonSmokeTests` |
| Fences, bands, NaN, CI, Wilson, p-value | `NumericSeriesTests` |
| Observation / Slice / window / ECDF points | add `NumericSeriesTimeTests` (or extend the same file) |

Tests do not require a chart library.

---

## 15. Implementation notes (non-normative but expected)

Current files under `src/Vestigium.Helpers.Analytics/`:

| File | Role |
|---|---|
| `AnalyticsHelper.cs` | Identity + factories |
| `NumericSeries.cs` | Snapshot, bands, confidence helpers, time slice, chart points |
| `SeriesSlice.cs` | Band descriptors |
| `Observation.cs` | Observation + SeriesWindow + TimedValue + ChartPoint + ParetoPoint |
| `DescriptiveStatistics.cs` | Moments, five-number, fences |
| `FrequencyTable.cs` | Exact counts + FD histogram |
| `Quantiles.cs` | PERCENTILE.INC |
| `Confidence.cs` | Level, interval, report, Wilson, FPC mean, planning n |
| `NumberConvert.cs` | `INumber<T>` → finite decimal |

v1.1 code already covers §§7–9.3 and most of §8. v1.2 work is: `Observation`, window, `Slice`, `LowOutliers` / `HighOutliers`, FPC overload, `ChartPoint` views. Do not regress Identity or PERCENTILE.INC fixtures.

---

## 16. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 7 Sep 2026 | Skeleton (“counters and timings”). Placeholder type only. |
| 1.1 | 7 Sep 2026 | `NumericSeries`, six slices, required descriptors, frequency, moments, confidence level vs interval, Wilson, p-value, planned n. |
| 1.2 | 7 Sep 2026 | Lossless capture of the design conversation: glossary (P vs γ vs sample fraction), right-tail reading, optional `Observation` / UTC window / `Slice`, FPC mean overload, chart-ready numeric views, explicit non-goals for charting and time-bucket histograms, host feed pattern, expanded acceptance. |
| 1.3 | 8 Sep 2026 | Histogram OLS trend points and Pareto points (count-desc bins + cumulative share). Gallery draws the trend line and Pareto chart. No charting NuGet. |
