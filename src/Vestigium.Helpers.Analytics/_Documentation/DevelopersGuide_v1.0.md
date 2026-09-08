# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 1.5  
**Status:** Design companion to SRS v1.5  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx` → `src/Vestigium.Helpers.Analytics/`.

The binding contract is `_Documentation/Requirements_v1.0.md` (document version **1.5**). This page is why it looks like this, and how to call it.

## Design

**Intent.** One in-process snapshot type for a finite batch of numbers. Hosts (PingIQ, galleries, later services) accumulate observations, then construct a `NumericSeries`. The library answers “what does this batch look like?”, “where is the slow tail?”, and “how uncertain is the mean?”. It does not draw and it does not persist.

**Locked decisions.**

| Decision | Why |
|---|---|
| Instance `NumericSeries`, not a static math bag | A series is a snapshot with identity (`SeriesId`) for HelperLog. |
| Values stored as `decimal` | Rank statistics and Excel PERCENTILE.INC cross-check without binary float noise. |
| Six slices from full-series fences | Q4 is “the slow group” without a second type. |
| P95 is a rank cut, γ is an input | Those words are not interchangeable. SRS §3 is binding language. |
| ControlLimits live here | Charts must not invent UCL/LCL. Mean ± kσ swallows a lone spike on small n; MovingRange exists because of that. |
| Time is optional metadata | Histogram, P95, and confidence sit on the value axis. |
| No charting NuGet | Sibling `Vestigium.Helpers.Charts` consumes the numbers. This project stays `net10.0`. |

**Shape.**

```
host buffer  →  NumericSeries.From / FromObservations
                     ├─ SeriesSlice × 6   (Full, Q1–Q4, IQR)
                     ├─ FrequencyTable    (exact + FD histogram)
                     ├─ Confidence(γ)
                     ├─ ControlLimits(method)
                     └─ ChartPoint views  (ECDF, hist, Pareto, …)
Charts / ClosedXml / PingIQ bind those numbers. This DLL does not reference them.
```

## Use (values only)

```csharp
using Vestigium.Helpers.Analytics;

var series = NumericSeries.From(new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2 }, name: "rtt-ms");

SeriesSlice full = series.Full;
decimal min = full.Min!.Value;
decimal p95 = full.Percentile(0.95);          // tail cut — not a confidence level
var slow = series.Q4;                         // right tail as a group
var high = full.HighOutliers;

ConfidenceReport ci = series.Confidence(0.95);
double? meanLo = ci.Mean.Lower;
double? meanHi = ci.Mean.Upper;

double? justContains = series.MeanConfidenceLevelContaining(12.0); // 1 − p, not “sample confidence”

var sigma = series.ControlLimits();                                  // mean ± 3s
var mr = series.ControlLimits(ControlLimitMethod.MovingRange);       // Shewhart individuals
var sla = ControlLimits.FromCaller(center: 12, upper: 30, lower: 0); // host / SLA fences
```

Pass `sigma`, `mr`, or `sla` to `Vestigium.Helpers.Charts`. This library does not draw.

## Use (optional timestamps)

```csharp
var observations = new[]
{
    new Observation(12.4m, at: DateTimeOffset.UtcNow.AddSeconds(-4)),
    new Observation(11.9m, at: DateTimeOffset.UtcNow.AddSeconds(-3)),
    new Observation(18.7m, at: DateTimeOffset.UtcNow.AddSeconds(-1)),
};

var timed = NumericSeries.FromObservations(observations, name: "rtt-ms");
var lastTwoSeconds = timed.Slice(DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow);

foreach (var point in timed.EcdfPoints())
{
    // point.X = ms, point.Y = fraction finished — bind in Charts
    _ = point;
}
```

`From(double[])` stays legal. Time is never required.

## Demo

`dotnet run --project src/Vestigium.Helpers.Analytics.Demo` opens the WPF gallery (same navy chrome as Vestigium.Logging). **Draw new sample** takes 1,000 unique integers from 1..100,000 with CSPRNG. Tabs: Overview, Sample, Summary, Bands, Histogram, Charts, Confidence, JSONL. The gallery is a **Charts host**: histogram, Pareto, ECDF, five-number box, and control charts are `ChartView` widgets. This library still has no ScottPlot reference.

The gallery calls `HelperWpfHost.Start` with `MinimumDiskLevel = Debug`. This library never calls `Initialize`. Factories, `Slice`, `Confidence`, and `ControlLimits` write Debug enter plus an Information constructed/confidence/limits line. Rejects write Error then throw. Percentile and histogram loops stay silent.

## Roadmap

Shipped surface is SRS v1.4. Next work is SRS §16, not a rewrite:

1. **Run rules** (Nelson / Western Electric) as indexes — Charts paints, Analytics computes.
2. **Confidence interval for a percentile** (fence on P95, not “95 % confidence”).
3. **`PdfPoints()`** so the bell overlay is an Analytics number.
4. **Two-series compare** (difference of means).

Never: charting, streaming sketches, time-bucket histograms, Bayesian, OTel.

## Do not

- Add OxyPlot / ScottPlot / LiveCharts to this project.
- Treat P95 as 95 % confidence.
- Call `Initialize` on `Vestigium.Logging` from this library.
- Bin the value histogram by clock time.
- Recompute UCL/LCL in a host “to make the spike show”. Pass `MovingRange` or `FromCaller` instead.

## Files

See SRS §15. Tests: `src/Vestigium.Helpers.Tests/NumericSeriesTests.cs`, `ControlLimitsTests.cs`.
