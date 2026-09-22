# Vestigium.Helpers

[![build](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml/badge.svg)](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml)

Cross-cutting helper libraries for the Vestigium suite (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

**Target:** .NET 10 LTS / Visual Studio 2026  
**Shape:** class libraries. Some older libraries still have a WPF gallery; **new work does not add Demo projects.** FileIo, Network, and Hashing have none.  
**Windows-only projects:** `Vestigium.Helpers.WinReg`, `Vestigium.Helpers.Charts`  
**Logging:** [Vestigium.Logging](https://www.nuget.org/packages/Vestigium.Logging) 1.7.1 (NuGet).

Umbrella requirements: [`_Documentation/Requirements_v1.0.md`](_Documentation/Requirements_v1.0.md)  
Umbrella developer notes: [`_Documentation/DevelopersGuide_v1.0.md`](_Documentation/DevelopersGuide_v1.0.md)

## Logging

Libraries never call `VestigiumLogger.Initialize`. FileIo writes through `FileIoLog` (no-op until a host initializes). Hashing writes through `HashingLog` the same way.

JSONL lands at:

```
%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json
```

Restore `Vestigium.Logging` 1.7.1 from nuget.org (`Directory.Build.props`). Do not vendor Logging source into this repo.

## Libraries

| Project | TFM | Role |
|---|---|
| `Vestigium.Helpers.ClosedXml` | `net10.0` | ClosedXML write-first Excel (`.xlsx`) |
| `Vestigium.Helpers.Csv` | `net10.0` | RFC 4180 CSV / TSV |
| `Vestigium.Helpers.Encryption` | `net10.0` | AES-256-GCM, ChaCha20-Poly1305, AES-256-CBC+HMAC, RSA-OAEP wrap, Argon2id |
| `Vestigium.Helpers.Hashing` | `net10.0` | SHA-256 default, SHA-384/512, SHA-3, HMAC-SHA256, Argon2id PHC. **No Demo project.** |
| `Vestigium.Helpers.WinReg` | `net10.0-windows` | Windows Registry helpers |
| `Vestigium.Helpers.Json` | `net10.0` | System.Text.Json helpers |
| `Vestigium.Helpers.Xml` | `net10.0` | XML document helpers |
| `Vestigium.Helpers.FileIo` | `net10.0` | Validated file jobs: recon, five buckets, UniqueName, Audit Mode, Pause/Cancel, Analytics sizes/rates. Not robocopy.exe. **No Demo project.** |
| `Vestigium.Helpers.Processes` | `net10.0` | Process launch and capture |
| `Vestigium.Helpers.Services` | `net10.0` | Service control helpers |
| `Vestigium.Helpers.Analytics` | `net10.0` | NumericSeries: five-number, bands, P95, intervals, ControlLimits |
| `Vestigium.Helpers.Charts` | `net10.0-windows` | ScottPlot wrapper. Does not compute UCL/LCL. |
| `Vestigium.Helpers.Network` | `net10.0` | Workstation inventory, ICMP Echo/Trace, DNS. No Demo project. |
| `Vestigium.Helpers.Tests` | `net10.0-windows` | xUnit |

**FileIo, Network, and Hashing have no Demo project.** Hosts consume those libraries.

## Open in Visual Studio

1. Clone this repository.
2. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
3. Restore NuGet (`Vestigium.Logging` 1.7.1 comes from nuget.org).
4. There is no `Vestigium.Helpers.Hashing.Demo`, `FileIo.Demo`, or `Network.Demo`. Run tests for those contracts.
5. Run `Vestigium.Helpers.Tests` for the contract.

```
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Hashing
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

## Contracts that do not move

- `.slnx` (not legacy `.sln`).
- Helpers never call `Initialize`. Application hosts do.
- Disk format is JSON Lines from Vestigium.Logging.
- `WinReg` stays on `net10.0-windows`.
- Tests must not hit live ProgramData; they pass a temp `LogDirectory` or `IndexRootOverride`.
- `Vestigium.Logging` is a NuGet package. Do not vendor its source into Helpers.
