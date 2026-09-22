# Vestigium.Helpers.Charts — Requirements Specification

**Document ID:** VEST-HLP-CHARTS-SRS-000  
**Version:** 2.0  
**Status:** Accepted — single binding contract  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Charts`  
**Engine:** ScottPlot 5 (`ScottPlot.WPF` 5.1.x)  
**TFM:** `net10.0-windows`  
**Companions:** `Design_v2.0.md`, `DevelopersGuide_v2.0.md`  
**Predecessor:** SRS v1.1 + closed PR05

This page is complete. It does not defer to a prior blob.

---

## 1. Purpose

Easy WPF wrapper over ScottPlot. Analytics (or the host) supplies the numbers. Charts draws them.

```csharp
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
panel.Children.Add(ChartView.Control(series, limits, series.RunRules()));
panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.FiveNumber));
panel.Children.Add(ChartView.Histogram(series, new ChartOptions { ShowBellCurve = true, ShowKde = true }));
```

Excel native charts stay in ClosedXml. This package must not reference ClosedXML.

---

## 2. Binding rules

| ID | Rule |
|---|---|
| C1 | ScottPlot types are not public. Public return type of `ChartView.*` (except `SavePng`) is `FrameworkElement`. |
| C2 | `SavePng(ChartSpec, path)` is the headless path (tests, report packs). Linux testhost uses this; ChartView host tests compile only on Windows. |
| C3 | UCL / CL / LCL are **inputs** (`ControlLimits` from Analytics). No public overload invents mean ± kσ or MR̄. |
| C4 | Vestigium.Logging APPID `Charts`. Library never calls `Initialize`. |
| C5 | One Vestigium palette. |
| C6 | Layout-only overlays Charts *may* compute: OLS trend through plotted points, parametric N(μ, s) bell sampled for display (μ ± 3.5s), Pareto sort-and-accumulate for display. Density, percentile bounds, run rules, and spec scoring belong in Analytics. |
| C7 | Analytics.Demo and ClosedXml.Demo may host `ChartView`. ClosedXml the **library** does not reference Charts. |
| C8 | Process fences and specification fences are different lines. Charts must not paint LSL as LCL. |

`ChartHelper.Probe` may call `series.ControlLimits()` as suite smoke. That is not a public Control door.

---

## 3. Kinds

| Kind | Source | Notes |
|---|---|---|
| Column, Bar | series or `ChartSeries` | |
| Line, Scatter | series, XY lists, or timed series | Optional OLS trend. Optional `ControlLimits` overlay. |
| Pie | series frequencies or `ChartSlice` | Positive slices only. More than 12 collapse to top 11 + Other. |
| Histogram | `NumericSeries` | FD bins from Analytics. Optional `ShowBellCurve`, `ShowKde`. |
| Ecdf | series | Y in [0, 1]. |
| Pareto | series or slices | Optional cumulative line on the right axis. |
| Box | series | Default five-number (min / Q1 / median / Q3 / max). `Tukey` stops in-fence and plots outliers. |
| Bands | series | Mean of Full, Q1–Q4, IQR. |
| MeanInterval | series | Student-t mean fence at `IntervalLevel` (default γ = 0.95). |
| Control | series or Y list + **required** `ControlLimits` | Running sample, outside-fence marks, optional run-rule marks, optional LSL/USL. |
| PercentileInterval | series | Point = `Percentile(p)`. Whiskers = Analytics `PercentileInterval`. |

Unknown `ChartKind` → `ArgumentOutOfRangeException`.

---

## 4. Inputs Charts must honor

On `ChartOptions` / `ChartSpec`:

| Field | Effect |
|---|---|
| Title, XLabel, YLabel | Labels |
| ShowLegend, ShowGrid | Chrome |
| Color | Series fill/stroke; empty falls back to palette primary |
| Width, Height | Host size; default height 240 |
| ShowBellCurve | Parametric N(μ, s) on histogram, count-scaled |
| ShowKde | Analytics `PdfPoints`, count-scaled (`n × binWidth`). Empty KDE → no overlay, no throw. Both overlays may be on. |
| Trend | `None` or `Linear` |
| ShowParetoLine | Default true |
| BoxWhisker | FiveNumber or Tukey |
| Limits | Process fences on Control / Line / Scatter |
| IntervalLevel | γ for MeanInterval and PercentileInterval |
| PercentileP | p for PercentileInterval (default 0.95) |
| RunRules | Host `RunRuleReport`. Charts maps `AllIndexes` onto plotted points. Does not compute rules. |
| Spec | Host `SpecLimits`. Charts draws LSL/USL only. Does not score outside-spec. |

`ChartSpec.RunRules` / `ChartSpec.Spec` win over the same fields on `Options` when both are set.

Control rejects null limits and malformed bands (UCL ≤ CL or CL ≤ LCL) with `ArgumentException`. Empty pie/Pareto (no positive slice) → `ArgumentException`. Blank `SavePng` path → `ArgumentException`. X/Y length mismatch → `ArgumentException`.

MeanInterval throws `InvalidOperationException` when the mean interval is undefined.

---

## 5. Palette

One light Vestigium palette (print / Excel-adjacent default):

| Token | Use |
|---|---|
| Primary `#4C6B8A` | Series |
| Secondary / Kde / Rule `#C47B4A` | KDE, run-rule marks |
| Trend `#2F4F4F` | OLS, interval whiskers |
| Bell `#8B3A3A` | Parametric bell |
| Outlier `#A33B3B` | Outside UCL/LCL, Tukey outliers |
| Cl `#2F4F4F` | Center line |
| Ucl `#A33B3B` | Upper control |
| Lcl `#3B6EA3` | Lower control |
| Lsl `#5B8C5A` | Spec lower |
| Usl `#8C5A7A` | Spec upper |
| Grid `#D9DEE4` | Grid |

