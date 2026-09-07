# Vestigium.Helpers

[![build](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml/badge.svg)](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml)

Cross-cutting helper libraries for the Vestigium suite (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

**Target:** .NET 10 LTS / Visual Studio 2026  
**Shape:** class libraries + one CLI demo per library  
**Windows-only project:** `Vestigium.Helpers.WinReg`  
**Logging:** [Vestigium.Logging](https://github.com/nwilkinsonlsnh/Vestigium.Logging) JSON Lines (sibling repo, solution folder `/Logging/`)

Umbrella requirements: [`_Documentation/Requirements_v1.0.md`](_Documentation/Requirements_v1.0.md)  
Umbrella developer notes: [`_Documentation/DevelopersGuide_v1.0.md`](_Documentation/DevelopersGuide_v1.0.md)

## Logging

Libraries never call `VestigiumLogger.Initialize`. They call `HelperLog.*`, which is a no-op until a host initializes.

Each CLI demo is a host. It initializes with that helper's APPID. JSONL lands at:

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
| `Vestigium.Helpers` | `net10.0` | Guards, `HelperLog`, `HelperDemoHost` |
| `Vestigium.Helpers.ClosedXml` | `net10.0` | ClosedXML Excel wrappers |
| `Vestigium.Helpers.Encryption` | `net10.0` | Hashing and encryption helpers |
| `Vestigium.Helpers.WinReg` | `net10.0-windows` | Windows Registry helpers |
| `Vestigium.Helpers.Json` | `net10.0` | System.Text.Json helpers |
| `Vestigium.Helpers.Xml` | `net10.0` | XML document helpers |
| `Vestigium.Helpers.FileIo` | `net10.0` | File and directory helpers |
| `Vestigium.Helpers.Processes` | `net10.0` | Process launch and capture |
| `Vestigium.Helpers.Services` | `net10.0` | Service control helpers |
| `Vestigium.Helpers.Analytics` | `net10.0` | Numeric series descriptors, quartile bands, confidence intervals |
| `Vestigium.Helpers.Network` | `net10.0` | HTTP / socket helpers |
| `Vestigium.Helpers.Tests` | `net10.0-windows` | xUnit (logger collection is serial) |

Each library has a matching `*.Demo` console project under the **Demo** solution folder.

## Open in Visual Studio

1. Clone this repository **and** `Vestigium.Logging` next to it.
2. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
3. Restore NuGet.
4. Set any `*.Demo` project as startup, F5. Then open `%ProgramData%\Vestigium\Logs\{APPID}\`.
5. Run `Vestigium.Helpers.Tests` for the contract.

```
dotnet run --project src/Vestigium.Helpers.ClosedXml.Demo
dotnet test src/Vestigium.Helpers.Tests
```

## Contracts that do not move

- `.slnx` (not legacy `.sln`).
- Helpers never call `Initialize`. CLIs and application hosts do.
- Disk format is JSON Lines from Vestigium.Logging. No `.log` / CSV path.
- `WinReg` stays on `net10.0-windows`.
- Tests must not hit live ProgramData; they pass a temp `LogDirectory`.
- `Vestigium.Logging` stays a sibling repo. Do not vendor its source into Helpers.
