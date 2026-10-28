# Vestigium.Helpers.PerfMon.Gpu

GPU engine and adapter memory the OS exposes.

Not a CLI. Not `perfmon.exe`. Not a plot package.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.PerfMon.Gpu` 0.1.0 |
| TFM | `net10.0-windows` |
| APPID | `PerfMon.Gpu` |
| EVENTID | Reserved 18500–18999 |
| Depends on | `Vestigium.Helpers.PerfMon`, Vestigium.Logging |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/Gpu/002%20--%20Requirements%20Document) |

## Rules that do not move

- The library never calls `VestigiumLogger.Initialize`.
- Missing counter categories are `Unavailable`, not a fake zero.
- No Demo project.

Long-form documents live in [Vestigium.Documentation / Helpers / PerfMon / Gpu](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/Gpu).
