# Vestigium.Helpers.Analytics — Hygiene and library-complete features

**Document ID:** VEST-HLP-ANALYTICS-PLAN-001  
**Version:** 1.2  
**Status:** S4 and S5 closed 19 Sep 2026 — S6 is the stop  
**Date:** 19 September 2026  
**Binding contract:** `Requirements_v1.0.md` (SRS v1.6)  
**Predecessor:** `StabilizationPlan_v1.0.md` (closed)

This plan is **library-only**. Consumers stay callers of `NumericSeries` / `SeriesSlice` / `ControlLimits`. No host adapter lives in this project.

| Slice | Items | Status |
|---|---|---|
| S4 | H1–H4 XML / SRS / closed stabilize / guide pointer | Closed |
| S5 | L1 `ControlLimits.Against`, L2 `PercentileRank` | Closed |
| S6 | stop | Next: re-evaluate L4–L7 |

## S5 that shipped

```csharp
ControlLimits scored = ControlLimits.FromCaller(12, 30, 0).Against(values);
// fences unchanged; OutOfControlIndexes filled

double rank = series.Full.PercentileRank(5m); // count(≤ x) / n
```

`PercentileRank` is an extension on `SeriesSlice` (`SeriesSliceRanks`) so the call site is `slice.PercentileRank(x)`. Empty band throws the same `InvalidOperationException` as `Percentile`. Null or empty `Against` input → count 0, no throw.

L4 run rules, L5 percentile interval, L6 `PdfPoints`, L7 two-series stay parked until we look again.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Hygiene H1–H4, library holes L1–L2, later L4–L7 parked. |
| 1.1 | 19 Sep 2026 | S4 closed. |
| 1.2 | 19 Sep 2026 | S5 closed. |