Dark mode is roadmap, not this version.

---

## 6. Logging

Never `Initialize`. Writes no-op when Logging is down; drawing still runs. APPID `Charts`. EventIds 16500+. Shard `EventCatalog/charts.json`. Boundary Debug enter / Information built or saved / Error then throw. Do not treat HelperLog as the Charts door inside the library (tests may use the suite harness).

---

## 7. Samples

`ChartSamples.Symmetric` / `RightTail` / `LeftTail` are seeded gallery series (`DefaultSeed = 20260908`). Symmetric is N(12, 1.15). Right tail is log-normal-like. Left tail mirrors that sample about 40. Not a process model. Not Analytics fixtures. `n < 2` throws.

---

## 8. Tests

Identity, Probe JSONL, SavePng each shipped kind, Control null/malformed rejects, pie collapse to Other, OLS slope ≈ 1 on `{1..5}`, public types do not name ScottPlot, five-number whiskers are min/max (Tukey stops in-fence), ChartSamples skew signs, unknown kind / blank path / missing source rejects.

ChartView STA host tests are Windows. `SavePng` is the Linux path. PR05 Analytics fixtures pin γ, KDE finiteness, and percentile-interval existence; Charts paints those numbers.

---

## 9. Packaging

| Item | Value |
|---|---|
| TFM | `net10.0-windows` (`UseWPF`) |
| Identity | `Vestigium.Helpers.Charts` |
| Engine | ScottPlot.WPF 5.x |
| References | Analytics, Logging. Not ClosedXML. |
| InternalsVisibleTo | `Vestigium.Helpers.Tests` |

On non-Windows, `ChartView.From` / kind doors return a `TextBlock` telling the caller to use `SavePng`. `SavePng` still builds a ScottPlot `Plot`.

---

## 10. Non-goals

- Computing UCL / CL / LCL, run rules, spec scores, KDE bandwidth, or percentile bounds
- Excel OOXML charts
- Replacing ScottPlot with an engine we own
- Public ScottPlot types
- A non-Windows WPF host
- Live data binding / streaming points
- Host / PingIQ adapters

---

## 11. Roadmap (not this version)

| Item | Why later |
|---|---|
| `Refresh(ChartSpec)` on an existing host element | Keep the WpfPlot; refill. |
| Dark palette | Same tokens, second mode. Default stays light. |
| Multi-series Line / Scatter | Two `NumericSeries` on one plot. |
| `SaveSvg` | Report packs that are not PNG. |

Run-rule markers, spec lines, KDE overlay, and percentile-interval kind **shipped in PR05**. They are not roadmap.

---

## 12. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 8 Sep 2026 | Wrapper over ScottPlot. Control from Analytics limits. |
| 1.1 | 8 Sep 2026 | Five-number box, Tukey, ChartSamples, demo hosts. |
| 2.0 | 19 Sep 2026 | Lossless contract: v1.1 + PR05 (γ, run rules, spec lines, KDE, percentile interval). |
