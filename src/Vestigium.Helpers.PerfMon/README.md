# Vestigium.Helpers.PerfMon

Shared performance sample contract, job runner, and counter source.

Not a CLI. Not `perfmon.exe`. Not a plot package.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.PerfMon` 0.1.3 |
| TFM | `net10.0-windows` |
| APPID | `PerfMon` |
| EVENTID | Reserved 17000–17499 |
| Depends on | Vestigium.Logging |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/PerfMon/002%20--%20Requirements%20Document) |

## Rules that do not move

- The library never calls `VestigiumLogger.Initialize`.
- Missing counter categories are `Unavailable`, not a fake zero.
- The first read of a rate counter is Unavailable. It is the prime, not a sample.
- No Demo project.
- The one public source is `CachedPdhSource`. Hosts do not subclass it. `SampleJob` with no `Source` constructs it and disposes it. A host-passed source is not disposed. `PerformanceCounterSource` stays internal.
- `ListInstances` uses `PdhCounterInventory.Shared`. It does not walk PDH a second time.

Long-form documents live in [Vestigium.Documentation / Helpers / PerfMon / PerfMon](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/PerfMon).
