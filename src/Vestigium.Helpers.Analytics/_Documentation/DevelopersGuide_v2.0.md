# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 2.0  
**Status:** How-to companion to SRS v2.0  
**Date:** 19 September 2026  
**Binding:** `Requirements_v2.0.md`  
**Design:** `Design_v2.0.md`

Open `Vestigium.Helpers.slnx` → `src/Vestigium.Helpers.Analytics/`.

---

## Logging (host)

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "Analytics"; // or the host APPID — folder follows this
    AnalyticsCatalog.Register(cfg);
});
```

This library never calls `Initialize`. Writes no-op if Logging is down; math still runs. EventIds 10500+. JSON shard `EventCatalog/analytics.json`.

---

## Values only

```csharp
using Vestigium.Helpers.Analytics;

var series = NumericSeries.From(new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2 }, "rtt-ms");

decimal p95 = series.Full.Percentile(0.95);          // tail cut — not γ
double rank = series.Full.PercentileRank(12.0m);     // share ≤ 12
var slow = series.Q4;
var highAt = series.Full.HighOutlierIndexes;         // into Full.Values

ConfidenceReport ci = series.Confidence(0.95);
var p95Ci = series.PercentileInterval(0.95);         // order-stat or sample range
var pdf = series.PdfPoints();                        // empty if s = 0

if (series.TryControlLimits(out var sigma))
    _ = sigma;
var thrown = series.ControlLimits();                 // mean ± 3s
var mr = series.ControlLimits(ControlLimitMethod.MovingRange);
var sla = ControlLimits.FromCaller(12, 30, 0);
var scored = sla.Against(series.Values);

var spec = SpecLimits.From(0, 30);
var cap = series.Capability(spec);                   // Pp from s, Cp from MR on Full
var rules = series.RunRules();                       // WE 1–4 + Nelson 5–8

foreach (var band in series.Bands)
{
    if (band.TryControlLimits(out var bandLimits))
        _ = bandLimits;                              // empty Q4 → false
}
```

Pass `sigma` / `mr` / `sla`, `rules`, and `spec` to `Vestigium.Helpers.Charts`. Do not call `Q4.ControlLimits(MovingRange)` or `Q4.RunRules()`.

---

## Optional timestamps

```csharp
var timed = NumericSeries.FromObservations(
[
    new Observation(12.4m, DateTimeOffset.UtcNow.AddSeconds(-4)),
    new Observation(11.9m, DateTimeOffset.UtcNow.AddSeconds(-3)),
    new Observation(18.7m, DateTimeOffset.UtcNow.AddSeconds(-1)),
], "rtt-ms");

var lastTwo = timed.Slice(DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow);
var line = timed.TimeSeriesPoints();                 // clock, then encounter index
```

`From(double[])` stays legal. Time is never required. Encounter order remains `SampleOrderPoints()` plus `Times`.

---

## Two series

```csharp
var cmp = before.Compare(after);
// cmp.WelchTwoSidedP may be null when n < 2 or s = 0
// cmp.Paired is set only when counts match
```

---

## Charts doors that consume this library

```csharp
ChartView.Control(series, limits, series.RunRules(), new ChartOptions { Spec = spec });
ChartView.Histogram(series, new ChartOptions { ShowBellCurve = true, ShowKde = true });
ChartView.MeanInterval(series, 0.80);
ChartView.PercentileInterval(series, p: 0.95, level: 0.95);
```

Charts must not recompute UCL/LCL, run rules, KDE bandwidth, or percentile bounds.

---

## Demo

`dotnet run --project src/Vestigium.Helpers.Analytics.Demo` (Windows gallery). The gallery is a host: it must Initialize + Register. This library has no ScottPlot reference.

---

## Tests

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Analytics
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02_
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_
```

---

## Do not

- Add OxyPlot / ScottPlot / LiveCharts to this project.
- Treat P95 as 95 % confidence.
- Call `VestigiumLogger.Initialize` from this library.
- Bin the value histogram by clock time.
- Recompute UCL/LCL in a host “to make the spike show”. Pass MovingRange or FromCaller.
- Run moving-range or run-rules on Q1–Q4 / IQR.
- Cast `Values` to `List<decimal>` and mutate it.
- Put a PingIQ / host adapter in this project.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.8 | 19 Sep 2026 | Companion to SRS v1.6. |
| 2.0 | 19 Sep 2026 | Lossless how-to for SRS v2.0. Design moved out. |
