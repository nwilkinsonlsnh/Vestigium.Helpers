# Vestigium.Helpers.Analytics — Requirements Specification

**Document ID:** VEST-HLP-ANALYTICS-SRS-000  
**Version:** 1.6  
**Status:** Accepted — complete numeric surface + stabilize pass  
**Date:** 19 September 2026  
**Target:** .NET 10 LTS / Visual Studio 2026 / `net10.0`  
**Companion:** `DevelopersGuide_v1.0.md` (design + how to call); `StabilizationPlan_v1.0.md`  
**Project:** `src/Vestigium.Helpers.Analytics/`

The full v1.6 text is the previous v1.5 contract plus the stabilize amendments. Binding changes from v1.5:

- Logging door is `Vestigium.Logging` + `AnalyticsCatalog.Register` (EVENTID 10500+). The library never calls `Initialize`. Not HelperLog.
- A5: published `Values` / `Sorted` / `Times` are frozen copies.
- A9: MathNet is called only from internal `QuantileFunctions`.
- Public types include `ControlLimits`, `ControlLimitMethod`, `ChartPoint`, `ParetoPoint`, `TimedValue`, `AnalyticsCatalog`, `AnalyticsEvents`.
- Tukey `LowOutlierIndexes` / `HighOutlierIndexes` / `OutlierIndexes` are encounter indexes into that slice.
- `TimeSeriesPoints()` sorts by `At.UtcTicks`, then original encounter index. Null `At` omitted.
- `TryControlLimits` returns false when the method cannot run. Throwing `ControlLimits()` stays.
- `ControlLimitMethod.MovingRange` is legal only on `SliceKind.Full`.
- Package tags: `analytics;statistics;quartile;confidence-interval;control-chart;shewhart;vestigium`.
- Package description names control limits.
- §16.2 features (run rules, percentile CI, PdfPoints, two-series compare) remain parked.

See `_Documentation/StabilizationPlan_v1.0.md` for the work that produced this version and `DevelopersGuide_v1.0.md` for how to call the surface.

The detailed sections §0–§16 of v1.5 remain in force except where the bullets above replace them (A3, A5, A9, §4 types, §5.3 frozen lists, §8.2 indexes, §10.1 clock order, §10.2 Try + MR-on-Full, §11 packaging/logging).
