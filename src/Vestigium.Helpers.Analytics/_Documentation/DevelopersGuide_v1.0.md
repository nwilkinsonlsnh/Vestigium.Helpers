# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 1.2  
**Status:** Companion to SRS v1.2  
**Date:** 7 September 2026

Open `Vestigium.Helpers.slnx` → `src/Vestigium.Helpers.Analytics/`.

The binding contract is `_Documentation/Requirements_v1.0.md` (document version **1.2** inside that file). This page is how to call it.

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
```

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
    // point.X = ms, point.Y = fraction finished — bind in a host / future Charts helper
    _ = point;
}
```

`From(double[])` stays legal. Time is never required.

## Do not

- Add OxyPlot / ScottPlot / LiveCharts to this project.
- Treat P95 as 95 % confidence.
- Call `Initialize` on `Vestigium.Logging` from this library.
- Bin the value histogram by clock time.

## Files

See SRS §15. Tests: `src/Vestigium.Helpers.Tests/NumericSeriesTests.cs`.
