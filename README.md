# Vestigium.Helpers

[![build](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml/badge.svg)](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/actions/workflows/build.yml)

Cross-cutting helper libraries for the Vestigium suite (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost).

**Target:** .NET 10 LTS / Visual Studio 2026  
**Shape:** class libraries, independently referenced  
**Windows-only project:** `Vestigium.Helpers.WinReg`

Umbrella requirements: [`_Documentation/Requirements_v1.0.md`](_Documentation/Requirements_v1.0.md)  
Umbrella developer notes: [`_Documentation/DevelopersGuide_v1.0.md`](_Documentation/DevelopersGuide_v1.0.md)

This repository is a **skeleton**. Public types compile and document intent. Behaviour lands in later milestones after each library's SRS is accepted.

## Libraries under this solution

| Project | TFM | Role |
|---|---|---|
| `Vestigium.Helpers` | `net10.0` | Core guards and shared primitives |
| `Vestigium.Helpers.ClosedXml` | `net10.0` | ClosedXML Excel wrappers |
| `Vestigium.Helpers.Encryption` | `net10.0` | Hashing and encryption helpers |
| `Vestigium.Helpers.WinReg` | `net10.0-windows` | Windows Registry helpers |
| `Vestigium.Helpers.Json` | `net10.0` | System.Text.Json helpers |
| `Vestigium.Helpers.Xml` | `net10.0` | XML document helpers |
| `Vestigium.Helpers.FileIo` | `net10.0` | File and directory helpers |
| `Vestigium.Helpers.Processes` | `net10.0` | Process launch and capture |
| `Vestigium.Helpers.Services` | `net10.0` | Service control helpers |
| `Vestigium.Helpers.Analytics` | `net10.0` | Counters and timings |
| `Vestigium.Helpers.Network` | `net10.0` | HTTP / socket helpers |
| `Vestigium.Helpers.Tests` | `net10.0-windows` | xUnit smoke tests |

`WinReg` is the only Windows-only library. Every other project targets `net10.0` so console hosts, Windows Services, and WPF apps can share the same package.

The umbrella project does **not** reference the sibling libraries. Consuming apps add the helper they need.

## Open in Visual Studio

1. Clone this repository.
2. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
3. Restore NuGet.
4. Build. Run `Vestigium.Helpers.Tests`.

## Contracts that do not move

- `.slnx` (not legacy `.sln`).
- `Directory.Build.props` sets nullable, implicit usings, latest C#.
- MIT license, same copyright line as Logging / Controls / Themes.
- GitHub Actions `windows-latest` + `dotnet-version: 10.0.x`.
- No WPF, no Themes, no Controls references from these libraries.
- `Vestigium.Helpers.WinReg` stays on `net10.0-windows`. Do not multi-target it.
- Helpers never log by themselves. Hosts that want disk traces call `Vestigium.Logging`.

## Suite neighbors

- [Vestigium.Logging](https://github.com/nwilkinsonlsnh/Vestigium.Logging) — centralized Serilog module.
- [Vestigium.Controls](https://github.com/nwilkinsonlsnh/Vestigium.Controls) — WPF control library.
- [Vestigium.Themes](https://github.com/nwilkinsonlsnh/Vestigium.Themes) — host-assigned palettes.
