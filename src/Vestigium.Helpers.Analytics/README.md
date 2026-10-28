# Vestigium.Helpers.Analytics

Descriptive statistics, quartile bands, confidence intervals, Shewhart control limits, capability, and run rules for a finite numeric series.

It does not draw. It does not persist. Charts paints; the host owns storage.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Analytics` 1.0.1 |
| TFM | `net10.0` |
| APPID | `Analytics` (`AnalyticsCatalog.AppId`) |
| EVENTID | Reserved 10500–10999 (used through 10615) |
| Depends on | `MathNet.Numerics` 5.0.0, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />
```

```csharp
using Vestigium.Helpers.Analytics;

var series = NumericSeries.From(rtts, "rtt-ms");
var p95    = series.Full.Percentile(0.95);   // rank cut — not γ
var ci     = series.Confidence(0.95);
var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
var cap    = series.Capability(SpecLimits.From(0, 30));
var rules  = series.RunRules();
```

Pass `limits`, `rules`, and `SpecLimits` to `Vestigium.Helpers.Charts`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `NumericSeries.From<T>(values, name?)` | `NumericSeries` | Any `INumber<T>` → finite decimal. Encounter order kept. |
| `FromDecimal` / ctor | `NumericSeries` | Same, decimals only. |
| `FromObservations(...)` | `NumericSeries` | Optional UTC window `[start, end)`. |
| `series.Full` / `Q1`–`Q4` / `Iqr` | `SeriesSlice` | Empty bands exist and have count 0. |
| `slice.Percentile(p)` | `decimal` | Rank cut. Not a confidence level. |
| `series.Confidence(γ)` | `ConfidenceReport` | Mean / median / variance / σ. Default γ = 0.95. |
| `series.ControlLimits(method, k = 3)` | `ControlLimits` | Prefer `TryControlLimits` when walking `Bands`. |
| `series.Capability(spec)` | `ProcessCapability` | Spec is caller-supplied. Cp/Cpk on Full only. |
| `series.RunRules()` | run-rule hits | Inputs to Charts. |
| `series.Slice(start, end)` | `NumericSeries` | Requires timestamps. Empty slice throws. |
| `AnalyticsCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Finite values only. Empty or non-finite input is rejected.
- Time is optional. Slice and inferred windows require timestamps.
- Percentile `p` is a rank cut. Confidence `γ` is coverage. Do not swap them.
- Spec fences (LSL/USL) are not control fences (CL/UCL/LCL).
- `ControlLimitMethod.CallerSupplied` is not computed here.
- UCL / CL / LCL are numbers. This library does not draw them.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Analytics
    cfg.LogDirectory = logDir;
    AnalyticsCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`. Correlation id is `SeriesId`.

Named events live in `EventCatalog/analytics.json`.

## Related

Paint: `Vestigium.Helpers.Charts`. Disk log: `Vestigium.Logging`.

Long-form documents live in [Vestigium.Documentation / Helpers / Analytics](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Analytics/004%20--%20Developers%20Guide) |
