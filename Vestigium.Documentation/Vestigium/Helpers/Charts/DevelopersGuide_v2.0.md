# Vestigium.Helpers.Charts — Developers Guide

**Document ID:** VEST-HLP-CHARTS-DEV-000  
**Version:** 2.0  
**Status:** How-to companion to SRS v2.0  
**Date:** 19 September 2026  
**Binding:** `Requirements_v2.0.md`  
**Design:** `Design_v2.0.md`

Open `Vestigium.Helpers.slnx` → `src/Vestigium.Helpers.Charts/`.

---

## Logging (host)

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "Charts"; // or the host APPID — folder follows this
    ChartsCatalog.Register(cfg);
});
```

This library never calls `Initialize`.

---

## Call

```csharp
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

var series = NumericSeries.From(rtts, "rtt-ms");
var limits = series.ControlLimits();
var mr = series.ControlLimits(ControlLimitMethod.MovingRange);
var spec = SpecLimits.From(0, 30);
var rules = series.RunRules();

panel.Children.Add(ChartView.Histogram(series, new ChartOptions
{
    ShowBellCurve = true,
    ShowKde = true
}));
panel.Children.Add(ChartView.Control(series, limits, rules, new ChartOptions { Spec = spec }));
panel.Children.Add(ChartView.Control(series, mr));
panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.FiveNumber));
panel.Children.Add(ChartView.Box(series, BoxWhiskerKind.Tukey));
panel.Children.Add(ChartView.Pareto(series));
panel.Children.Add(ChartView.Line(series, TrendKind.Linear));
panel.Children.Add(ChartView.MeanInterval(series, 0.80));
panel.Children.Add(ChartView.PercentileInterval(series, p: 0.95, level: 0.95));
panel.Children.Add(ChartView.Histogram(ChartSamples.RightTail(), showBellCurve: true));

ChartView.SavePng(
    new ChartSpec { Kind = ChartKind.Ecdf, Source = series },
    path);
```

`Control(series)` without limits does not exist. Do not invent fences in the host “to make the spike show” — pass MovingRange or `FromCaller`.

---

## Demo

`dotnet run --project src/Vestigium.Helpers.Charts.Demo` (Windows). APPID `Charts`. JSONL under `%ProgramData%\Vestigium\Logs\` for the host APPID. Tabs: Overview, Histogram, Shapes, Control, Pareto, Library, JSONL.

---

## Tests

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Chart
```

Windows for STA `ChartView` host tests. `SavePng` runs headless.

---

## Do not

- Add ScottPlot types to public signatures.
- Compute UCL/LCL, run rules, spec scores, or KDE bandwidth here.
- Reference ClosedXML from this project.
- Paint LSL with the LCL token.
- Call `VestigiumLogger.Initialize` from this library.
- Put a PingIQ adapter in this project.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.1 | 8 Sep 2026 | Companion to SRS v1.1. |
| 2.0 | 19 Sep 2026 | Lossless how-to for SRS v2.0. Design moved out. PR05 doors added. |
