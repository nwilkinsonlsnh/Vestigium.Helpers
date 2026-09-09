# Vestigium.Helpers — Requirements Specification

**Document ID:** VEST-HLP-SRS-000  
**Version:** 1.2  
**Status:** Active  
**Date:** 8 September 2026

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
| HLP-ENC | Vestigium.Helpers.Encryption | net10.0 | AES-256-GCM, ChaCha20-Poly1305, AES-256-CBC+HMAC (v1.1), RSA-OAEP wrap (v1.2), Argon2id; no custom primitives |
| HLP-HASH | Vestigium.Helpers.Hashing | net10.0 | SHA-256 default, SHA-384/512, SHA-3, HMAC-SHA256/384/512/SHA3, KMAC, SHAKE, Argon2id PHC. Hex default + Base64 converters. |
| HLP-REG | Vestigium.Helpers.WinReg | net10.0-windows | Windows Registry only |
| HLP-JSON | Vestigium.Helpers.Json | net10.0 | System.Text.Json |
| HLP-XML | Vestigium.Helpers.Xml | net10.0 | |
| HLP-FIO | Vestigium.Helpers.FileIo | net10.0 | SRS v1.0 accepted. Recon job engine, UniqueName, Audit Mode, Pause/Cancel. |
| HLP-PRC | Vestigium.Helpers.Processes | net10.0 | |
| HLP-SVC | Vestigium.Helpers.Services | net10.0 | SCM / hosted services |
| HLP-ANL | Vestigium.Helpers.Analytics | net10.0 | In-process only in v1 |
| HLP-CHARTS | Vestigium.Helpers.Charts | net10.0-windows | ScottPlot wrapper. UCL/LCL are inputs. |
| HLP-NET | Vestigium.Helpers.Network | net10.0 | |
| HLP-CSV | Vestigium.Helpers.Csv | net10.0 | RFC 4180 read/write, settable delimiter; not ClosedXml |
| HLP-TST | Vestigium.Helpers.Tests | net10.0-windows | xUnit, serial logger collection |

Each library has a matching `*.Demo` WPF gallery (`net10.0-windows`). Analytics, ClosedXml, Charts, Csv, Encryption, Hashing, and FileIo are shipped galleries with accepted SRS + design companion. The rest use the shared skeleton in `Vestigium.Helpers.Gallery` until their own lossless SRS is accepted.

## 4. Logging

- Disk format is Vestigium.Logging JSON Lines. No `.log` / CSV path.
- Path: `%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json`.
- Libraries never call `VestigiumLogger.Initialize`. `HelperLog` is a no-op until the host initializes.
- Each WPF gallery initializes with that helper's APPID so each demo writes its own folder.
- Tests must not hit live ProgramData; they pass a temp `LogDirectory`.
- Debug enter/argument lines stay in the compiled helpers. Hosts choose volume with `VestigiumLoggerOptions.MinimumDiskLevel` (galleries: Debug; quiet hosts: Information).
- `HelperGuard` is the contract layer: log Failed, then throw. Empty, null, non-finite, and 1-based range rejects go through it.

## 5. Non-functional

- .NET 10 LTS, C# latest, nullable enabled.
- Packable class libraries (`IsPackable=true`) except Tests and Demos.
- Deterministic builds.
- Helpers do not reference WPF, Themes, or Controls except `Vestigium.Helpers.Charts` (ScottPlot.WPF). Demo galleries do (`Vestigium.Helpers.Gallery`).
- `WinReg` and `Charts` stay on `net10.0-windows`.

## 6. Open items

- **Hashing lossless SRS** — accepted and shipped v1.0–v1.3 (SHA-2, SHA-3, HMAC-SHA256/384/512/SHA3, KMAC, SHAKE, Argon2id PHC, CRC/xxHash). Trailer fill remains an Encryption minor revision.
- Whether Services targets Service Control Manager only, or also `IHostedService`.

Accepted with a future roadmap: ClosedXml (SRS v1.1), Analytics (SRS v1.5), Charts (SRS v1.1), Csv (SRS v1.0), Encryption (SRS v1.0, v1.1 CBC + v1.2 RSA wrap shipped), Hashing (SRS v1.0 + v1.1 checksums + v1.2 HMAC-SHA2 + v1.3 HMAC-SHA3/KMAC/SHAKE), FileIo (SRS v1.0).
