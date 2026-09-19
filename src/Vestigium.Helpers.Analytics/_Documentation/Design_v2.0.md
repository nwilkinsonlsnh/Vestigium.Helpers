# Vestigium.Helpers.Analytics — Design

**Document ID:** VEST-HLP-ANALYTICS-DSN-000  
**Version:** 2.0  
**Status:** Locked companion to SRS v2.0  
**Date:** 19 September 2026  
**Binding:** `Requirements_v2.0.md` wins on conflict

This page records *why* the library is shaped this way. It does not add requirements.

---

## 1. Intent

A finite batch arrives from a host buffer. `NumericSeries` is that batch plus identity for the log. Callers ask questions of the snapshot. Nothing here is a live process, a chart, or a product adapter.

```
host buffer
    → NumericSeries.From / FromObservations
         ├ SeriesSlice × 6     Full, Q1–Q4, IQR
         ├ FrequencyTable      exact + FD histogram
         ├ Confidence(γ)
         ├ PercentileInterval(p, γ)
         ├ PdfPoints()
         ├ ControlLimits / Try / Against
         ├ SpecLimits / Capability
         ├ RunRules            WE 1–4 + Nelson 5–8, Full only
         ├ Compare             Welch + optional paired delta
         └ ChartPoint views
Callers bind numbers. This DLL does not reference a host or ScottPlot.
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Instance `NumericSeries`, not a static bag | A series is a snapshot with `SeriesId`. |
| Store values as `decimal` | Rank stats and Excel PERCENTILE.INC cross-check without binary dust. |
| Moments as `double` | Variance / s / SEM need a divisor; decimal overflow on descriptors is a reject. |
| Frozen lists | A host cannot mutate `Values` behind the snapshot. Same for OOC indexes and moving ranges. |
| Six slices from Full fences | Q4 is “the slow group” without a second type. |
| P95 is a rank cut; γ is an input | Operators mix those words. SRS §1 is binding. |
| Empty percentile is one `InvalidOperationException` | One sentence for Percentile, NamedPercentiles, PercentileRank, PercentileInterval. |
| ControlLimits live here | Charts must not invent UCL/LCL. Mean ± kσ swallows a lone spike on small n. |
| Moving range is Full only | MR needs encounter neighbors. Q1–Q4 are value filters. |
| `TryControlLimits` for band loops | Empty Q4 must not throw. Throwing API stays for “I know this is a process.” |
| Spec ≠ control | LSL/USL are customer fences. CL/UCL are process fences. |
| Pp from s; Cp from MR̄/d2 | Overall vs within. Cp stays off value bands. |
| Run rules on Full only | Same neighbor reason as MR. |
| Strict runs / ties break Nelson 5 and 7 | Predictable, testable. |
| Time is optional metadata | Histogram, P95, and CI sit on the value axis. Clock order is `TimeSeriesPoints()`. |
| MathNet behind `QuantileFunctions` | Package stays. Public files do not name MathNet. |
| Logging APPID stamps library identity | Folder follows the host. Matches Json / Encryption / FileIo / Network. |
| No charting NuGet | Sibling Charts consumes numbers. This project stays `net10.0`. |
| No host adapter | PingIQ / orchestration is a different project. |

---

## 3. Math notes (not a formula sheet for Charts)

**PERCENTILE.INC.** Position = 1 + p(n − 1). Interpolate between adjacent order stats. p = 0 → min. p = 1 → max.

**Student-t mean.** µ̂ ± t_{n−1, 1−α/2} · SEM. FPC multiplies SEM by √((N − n)/(N − 1)) when N is known and n < N.

**Median interval.** Normal approximation to order-stat ranks, clamped to 1..n.

**Variance.** χ² on SSD; bounds SSD/χ²_{1−α/2} and SSD/χ²_{α/2}.

**Wilson.** Proportion interval; defined at n ≥ 1.

**Percentile interval.** Binomial coverage of ranks (j, k). Tightest pair at ≥ γ, else sample range.

**KDE.** Gaussian kernel, Silverman h = 1.06 s n^{-⅕}. Empty when s = 0.

**Shewhart individuals MR.** d2(2) = 1.128. Span-2 moving range on encounter order.

**Welch.** Unequal-variance two-sample t. Satterthwaite df. Two-sided p from Student-t CDF.

Do not re-derive these in Charts.

---

## 4. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` | Required reference is null. |
| `ArgumentException` | Empty series, inverted window, MovingRange on a band, CallerSupplied on Compute, malformed spec (missing both sides or lower ≥ upper). |
| `ArgumentOutOfRangeException` | Non-finite value, p ∉ [0,1], γ ∉ (0,1), k ≤ 0, N invalid, descriptor overflow (inner `OverflowException`). |
| `InvalidOperationException` | Empty-band percentile family; run rules off Full; Slice without time; ControlLimits when the method cannot run. |

`Try*` never swallows caller errors. Insufficient data is false, not throw.

---

## 5. Files

| File | Role |
|---|---|
| `NumericSeries.cs` | Snapshot, doors, time slice |
| `SeriesSlice.cs` | Band descriptors + limits/confidence |
| `SeriesSlice.Rank.cs` | `PercentileRank` |
| `DescriptiveStatistics.cs` | Compute core |
| `FrequencyTable.cs` | Exact + FD + Pareto/trend |
| `Quantiles.cs` | PERCENTILE.INC + empty reject |
| `QuantileFunctions.cs` | MathNet only |
| `Confidence.cs` | γ, intervals, Wilson, plan-n |
| `PercentileInterval.cs` | Order-stat CI |
| `KernelDensity.cs` | `PdfPoints` |
| `ControlLimits.cs` | Fences, Against, log cap |
| `SpecLimits.cs` | LSL/USL |
| `ProcessCapability.cs` | Pp from s, Cp from MR |
| `RunRules.cs` | WE + Nelson |
| `SeriesCompare.cs` | Welch + paired |
| `NumberConvert.cs` | Freeze, convert, overflow |
| `AnalyticsLog.cs` / `AnalyticsCatalog.cs` / `AnalyticsEvents.cs` | Logging |
| `Observation.cs` | Timed value |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 6. What closed to reach 2.0

| Pass | Outcome |
|---|---|
| Stabilize W1–W8 | Try, MR-on-Full, Tukey indexes, freeze, clock order, QuantileFunctions, logging docs |
| S5 | `Against`, `PercentileRank` |
| PR02 | Frozen OOC lists, log cap 32, one empty exception, overflow inner, name sanitize. APPID stamp kept. |
| PR03 | Spec, capability, WE 1–4, Compare |
| PR04 | PercentileInterval, PdfPoints, Nelson 5–8 |

Plans for those passes are archived under `Arcchive/PR01/`.

---

## 7. Still out

EWMA, CUSUM, bootstrap percentile CI, other kernels, soft cap on n, moments as decimal, host/PingIQ adapters, public MathNet, drawing.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 2.0 | 19 Sep 2026 | First standalone Design. Content lifted from Guide v1.8 + closed plans. |
