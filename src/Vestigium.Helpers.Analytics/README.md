# Vestigium.Helpers.Analytics

Descriptive statistics, quartile bands, confidence intervals, and Shewhart control limits for a finite numeric series. It does not draw and it does not persist.

**Target:** .NET 10 (`net10.0`)  
**Contract:** `_Documentation/Requirements_v2.0.md` in the [source repo](https://github.com/nwilkinsonlsnh/Vestigium.Helpers)

```xml
<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />
```

```csharp
using Vestigium.Helpers.Analytics;

var series = NumericSeries.From(rtts, "rtt-ms");
var p95 = series.Full.Percentile(0.95);                 // rank cut — not γ
var ci = series.Confidence(0.95);
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
var cap = series.Capability(SpecLimits.From(0, 30));
var rules = series.RunRules();
```

Pass `limits`, `rules`, and `SpecLimits` to `Vestigium.Helpers.Charts`. Charts only paints.

Dependencies pulled for you: `MathNet.Numerics`, `Vestigium.Logging`. The library never calls `VestigiumLogger.Initialize`. Hosts that want a disk trace initialize Logging and call `AnalyticsCatalog.Register`.
