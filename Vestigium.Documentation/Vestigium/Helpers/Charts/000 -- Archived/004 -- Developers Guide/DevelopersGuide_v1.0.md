# Vestigium.Helpers.Charts — Developers Guide

**Document ID:** VEST-HLP-CHARTS-DEV-000  
**Version:** 1.1  
**Status:** Design companion to SRS v1.1  
**Date:** 8 September 2026

`ChartView` wraps ScottPlot. `NumericSeries.ControlLimits()` lives in Analytics.

## Design

**Intent.** Put Analytics numbers on a WPF form without every host learning ScottPlot. The public type is `FrameworkElement`. ScottPlot stays internal.

**Locked decisions.**

| Decision | Why |
|---|---|
| Wrapper, not an engine | We do not own a plot renderer. ScottPlot 5 draws; we bind. |
| UCL / CL / LCL are inputs | A lone spike on small n sits inside 3σ because s inflates. Moving range lives in Analytics so Charts cannot “fix” the band. |
| Five-number box is the default | Operators asked for min / Q1 / median / Q3 / max, labeled. Tukey is the outlier view. |
| `ChartSamples` live here | Seeded bells for the gallery. Not a process model. Not Analytics test fixtures. |
| Excel charts stay in ClosedXml | Native OOXML on save. This package must not reference ClosedXML. |
| `net10.0-windows` | `ScottPlot.WPF`. SavePng is the headless path. Tests Compile-remove ChartView on Linux. |

**Host pattern.**

```
Analytics.NumericSeries + ControlLimits
        → ChartView.*(series, limits?) → FrameworkElement
Analytics.Demo / ClosedXml.Demo / Charts.Demo drop that element on a form.
```

ClosedXml the library still writes Excel charts itself. Its **demo** hosts ChartView as a preview of the same series.

## Call

```csharp
var series = NumericSeries.From(rtts, "rtt-ms");
var limits = series.ControlLimits();                         // mean ± 3s
var mr = series.ControlLimits(ControlLimitMethod.MovingRange);
panel.Children.Add(ChartView.Histogram(series, showBellCurve: true));
panel.Children.Add(ChartView.Control(series, limits));
panel.Children.Add(ChartView.Control(series, mr));
panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.FiveNumber));
panel.Children.Add(ChartView.Pareto(series));
panel.Children.Add(ChartView.Line(series, TrendKind.Linear));
panel.Children.Add(ChartView.Histogram(ChartSamples.RightTail(), showBellCurve: true));
ChartView.SavePng(new ChartSpec { Kind = ChartKind.Ecdf, Source = series }, path);
```

Gallery: `dotnet run --project src/Vestigium.Helpers.Charts.Demo` (Windows). APPID `Charts`. JSONL under `%ProgramData%\Vestigium\Logs\Charts\`. Tabs: Overview, Histogram, Shapes, Control, Pareto, Library, JSONL.

## Roadmap

Shipped: v1 kinds, five-number / Tukey box, ChartSamples, demo hosts.

Next (SRS §6):

1. **Refresh in place** — refill the WpfPlot without tearing down the control.
2. **Dark palette** matching the navy gallery.
3. **Multi-series overlay** on Line / Scatter.
4. **SaveSvg**.
5. **Run-rule markers** once Analytics publishes them.

Never: compute UCL/LCL, Excel OOXML, public ScottPlot types.

## Do not

Do not add ScottPlot types to public signatures. Do not compute UCL/LCL here.
