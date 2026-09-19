# Vestigium.Helpers.Charts — PR05 implementation plan

**Document ID:** VEST-HLP-CHARTS-PLAN-PR05  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Binding:** Charts `Requirements_v1.0.md` C1–C6  
**Predecessor:** Analytics PR03 / PR04 (closed)

Charts still only draws. Analytics still only computes. No UCL/MR/KDE formula lands in this project.

Do the rows in number order. Stop after PR05.007 unless we reopen multi-series / Refresh / dark palette.

## Steps

| Step | Priority | Recommendation | Why | Files |
|---|---|---|---|---|
| **PR05.001** | P0 | Honor `level` on `MeanInterval` | `ChartView.MeanInterval(series, level)` calls `Confidence(level)` then `FillMeanInterval` always draws 0.95. Pass γ on `ChartSpec` (or options) and use it. Default remains `ConfidenceLevel.DefaultValue`. | `ChartTypes.cs`, `ChartView.cs`, `PlotBuilder.cs` |
| **PR05.002** | P0 | Mean-interval fixture | Series `{1..9}` at 0.95 and at 0.80 produce different whisker heights. Null/undefined mean still throws. | Charts tests (`SavePng` or layout numbers) |
| **PR05.003** | P1 | Paint `RunRuleReport` on Control | Optional `ChartSpec.RunRules` / `ChartOptions.RunRules`. Markers from `Hits` / `AllIndexes`. Do not call `series.RunRules()` inside Charts unless the host omitted the report and passed a flag we refuse — host supplies the report. Distinct marker from fence-outside. | `ChartTypes.cs`, `PlotBuilder.FillControl`, `ChartView.Control` overload |
| **PR05.004** | P1 | Paint `SpecLimits` | Optional LSL/USL horizontal lines. Not CL/UCL. New palette tokens if needed. Host passes `SpecLimits`, already scored or not — Charts only reads `Lower`/`Upper`. | `ChartTypes.cs`, `PlotBuilder` |
| **PR05.005** | P1 | Histogram `ShowKde` | Overlay `series.PdfPoints()` scaled by `n × binWidth` so it sits on the same count axis as the bars. Keep `ShowBellCurve` as parametric N(μ, s). Both may be on. Empty KDE → no overlay, no throw. | `ChartOptions`, `PlotBuilder.FillHistogram` |
| **PR05.006** | P2 | `PercentileInterval` chart kind | Same shape as mean CI: point = `slice.Percentile(p)`, whiskers = `PercentileInterval` bounds. Kind `PercentileInterval`. Default p=0.95, γ=0.95. | `ChartKind`, `ChartView`, `PlotBuilder` |
| **PR05.007** | P2 | Fixtures + close | Control + run-rule indexes marked; spec lines present; KDE overlay finite; percentile kind SavePng. Mark this plan Closed. | tests, this file |

## Priority key

| Priority | Meaning |
|---|---|
| P0 | Drawn interval disagrees with the API. |
| P1 | Paint numbers Analytics already ships. Same release. |
| P2 | New kind + close. |

## Rules that stay true

- C3: no overload invents mean ± kσ or MR̄.
- Run rules and specs are inputs. If the host wants them on the plot, the host computes them.
- KDE comes from `KernelDensity.PdfPoints`. Charts only scales and strokes.
- `SpecLimits` is never treated as `ControlLimits`.
- ScottPlot types stay internal.

## Out of PR05

| Item | Why |
|---|---|
| `Refresh(ChartSpec)` / dark palette / `SaveSvg` | Charts roadmap v1.2–v1.3. Separate plan. |
| Multi-series Line/Scatter | Charts v1.3. Needs `Compare` only as a host recipe, not a Charts type. |
| Computing capability or run rules in Charts | Analytics. |
| Host / PingIQ | Different project. |

## Close rule

PR05 is closed when 001–006 are in source, 007 tests pass, and this document says Closed.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR05_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR05.001–007 from the Analytics/Charts alignment review. |
