# Vestigium.Helpers.Charts — Developers Guide

`ChartView` wraps ScottPlot. `NumericSeries.ControlLimits()` lives in Analytics.

```csharp
var series = NumericSeries.From(rtts, "rtt-ms");
var limits = series.ControlLimits();                         // mean ± 3s
var mr = series.ControlLimits(ControlLimitMethod.MovingRange);
panel.Children.Add(ChartView.Histogram(series, showBellCurve: true));
panel.Children.Add(ChartView.Control(series, limits));
panel.Children.Add(ChartView.Control(series, mr));
panel.Children.Add(ChartView.Pareto(series));
panel.Children.Add(ChartView.Line(series, TrendKind.Linear));
ChartView.SavePng(new ChartSpec { Kind = ChartKind.Ecdf, Source = series }, path);
```

Gallery: `dotnet run --project src/Vestigium.Helpers.Charts.Demo` (Windows). APPID `Charts`. JSONL under `%ProgramData%\Vestigium\Logs\Charts\`.

Do not add ScottPlot types to public signatures. Do not compute UCL/LCL here.
