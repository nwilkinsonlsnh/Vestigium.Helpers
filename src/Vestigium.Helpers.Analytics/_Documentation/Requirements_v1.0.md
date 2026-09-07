# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 1.1  
**Status:** Accepted for implementation  
**Date:** 7 September 2026

## 1. Purpose

Give diagnostic hosts (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost) an in-process numeric series analyzer. Construct once from a set of numbers of any numeric type. Read the same family of descriptors on the whole series and on each quartile band and the IQR band.

This is not a telemetry pipeline and not a stats package for publishing. It is a snapshot calculator over values already in memory.

## 2. Shape

| Item | Decision |
|---|---|
| Primary type | Instance class `NumericSeries` |
| Lifetime | Immutable after construction. The source sequence is copied and sorted once. |
| Numeric type | Accept any `INumber<T>` / `IConvertible` integer, unsigned, signed, float, double, decimal. Internally normalize to `decimal` for exact-ish summary work. |
| Static surface | Keep `AnalyticsHelper` only for `Identity` and `Probe()`. Do not put series math on the static type. |
| Logging | `HelperLog` on construct / reject / empty. Do not log on every property get. |
| Allowed package | `MathNet.Numerics` for t and chi-square quantiles used by confidence intervals. |
| TFM | `net10.0` |

Construction examples (intent, not final signatures):

```
var rtt = new NumericSeries(pingTimes);
var rtt = new NumericSeries<int>(rawCounts);
var rtt = NumericSeries.From(values);
var rtt = new NumericSeries(values, confidenceLevel: 0.95m);
```

A host that already has `double[]` or `List<decimal>` must not have to cast by hand.

Closed decisions:

| Item | Decision |
|---|---|
| Q1–Q4 | Bands of the sample, not extra scalar hinges. |
| Percentile scale | 0–100. `Percentile(95)` is P95. |
| Unique-valued `Mode` | Empty list. Mode means a peak, not "every value appeared once". |
| Histogram bins | Sturges, cap 32. Host may pass `binCount`. |
| Default confidence level | 0.95 (95%). |

## 3. Slice model

Quartile *values* (Q1, Q2, Q3) are scalars. Quartile *bands* are the subsets of the series that fall in each quarter of the ordered sample.

| Slice | Membership (inclusive on the inner hinge) |
|---|---|
| `Full` | Entire finite sample |
| `Q1` | `Min … Q1` |
| `Q2` | `Q1 … Q2` (median) |
| `Q3` | `Q2 … Q3` |
| `Q4` | `Q3 … Max` |
| `Iqr` | `Q1 … Q3` |

`Q4` is the upper quarter of the sample, not a fourth hinge. The five-number summary stays Min, Q1, Q2, Q3, Max.

Every slice exposes the same descriptor so hosts can write one printer.

```
series.Full.Range
series.Q1.Frequency
series.Iqr.Skewness
series.Q4.Kurtosis
series.Full.MeanConfidence
series.Iqr.ConfidenceInterval(0.99m)
```

## 4. Required descriptors (v1)

Required on `Full`, `Q1`, `Q2`, `Q3`, `Q4`, and `Iqr`.

### Core

| Member | Meaning |
|---|---|
| `Count` | N in this slice |
| `Range` | `(Min, Max)` of the slice. Also `Span` = Max − Min. |
| `FiveNumberSummary` | Min, Q1, Q2, Q3, Max computed **on that slice**. On a small slice this may collapse. |
| `Quartiles` | Q1, Q2, Q3 of the slice |
| `Iqr` | Q3 − Q1 of the slice |
| `Frequency` | Exact-value frequencies after normalization (value → count). Also `DistinctCount`. |
| `Skewness` | Sample skewness (bias-corrected, G1). |
| `Kurtosis` | Sample excess kurtosis (G2). Also expose raw kurtosis. Default property is excess. |

### Location

