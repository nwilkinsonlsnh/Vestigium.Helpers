# Vestigium.Helpers

[![build](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml/badge.svg)](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml)

Cross-cutting helper libraries for the Vestigium suite (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

**Target:** .NET 10 LTS / Visual Studio 2026  
**Shape:** class libraries + one WPF gallery per library (same chrome as Vestigium.Logging)  
**Windows-only projects:** `Vestigium.Helpers.WinReg`, `Vestigium.Helpers.Charts`  
**Logging:** [Vestigium.Logging](https://github.com/nwilkinsonlsnh/Vestigium.Logging) JSON Lines (sibling repo, solution folder `/Logging/`)

Umbrella requirements: [`_Documentation/Requirements_v1.0.md`](_Documentation/Requirements_v1.0.md)  
Umbrella developer notes: [`_Documentation/DevelopersGuide_v1.0.md`](_Documentation/DevelopersGuide_v1.0.md)

## Logging

Libraries never call `VestigiumLogger.Initialize`. They call `HelperLog.*`, which is a no-op until a host initializes.

Enter/argument lines (`HelperLog.Begin` / `Enter`) are compiled into every build. They are **Debug**. Vestigium.Logging writes Debug to the in-memory ring always; disk only if the host sets `MinimumDiskLevel = Debug`. Helpers galleries do that. A quiet product host leaves the default (`Information`) and only gets Success / Failed / Error on disk.

`HelperGuard` logs Error / Failed, then throws. `WorkbookSession.SessionId` and `NumericSeries.SeriesId` stitch the chain.

Each WPF gallery is a host. It initializes with that helper's APPID. JSONL lands at:

```
%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json
```

Examples:

```
C:\ProgramData\Vestigium\Logs\ClosedXml\vestigium-ClosedXml-20260907.json
C:\ProgramData\Vestigium\Logs\Encryption\vestigium-Encryption-20260907.json
```

Clone [Vestigium.Logging](https://github.com/nwilkinsonlsnh/Vestigium.Logging) as a **sibling** of this repo. The solution already references it:

```
Vestigium.Helpers/
Vestigium.Logging/
```

`Vestigium.Helpers.slnx` loads `../Vestigium.Logging/src/Vestigium.Logging/Vestigium.Logging.csproj` under the **Logging** folder. The padlock in Solution Explorer is expected — the project lives outside this repo.

CI checks both repositories out as siblings so the same slnx path restores.

## Libraries

| Project | TFM | Role |
|---|---|---|
| `Vestigium.Helpers` | `net10.0` | Guards, `HelperLog`, `HelperWpfHost` |
| `Vestigium.Helpers.ClosedXml` | `net10.0` | ClosedXML write-first Excel (`.xlsx`) |
| `Vestigium.Helpers.Csv` | `net10.0` | RFC 4180 CSV / TSV (settable delimiter; not ClosedXml) |
| `Vestigium.Helpers.Encryption` | `net10.0` | Hashing and encryption helpers |
| `Vestigium.Helpers.WinReg` | `net10.0-windows` | Windows Registry helpers |
| `Vestigium.Helpers.Json` | `net10.0` | System.Text.Json helpers |
| `Vestigium.Helpers.Xml` | `net10.0` | XML document helpers |
| `Vestigium.Helpers.FileIo` | `net10.0` | File and directory helpers |
| `Vestigium.Helpers.Processes` | `net10.0` | Process launch and capture |
| `Vestigium.Helpers.Services` | `net10.0` | Service control helpers |
| `Vestigium.Helpers.Analytics` | `net10.0` | NumericSeries: five-number, bands, P95, intervals, **ControlLimits** |
| `Vestigium.Helpers.Charts` | `net10.0-windows` | ScottPlot wrapper. Draws Analytics numbers on a WPF form (histogram, five-number box, control). Does not compute UCL/LCL. Analytics.Demo and ClosedXml.Demo host it. |
| `Vestigium.Helpers.Network` | `net10.0` | HTTP / socket helpers |
| `Vestigium.Helpers.Tests` | `net10.0-windows` | xUnit (logger collection is serial; ChartView tests run on Windows) |

Each library has a matching `*.Demo` WPF gallery under the **Demo** solution folder. Shared chrome lives in `Vestigium.Helpers.Gallery`. Analytics, ClosedXml, Charts, and Csv are full galleries; the rest are Probe + JSONL skeletons until their SRS is accepted.

## Open in Visual Studio

1. Clone this repository **and** `Vestigium.Logging` next to it.
2. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
3. Restore NuGet.
4. Set any `*.Demo` project as startup, F5. Analytics, ClosedXml, Charts, and Csv open a gallery; the others open the shared skeleton. Then open `%ProgramData%\Vestigium\Logs\{APPID}\`.
5. Run `Vestigium.Helpers.Tests` for the contract.

```
dotnet run --project src/Vestigium.Helpers.ClosedXml.Demo
dotnet run --project src/Vestigium.Helpers.Analytics.Demo
dotnet run --project src/Vestigium.Helpers.Charts.Demo
dotnet run --project src/Vestigium.Helpers.Csv.Demo
dotnet test src/Vestigium.Helpers.Tests
```

## Contracts that do not move

- `.slnx` (not legacy `.sln`).
- Helpers never call `Initialize`. WPF galleries and application hosts do.
- Disk format is JSON Lines from Vestigium.Logging. No `.log` / CSV path.
- `WinReg` stays on `net10.0-windows`.
- Tests must not hit live ProgramData; they pass a temp `LogDirectory`.
- `Vestigium.Logging` stays a sibling repo. Do not vendor its source into Helpers.
