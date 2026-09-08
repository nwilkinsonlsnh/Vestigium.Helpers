# Vestigium.Helpers.Charts — Requirements Specification

**Document ID:** VEST-HLP-CHARTS-SRS-000  
**Version:** 1.1  
**Status:** Accepted — implementation follows this document  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Charts`  
**Engine:** ScottPlot 5 (`ScottPlot.WPF` 5.1.x)  
**TFM:** `net10.0-windows`  
**Companion:** `DevelopersGuide_v1.0.md` (design + how to call)

---

## 1. Purpose

Easy WPF wrapper over ScottPlot. Analytics (or the host) supplies the numbers. Charts draws them.

```csharp
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
panel.Children.Add(ChartView.Control(series, limits));
panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.FiveNumber));
panel.Children.Add(ChartView.Histogram(ChartSamples.RightTail(), showBellCurve: true));
```

Excel native charts stay in ClosedXml. This package must not reference ClosedXML.

---

## 2. Binding rules

| ID | Rule |
|---|---|
| C1 | ScottPlot types are not public. Public return type of `ChartView.*` is `FrameworkElement`. |
| C2 | `SavePng(ChartSpec, path)` is the headless path (tests, report packs). Linux testhost uses this; ChartView tests compile only on Windows. |
| C3 | UCL / CL / LCL are **inputs** (`ControlLimits` from Analytics). No overload invents mean ± kσ or MR̄. |
| C4 | `HelperLog` APPID `Charts`. Library never calls `Initialize`. |
| C5 | One Vestigium palette. |
| C6 | Layout-only overlays Charts *may* compute: OLS trend through plotted points, normal PDF sampled for the bell (μ ± 3.5s so tails show), Pareto sort-and-accumulate for display. Density math that hosts will reuse belongs in Analytics later (`PdfPoints`). |
| C7 | Analytics.Demo and ClosedXml.Demo host `ChartView`. ClosedXml the **library** does not reference Charts. |

---

## 3. v1 kinds (shipped)

Histogram (+ optional bell overlay), ECDF, Line, Scatter, Column, Bar, Pie, Pareto, Box (five-number or Tukey), Bands, MeanInterval, Control.

`ChartView.Box` defaults to the five-number summary (min / Q1 / median / Q3 / max) with those labels on the plot. `BoxWhiskerKind.Tukey` stops the whiskers at the last in-fence point and plots outliers.

`ChartSamples.Symmetric` / `RightTail` / `LeftTail` are seeded demo series for the gallery. Symmetric is N(12, 1.15). Right tail is log-normal. Left tail is that sample mirrored about 40.

`ChartView.Control(series, limits)` is required. No overload invents fences. Horizontal CL / UCL / LCL, running sample, points outside the given band marked.

---

## 4. Tests

Identity, Probe JSONL, SavePng each kind, Control null/malformed rejects, pie collapse to Other, OLS slope ≈ 1 on `{1..5}`, public types do not name ScottPlot, five-number whiskers are min/max (Tukey stops in-fence), ChartSamples skew signs.

ChartView tests are Compile-removed on Linux (`UseWPF`). Windows CI must run them.

---

## 5. Non-goals (this version)

- Computing UCL / CL / LCL
- Excel OOXML charts
- Replacing ScottPlot with a drawing engine we own
- Public ScottPlot types
- A non-Windows WPF host (SavePng is the headless path)
- Live data binding / streaming points (roadmap)

---

## 6. Roadmap

### 6.1 Shipped

v1 kinds, five-number / Tukey box, ChartSamples, SavePng, Control from caller limits, Analytics.Demo and ClosedXml.Demo as ChartView hosts.

### 6.2 Next

| Version | Item | Why |
|---|---|---|
| **v1.2** | **`Refresh(ChartSpec)`** on an existing host element | Hosts redraw a running sample today by replacing the `FrameworkElement`. Keep the WpfPlot and refill. |
| **v1.2** | **Dark palette** matching the navy gallery chrome | Still one Vestigium palette, two modes. Default stays light (print / Excel-adjacent). |
| **v1.3** | **Multi-series overlay** on Line / Scatter | Two `NumericSeries` on one plot (before / after a route change). |
| **v1.3** | **`SaveSvg`** | Report packs that are not PNG. |
| **v1.4** | **Run-rule markers** | When Analytics publishes Nelson / Western Electric indexes, paint them. Do not compute the rules here. |

### 6.3 Never here

Mean ± kσ, MR̄, Excel charts, a homegrown canvas, ClosedXML, web/WASM control, public `ScottPlot.Plot`.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 8 Sep 2026 | Wrapper over ScottPlot. Control from Analytics limits. |
| 1.1 | 8 Sep 2026 | Five-number box, Tukey option, ChartSamples, demo hosts. Future roadmap: Refresh, dark palette, multi-series, SaveSvg, run-rule markers. |