| Member | Meaning |
|---|---|
| `Sum` | Sum of the slice |
| `Mean` | Arithmetic mean |
| `Median` | Alias of Q2 |
| `Mode` | Most frequent value(s). Empty when every value is unique. Multimodal returns every peak. |
| `MidRange` | (Min + Max) / 2 |

### Scale

| Member | Meaning |
|---|---|
| `Variance` | Sample variance (N − 1) |
| `StdDev` | Sample standard deviation |
| `StdError` | StdDev / √N |
| `CoefficientOfVariation` | StdDev / Mean when Mean ≠ 0, else `null` |
| `Mad` | Median absolute deviation |
| `MeanAbsoluteDeviation` | Mean of ┆x − Mean┆ |

### Tails and outliers

| Member | Meaning |
|---|---|
| `TukeyFences` | Inner 1.5·IQR and outer 3.0·IQR fences |
| `Outliers` | Values outside the inner fences, with count and the values |
| `Percentile(p)` | p in `[0, 100]` |
| `P01` `P05` `P10` `P90` `P95` `P99` | Named tail percentiles |

### Shape

| Member | Meaning |
|---|---|
| `IsSymmetric` | ┆Skewness┆ < 0.5. Flag, not a proof. |
| `IsPlatykurtic` / `IsLeptokurtic` | Sign of excess kurtosis |
| `Moments` | 1st–4th central moments |

### Frequency extras

| Member | Meaning |
|---|---|
| `RelativeFrequency` | Count / N per distinct value |
| `Histogram(binCount)` | Equal-width bins over the slice Range. Default = Sturges, cap 32 |
| `CumulativeFrequency` | Running counts on the ordered distinct values |

### Confidence

Confidence *level* is an input (0.90, 0.95, 0.99, …). Confidence *interval* is the output (Lower, Upper) around an estimate.

| Member | Meaning |
|---|---|
| `ConfidenceLevel` | Level used by the default interval properties. Constructor default `0.95m`. Must be in `(0.50, 0.999]`. |
| `MeanConfidence` | Two-sided Student t interval for the mean at `ConfidenceLevel`. |
| `MedianConfidence` | Two-sided order-statistic interval for the median at `ConfidenceLevel`. |
| `StdDevConfidence` | Two-sided chi-square interval for the standard deviation at `ConfidenceLevel`. |
| `ConfidenceInterval(level)` | Same three intervals at a caller-chosen level without rebuilding the series. |
| `ProportionConfidence(value)` | Wilson interval for the relative frequency of one distinct value. |

On `Full` only, also expose the hinge scalars used to cut the bands:

| Member | Meaning |
|---|---|
| `Min` / `Max` | Sample extremes |
| `Q1` / `Q2` / `Q3` | Hinges of the full series |
| `IqrValue` | Full-series Q3 − Q1 |
| `ExcludedCount` | Non-finite values dropped at construction |
| `ConfidenceLevel` | Series default level |

## 5. Confidence rules

A confidence level is not a property of the data. It is the host saying "cover the parameter this often in the long run." 95% is the suite default because that is what operators already read on probe dashboards.

1. Mean interval uses Student's t with `df = N − 1`:

   `Mean ± t_{1-α/2, N-1} · StdError`

   Requires N ≥ 2 and StdDev > 0. Otherwise `null`.
2. Median interval uses the binomial / order-statistic method on the sorted sample (closed-form ranks, no bootstrap in v1).
3. StdDev interval uses the chi-square pivot:

   `sqrt((N-1) s² / χ²_{1-α/2, N-1})` … `sqrt((N-1) s² / χ²_{α/2, N-1})`

   Requires N ≥ 2.
4. Proportion interval uses Wilson score, not Wald. Wald blows up on 0/N and N/N, which is common for exact-value frequencies.
5. Quantiles for t and χ² come from `MathNet.Numerics`. Do not hand-roll the inverse CDF.
6. The interval does **not** prove the sample is normal. Hosts may print it next to `IsSymmetric` / `Skewness` so a reader can judge the assumption.
7. `ConfidenceInterval(level)` must not mutate the instance. Default properties keep using the constructor level.

