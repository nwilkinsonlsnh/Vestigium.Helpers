# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 1.0  
**Status:** Draft — pending acceptance  
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
| TFM | `net10.0` |

Construction examples (intent, not final signatures):

```
var rtt = new NumericSeries(pingTimes);
var rtt = new NumericSeries<int>(rawCounts);
var rtt = NumericSeries.From(values);
```

A host that already has `double[]` or `List<decimal>` must not have to cast by hand.

## 3. Slice model

Quartile *values* (Q1, Q2, Q3) are scalars. Quartile *bands* are the subsets of the series that fall in each quarter of the ordered sample. Hosts asked for Range / Frequency / Skewness / Kurtosis *on each quartile and on IQR*, which only makes sense on the bands.

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
```

## 4. Required descriptors (v1)

These are required on `Full`, `Q1`, `Q2`, `Q3`, `Q4`, and `Iqr`.

| Method / property | Meaning |
|---|---|
| `Count` | N in this slice |
| `Range` | `(Min, Max)` of the slice. Also `Span` = Max − Min. |
| `FiveNumberSummary` | Min, Q1, Q2, Q3, Max computed **on that slice**. On a small slice this may collapse (Min = Q1, etc.). |
| `Quartiles` | Q1, Q2, Q3 of the slice |
| `Iqr` | Q3 − Q1 of the slice |
| `Frequency` | Exact-value frequencies after normalization (value → count). Also `DistinctCount`. |
| `Skewness` | Sample skewness (bias-corrected, G1). |
| `Kurtosis` | Sample excess kurtosis (G2). Document both raw and excess; default property is excess. |

On `Full` only, also expose the hinge scalars used to *cut* the bands so hosts do not have to fish them out of `Full.FiveNumberSummary`:

| Member | Meaning |
|---|---|
| `Min` / `Max` | Sample extremes |
| `Q1` / `Q2` / `Q3` | Hinges of the full series |
| `IqrValue` | Full-series Q3 − Q1 |

## 5. Proposed descriptors (v1 unless marked later)

These are the extra points that pay rent for diagnostic hosts. Same object, same slices.

### Location

| Member | Why |
|---|---|
| `Sum` | Cheap, and hosts already think in totals (timeouts, bytes). |
| `Mean` | Arithmetic mean. |
| `Median` | Alias of Q2. |
| `Mode` | Most frequent value(s). Return a small list — series can be multimodal. |
| `MidRange` | (Min + Max) / 2. Fast sanity check against Mean. |

### Scale

| Member | Why |
|---|---|
| `Variance` | Sample variance (N − 1). |
| `StdDev` | Sample standard deviation. |
| `StdError` | StdDev / √N. |
| `CoefficientOfVariation` | StdDev / Mean when Mean ≠ 0. |
| `Mad` | Median absolute deviation. Robust scale next to StdDev. |
| `MeanAbsoluteDeviation` | Mean of ┆x − Mean┆. |

### Tails and outliers

| Member | Why |
|---|---|
| `TukeyFences` | Lower = Q1 − 1.5·IQR, Upper = Q3 + 1.5·IQR (and 3.0·IQR extreme fences). |
| `Outliers` | Values outside the inner fences, with count and the actual values. |
| `Percentile(p)` | Arbitrary p in [0, 1] or [0, 100] — pick one scale and stick to it. Recommend 0–100. |
| `P01` `P05` `P10` `P90` `P95` `P99` | Named tails hosts actually print on RTT / hop / payload size. |

### Shape extras

| Member | Why |
|---|---|
| `IsSymmetric` | ┆Skewness┆ under a documented epsilon (default 0.5). Flag, not a proof. |
| `IsPlatykurtic` / `IsLeptokurtic` | Sign of excess kurtosis. |
| `Moments` | 1st–4th central moments for hosts that want to store raw moments. |

### Frequency extras

| Member | Why |
|---|---|
| `RelativeFrequency` | Count / N per distinct value. |
| `Histogram(binCount)` | Equal-width bins over the slice Range. Default bin count = Sturges, cap at 32. |
| `CumulativeFrequency` | Running counts on the ordered distinct values. |

Hold for a later SRS (do not implement in v1):

- Geometric / harmonic mean
- Rolling / windowed series
- Weighted observations
- Two-sample compare (t, KS)
- Formal normality tests
- Streaming / online updates after construction

## 6. Computation rules

1. Copy the source sequence at construction. Later mutation of the caller's list must not change the instance.
2. Drop non-finite values (`NaN`, `±Infinity`). Record `ExcludedCount`. Log `Warning` once if anything was dropped.
3. Empty after exclusion → throw `ArgumentException` via `HelperGuard`.
4. Quartile algorithm: **linear interpolation, inclusive** (Excel `PERCENTILE.INC` / NIST R7). Document the formula in the Developers Guide so PingIQ and Excel agree.
5. Skewness / kurtosis require N ≥ 3 and N ≥ 4 respectively. Below that, return `null` (nullable `decimal?`) rather than fake 0.
6. Frequency keys are the normalized `decimal` values. Do not string-format them.
7. Thread-safe for reads after construction. Not safe to construct the same instance from two threads.
8. Do not allocate on property get beyond returning small records. Compute once in the constructor (or lazily with a lock-free cache on first read of that slice).

## 7. Records (intent)

Keep these as readonly records in the same assembly. Names can move during implementation as long as the fields stay.

```
Range(Min, Max, Span)
FiveNumberSummary(Min, Q1, Q2, Q3, Max)
FrequencyBucket(Value, Count, Relative)
HistogramBin(Lower, Upper, Count)
TukeyFences(InnerLower, InnerUpper, OuterLower, OuterUpper)
OutlierSet(Count, Values, Fences)
```

`NumericSlice` is the reusable descriptor (Range, FiveNumberSummary, Frequency, Skewness, Kurtosis, plus the v1 extras in §5). `NumericSeries` holds `Full`, `Q1`, `Q2`, `Q3`, `Q4`, `Iqr` plus the full-series hinge scalars.

## 8. Logging

APPID `Analytics`. CATEGORY `Helpers`. SUBCATEGORY `Analytics` or `Probe`.

| Event | LEVEL | STATUS |
|---|---|---|
| Construct started | Information | Pending |
| Construct finished (N, ExcludedCount) | Information | Success |
| Dropped non-finite values | Warning | Success |
| Empty after exclusion | Error | Failed |
| Probe | Information | Pending then Success |

## 9. Tests

- Mixed types in one series (`int`, `uint`, `decimal`, `double`) produce the same hinges as the all-decimal equivalent.
- Known fixture: `{1,2,3,4,5,6,7,8,9}` — document expected Q1/Q2/Q3 under the chosen percentile rule.
- Slice membership covers the full sample with no gaps at the hinges (inclusive rule).
- Frequency counts sum to `Count`.
- Skewness of a symmetric fixture is ~0.
- Empty and all-NaN throw.
- Tests use a temp `LogDirectory`. Never live ProgramData.

## 10. Demo

`Vestigium.Helpers.Analytics.Demo` builds a `NumericSeries` from a fixed fixture, prints `Full` and each slice (Range, Frequency, Skewness, Kurtosis), then the five-number summary. JSONL still lands under `%ProgramData%\Vestigium\Logs\Analytics\`.

## 11. Out of scope (v1)

- Persisting series or writing `.csv` / `.xlsx` (use FileIo / ClosedXml from the host).
- Charting.
- Multi-series join.
- Incremental `Add(value)` after construction.

## 12. Open items

- Confirm Q1–Q4 mean **bands** (this draft) and not four extra scalar hinges.
- Percentile scale: 0–100 vs 0–1.
- Default histogram bin policy (Sturges vs fixed 10).
- Whether `Mode` on a unique-valued series returns every value or an empty list.
