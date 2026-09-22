# Vestigium.Helpers.Charts — Design

**Document ID:** VEST-HLP-CHARTS-DSN-000  
**Version:** 2.0  
**Status:** Locked companion to SRS v2.0  
**Date:** 19 September 2026  
**Binding:** `Requirements_v2.0.md` wins on conflict

This page records *why* Charts is shaped this way. It does not add requirements.

---

## 1. Intent

Put Analytics numbers on a WPF form without every host learning ScottPlot. The public type is `FrameworkElement`. ScottPlot stays internal.

```
Analytics.NumericSeries
    + ControlLimits / RunRuleReport / SpecLimits / Confidence / PdfPoints / PercentileInterval
        → ChartView.*(series, inputs) → FrameworkElement
Analytics.Demo / ClosedXml.Demo / Charts.Demo drop that element on a form.
```

ClosedXml the library still writes Excel charts itself. Its **demo** may host ChartView as a preview of the same series.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Wrapper, not an engine | We do not own a plot renderer. ScottPlot 5 draws; we bind. |
| UCL / CL / LCL are inputs | A lone spike on small n sits inside 3σ because s inflates. Moving range lives in Analytics so Charts cannot “fix” the band. |
| Spec lines are inputs | LSL/USL are customer fences. Painting them as LCL would lie. |
| Run-rule marks are inputs | Indexes come from `RunRuleReport.AllIndexes`. Charts maps them onto plotted X/Y. |
| Five-number box is the default | Operators asked for min / Q1 / median / Q3 / max, labeled. Tukey is the outlier view. |
| `ChartSamples` live here | Seeded bells for the gallery. Not a process model. Not Analytics test fixtures. |
| Excel charts stay in ClosedXml | Native OOXML on save. This package must not reference ClosedXML. |
| `net10.0-windows` | `ScottPlot.WPF`. SavePng is the headless path. Tests Compile-remove ChartView host on Linux. |
| Layout-only math only | OLS, display bell, Pareto accumulate. KDE / percentile CI / capability stay in Analytics. |
| One light palette | Print and Excel-adjacent. Dark is a second mode later, not a second product. |
| Probe may compute limits | Suite smoke only. Public Control still requires caller fences. |

---

## 3. Shape

| File | Role |
|---|---|
| `ChartView.cs` | Public doors, SavePng, WPF host |
| `ChartView.Interval.cs` | PercentileInterval door |
| `ChartTypes.cs` | Kind, options, spec, palette, TrendFit |
| `PlotBuilder.cs` | Fill switch + most kinds |
| `PlotBuilder.Control.cs` | Control layer (fences, outside, run-rule marks, spec lines) |
| `PlotBuilder.Interval.cs` | PercentileInterval fill |
| `ChartControlOverlay.cs` | Map run-rule indexes → points |
| `ChartSpecOverlay.cs` | Map SpecLimits → LSL/USL lines |
| `ChartHelper.cs` | Identity + Probe |
| `ChartSamples.cs` | Seeded gallery series |
| `ChartsLog` / `ChartsCatalog` / `ChartsEvents` | Logging |

`ChartKind.PercentileInterval` is routed through `TryFillExtra` so the main switch stays closed over the original kinds.

---

## 4. What Charts computes vs paints

| Computes here | Paints from Analytics |
|---|---|
| OLS through plotted X/Y | ControlLimits, OutOfControlIndexes |
| Display bell N(μ, s) × n × binWidth | `PdfPoints` × n × binWidth |
| Pareto sort + cumulative share | Frequency / ParetoPoints |
| Pie Other collapse | Slice values |
| Box layout from slice fences | Q1/Q3/median/outliers |
| | Mean interval at caller γ |
| | PercentileInterval bounds |
| | RunRuleReport indexes |
| | SpecLimits Lower/Upper |

---

## 5. What closed to reach 2.0

PR05 (archived under `Archive/PR01/`):

- Honor `IntervalLevel` on MeanInterval
- Paint host run-rule indexes on Control
- Paint host LSL/USL
- Histogram `ShowKde`
- `ChartKind.PercentileInterval`

---

## 6. Still out

Refresh-in-place, dark palette, multi-series overlay, SaveSvg, host adapters, public `ScottPlot.Plot`, computing fences.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 2.0 | 19 Sep 2026 | First standalone Design. Content lifted from Guide v1.1 + PR05. |