Record:

```
ConfidenceInterval(Level, Estimate, Lower, Upper, Method)
```

`Method` is `StudentT`, `OrderStatistic`, `ChiSquare`, or `Wilson`.

## 6. Computation rules

1. Copy the source sequence at construction. Later mutation of the caller's list must not change the instance.
2. Drop non-finite values (`NaN`, `±Infinity`). Record `ExcludedCount`. Log `Warning` once if anything was dropped.
3. Empty after exclusion → throw `ArgumentException` via `HelperGuard`.
4. Quartile algorithm: **linear interpolation, inclusive** (Excel `PERCENTILE.INC` / NIST R7).
5. Skewness / kurtosis require N ≥ 3 and N ≥ 4 respectively. Below that, return `null` rather than fake 0.
6. Frequency keys are the normalized `decimal` values. Do not string-format them.
7. Thread-safe for reads after construction. Not safe to construct the same instance from two threads.
8. Do not allocate on property get beyond returning small records. Compute once in the constructor (or lazily with a lock-free cache on first read of that slice).

## 7. Records (intent)

```
Range(Min, Max, Span)
FiveNumberSummary(Min, Q1, Q2, Q3, Max)
FrequencyBucket(Value, Count, Relative)
HistogramBin(Lower, Upper, Count)
TukeyFences(InnerLower, InnerUpper, OuterLower, OuterUpper)
OutlierSet(Count, Values, Fences)
ConfidenceInterval(Level, Estimate, Lower, Upper, Method)
```

`NumericSlice` is the reusable descriptor. `NumericSeries` holds `Full`, `Q1`, `Q2`, `Q3`, `Q4`, `Iqr` plus the full-series hinge scalars.

## 8. Logging

APPID `Analytics`. CATEGORY `Helpers`. SUBCATEGORY `Analytics` or `Probe`.

| Event | LEVEL | STATUS |
|---|---|---|
| Construct started | Information | Pending |
| Construct finished (N, ExcludedCount, ConfidenceLevel) | Information | Success |
| Dropped non-finite values | Warning | Success |
| Empty after exclusion | Error | Failed |
| Probe | Information | Pending then Success |

## 9. Tests

- Mixed types in one series (`int`, `uint`, `decimal`, `double`) produce the same hinges as the all-decimal equivalent.
- Known fixture: `{1,2,3,4,5,6,7,8,9}` — document expected Q1/Q2/Q3 under PERCENTILE.INC.
- Slice membership covers the full sample with no gaps at the hinges.
- Frequency counts sum to `Count`.
- Unique-valued fixture → `Mode` is empty.
- Skewness of a symmetric fixture is ~0.
- 95% mean CI for `{1,2,3,4,5,6,7,8,9}` contains the mean and matches a documented t critical value.
- `ConfidenceInterval(0.99m)` is wider than the 95% interval on the same slice.
- Empty and all-NaN throw.
- Tests use a temp `LogDirectory`. Never live ProgramData.

## 10. Demo

`Vestigium.Helpers.Analytics.Demo` builds a `NumericSeries` from a fixed fixture, prints `Full` and each slice (Range, Frequency, Skewness, Kurtosis, MeanConfidence), then the five-number summary. JSONL still lands under `%ProgramData%\Vestigium\Logs\Analytics\`.

## 11. Out of scope (v1)

- Persisting series or writing `.csv` / `.xlsx` (use FileIo / ClosedXml from the host).
- Charting.
- Multi-series join / two-sample tests.
- Incremental `Add(value)` after construction.
- Bootstrap intervals.
- Geometric / harmonic mean.
- Rolling / windowed series.
- Weighted observations.
- Formal normality tests.
