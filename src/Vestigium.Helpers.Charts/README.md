# Vestigium.Helpers.Charts

ScottPlot wrapper that puts Analytics numbers on a WPF form. Analytics (or the host) supplies the numbers. Charts draws them.

It does not compute UCL / CL / LCL, run rules, spec fences, or KDE points. ScottPlot types are not public.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Charts` 1.0.1 |
| TFM | `net10.0-windows` (WPF) |
| APPID | `Charts` (`ChartsCatalog.AppId`) |
| EVENTID | Reserved 16500–16999 (used through 16545) |
| Depends on | `Vestigium.Helpers.Analytics`, `ScottPlot.WPF` 5.1.59, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Charts" Version="1.0.1" />
```

```csharp
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

var series = NumericSeries.From(rtts, "rtt-ms");
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
var rules  = series.RunRules();

panel.Children.Add(ChartView.Control(series, limits, rules));
panel.Children.Add(ChartView.Histogram(series, new ChartOptions
{
    ShowBellCurve = true,
    ShowKde = true
}));
```

Headless path: `ChartView.SavePng(spec, path)`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `ChartView.Control(series, limits, rules?)` | `FrameworkElement` | Requires UCL > CL > LCL. Does not invent fences. |
| `ChartView.Histogram(series, options?)` | `FrameworkElement` | Optional bell curve / KDE via `ChartOptions`. |
| `ChartView.Ecdf` / `Line` / `Scatter` | `FrameworkElement` | Scatter also accepts raw x/y of equal length. |
| `ChartView.Column` / `Bar` / `Pie` / `Pareto` | `FrameworkElement` | Pie/Pareto accept `ChartSlice` lists. |
| `ChartView.Box` / `Bands` | `FrameworkElement` | Whisker kind is an option, not a calculation change in Charts. |
| `ChartView.MeanInterval(series, γ)` | `FrameworkElement` | Calls Analytics `Confidence` then paints. |
| `ChartView.From(spec)` | `FrameworkElement` | Generic door. |
| `ChartView.SavePng(spec, path, w, h)` | void | Blank path rejected. Creates parent directories. |
| `ChartsCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Charts paints. Analytics computes.
- ScottPlot types stay internal. Hosts receive `FrameworkElement`.
- Empty series and x/y length mismatch are rejected.
- Control charts require well-ordered fences: UCL > CL > LCL.
- On a non-Windows host, `From` returns a text stand-in. Use `SavePng`.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Charts
    cfg.LogDirectory = logDir;
    AnalyticsCatalog.Register(cfg);
    ChartsCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`. Correlation id is the source `SeriesId` when present.

Named events live in `EventCatalog/charts.json`.

## Related

Numbers: `Vestigium.Helpers.Analytics`. Disk log: `Vestigium.Logging`.

Long-form documents live in [Vestigium.Documentation / Helpers / Charts](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Charts/004%20--Developers%20Guide) |
