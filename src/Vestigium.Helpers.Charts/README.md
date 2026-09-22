# Vestigium.Helpers.Charts

ScottPlot wrapper that puts Analytics numbers on a WPF form. Analytics (or the host) supplies the numbers. Charts draws them.

**Target:** .NET 10 Windows (`net10.0-windows`, WPF)  
**Contract:** `_Documentation/Requirements_v2.0.md` in the [source repo](https://github.com/nwilkinsonlsnh/Vestigium.Helpers)

```xml
<PackageReference Include="Vestigium.Helpers.Charts" Version="1.0.1" />
```

```csharp
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;

var series = NumericSeries.From(rtts, "rtt-ms");
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
var rules = series.RunRules();

panel.Children.Add(ChartView.Control(series, limits, rules));
panel.Children.Add(ChartView.Histogram(series, new ChartOptions
{
    ShowBellCurve = true,
    ShowKde = true
}));
```

UCL / CL / LCL, run rules, spec lines, and KDE points are inputs. This package does not invent fences. ScottPlot types are not public; `ChartView` returns `FrameworkElement`. Headless path: `ChartView.SavePng`.

Depends on `Vestigium.Helpers.Analytics` and `ScottPlot.WPF`. Never calls `VestigiumLogger.Initialize`. Hosts that want a disk trace initialize Logging and call `ChartsCatalog.Register`.
