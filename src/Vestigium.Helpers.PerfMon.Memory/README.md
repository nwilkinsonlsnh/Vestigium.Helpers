# Vestigium.Helpers.PerfMon.Memory

Commit, available, and machine memory samples.

Not a CLI. Not `perfmon.exe`. Not a plot package.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.PerfMon.Memory` 0.1.0 |
| TFM | `net10.0-windows` |
| APPID | `PerfMon.Memory` |
| EVENTID | Reserved 19000–19499 |
| Depends on | `Vestigium.Helpers.PerfMon`, Vestigium.Logging |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/Memory/002%20--%20Requirements%20Document) |

## Rules that do not move

- The library never calls `VestigiumLogger.Initialize`.
- Missing counter categories are `Unavailable`, not a fake zero.
- No Demo project.

Long-form documents live in [Vestigium.Documentation / Helpers / PerfMon / Memory](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/PerfMon/Memory).
