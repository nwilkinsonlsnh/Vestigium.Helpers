# Vestigium.Helpers.Charts — Requirements Specification

**Document ID:** VEST-HLP-CHARTS-SRS-000  
**Version:** 1.0  
**Status:** Accepted — implementation follows this document  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Charts`  
**Engine:** ScottPlot 5 (`ScottPlot.WPF` 5.1.x)  
**TFM:** `net10.0-windows`

Companion: `DevelopersGuide_v1.0.md`

## Purpose

Easy WPF wrapper over ScottPlot. Analytics (or the host) supplies the numbers. Charts draws them.

```csharp
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
panel.Children.Add(ChartView.Control(series, limits));
```

Excel native charts stay in ClosedXml. This package must not reference ClosedXML.

## Binding rules

- ScottPlot types are not public.
- Return type of `ChartView.*` is `FrameworkElement`.
- `SavePng(ChartSpec, path)` is the headless path (tests, report packs).
- UCL / CL / LCL are **inputs** (`ControlLimits` from Analytics). Charts never computes mean ± kσ or MR̄.
- `HelperLog` APPID `Charts`. Library never calls `Initialize`.
- One Vestigium palette.

## v1 kinds

Histogram (+ optional bell overlay, sampled to μ ± 3.5s so the tails show), ECDF, Line, Scatter, Column, Bar, Pie, Pareto, Box (five-number or Tukey), Bands, MeanInterval, Control.

`ChartView.Box` defaults to the five-number summary (min / Q1 / median / Q3 / max) with those labels on the plot. `BoxWhiskerKind.Tukey` stops the whiskers at the last in-fence point and plots outliers.

`ChartSamples.Symmetric` / `RightTail` / `LeftTail` are seeded demo series for the gallery. Overlays Charts *may* compute for layout only: OLS trend through plotted points, normal PDF sampled for the bell, Pareto sort-and-accumulate for display.

## Control widget

`ChartView.Control(series, limits)` is required. No overload invents fences. Horizontal CL / UCL / LCL, running sample, points outside the given band marked.

## Tests

Identity, Probe JSONL, SavePng each kind, Control null/malformed rejects, pie collapse to Other, OLS slope ≈ 1 on `{1..5}`, public types do not name ScottPlot, five-number whiskers are min/max (Tukey stops in-fence), ChartSamples skew signs.
