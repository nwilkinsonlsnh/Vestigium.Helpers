# Vestigium.Helpers — Requirements Specification

**Document ID:** VEST-HLP-SRS-000  
**Version:** 1.1  
**Status:** Active  
**Date:** 7 September 2026

## 1. Purpose

Provide small, independently referenced helper libraries for the Vestigium suite so diagnostic hosts (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost) and supporting services do not re-implement Excel, registry, JSON, XML, file, process, service, analytics, or network boilerplate.

## 2. Scope

In scope for this milestone: solution skeleton, project files, `HelperLog` façade, one WPF gallery per library, xUnit contracts for logging, umbrella documentation, CI build.

Out of scope until a per-library SRS is accepted: real algorithm work, NuGet publish.

## 3. Projects

| ID | Project | TFM | Notes |
|---|---|---|---|
| HLP-CORE | Vestigium.Helpers | net10.0 | Guards, `HelperLog`, `HelperWpfHost` |
| HLP-XLS | Vestigium.Helpers.ClosedXml | net10.0 | Wraps ClosedXML 0.105.1 |
| HLP-ENC | Vestigium.Helpers.Encryption | net10.0 | No custom crypto primitives |
| HLP-REG | Vestigium.Helpers.WinReg | net10.0-windows | Windows Registry only |
| HLP-JSON | Vestigium.Helpers.Json | net10.0 | System.Text.Json |
| HLP-XML | Vestigium.Helpers.Xml | net10.0 | |
| HLP-FIO | Vestigium.Helpers.FileIo | net10.0 | |
| HLP-PRC | Vestigium.Helpers.Processes | net10.0 | |
| HLP-SVC | Vestigium.Helpers.Services | net10.0 | SCM / hosted services |
| HLP-ANL | Vestigium.Helpers.Analytics | net10.0 | In-process only in v1 |
| HLP-NET | Vestigium.Helpers.Network | net10.0 | |
| HLP-CSV | Vestigium.Helpers.Csv | net10.0 | Skeleton; not ClosedXml |
| HLP-TST | Vestigium.Helpers.Tests | net10.0-windows | xUnit, serial logger collection |

Each library has a matching `*.Demo` WPF gallery (`net10.0-windows`). Analytics and ClosedXml are full galleries. The rest use the shared skeleton in `Vestigium.Helpers.Gallery`.

## 4. Logging

- Disk format is Vestigium.Logging JSON Lines. No `.log` / CSV path.
- Path: `%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json`.
- Libraries never call `VestigiumLogger.Initialize`. `HelperLog` is a no-op until the host initializes.
- Each WPF gallery initializes with that helper's APPID so each demo writes its own folder.
- Tests must not hit live ProgramData; they pass a temp `LogDirectory`.

## 5. Non-functional

- .NET 10 LTS, C# latest, nullable enabled.
- Packable class libraries (`IsPackable=true`) except Tests and Demos.
- Deterministic builds.
- Helpers do not reference WPF, Themes, or Controls. Demo galleries do (`Vestigium.Helpers.Gallery`).
- `WinReg` stays on `net10.0-windows`.

## 6. Open items

- Encryption key-storage contract (DPAPI vs raw key material).
- Whether Services targets Service Control Manager only, or also `IHostedService`.
- Csv lossless SRS (delimiter, quoting, injection prefix). ClosedXml write surface is accepted in `src/Vestigium.Helpers.ClosedXml/_Documentation/Requirements_v1.0.md`.
