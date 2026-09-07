# Vestigium.Helpers — Developers Guide

**Document ID:** VEST-HLP-DEV-000  
**Version:** 1.1  
**Status:** Active  
**Date:** 7 September 2026

## Open the solution

1. Clone `https://github.com/nwilkinsonlsnh/Vestigium.Helpers`.
2. Clone `https://github.com/nwilkinsonlsnh/Vestigium.Logging` next to it (sibling folder).
3. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
4. Set any `*.Demo` project as startup and F5. JSONL lands under `%ProgramData%\Vestigium\Logs\{APPID}\`.
5. Run `Vestigium.Helpers.Tests`. Tests use a temp `LogDirectory` and never write live ProgramData.

## Where things live

| Need | Place |
|---|---|
| Shared guards | `src/Vestigium.Helpers/HelperGuard.cs` |
| Logging façade | `src/Vestigium.Helpers/HelperLog.cs` |
| CLI host | `src/Vestigium.Helpers/HelperDemoHost.cs` |
| Excel wrapper | `src/Vestigium.Helpers.ClosedXml/WorkbookHelper.cs` |
| Registry wrapper | `src/Vestigium.Helpers.WinReg/RegistryHelper.cs` |
| Per-library CLI | `src/Vestigium.Helpers.*.Demo/` |
| Umbrella SRS | `_Documentation/Requirements_v1.0.md` |
| Per-library docs | `src/Vestigium.Helpers.*/_Documentation/` |

## Logging rules

- Libraries never call `VestigiumLogger.Initialize`. They call `HelperLog.*`, which is a no-op until a host initializes.
- Each CLI is a process. `HelperDemoHost.Run` initializes with that helper's APPID so the rolling file is `%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json`.
- Optional `appId` on `VestigiumLog.Write` stamps the JSON `APPID` field. The folder still follows the host's `VestigiumLoggerOptions.AppId`.
- Tests must pass a temp `LogDirectory`. The logger collection is serial (`DisableParallelization`).

## Conventions copied from the suite

- `.slnx` (not legacy `.sln`)
- `Directory.Build.props` sets nullable, implicit usings, latest C#
- MIT license, same copyright line as Logging / Controls / Themes
- GitHub Actions `windows-latest` + `dotnet-version: 10.0.x`
- CI checks Logging out into `_deps/Vestigium.Logging`

## Adding a helper later

1. New class library `src/Vestigium.Helpers.{Name}` targeting `net10.0` unless it is Windows-only.
2. `_Documentation/Requirements_v1.0.md` and `DevelopersGuide_v1.0.md` before implementation.
3. Add `HelperLog.AppIds.{Name}` and register it in `HelperLog.Taxonomy`.
4. Add `Probe()` that logs Pending then Success.
5. Add `src/Vestigium.Helpers.{Name}.Demo` and wire it through `HelperDemoHost.Run`.
6. Add the library under `/Library/` and the demo under `/Demo/` in `Vestigium.Helpers.slnx`.
7. Reference the library from Tests. Do not add it to the core `Vestigium.Helpers` project.
