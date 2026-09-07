# Vestigium.Helpers — Requirements Specification

**Document ID:** VEST-HLP-SRS-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 7 September 2026

## 1. Purpose

Provide small, independently referenced helper libraries for the Vestigium suite so diagnostic hosts (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost) and supporting services do not re-implement Excel, registry, JSON, XML, file, process, service, analytics, or network boilerplate.

## 2. Scope

In scope for this milestone: solution skeleton, project files, public type placeholders, umbrella documentation, CI build.

Out of scope until a per-library SRS is accepted: real algorithm work, NuGet publish, demo hosts.

## 3. Projects

| ID | Project | TFM | Notes |
|---|---|---|---|
| HLP-CORE | Vestigium.Helpers | net10.0 | Shared guards only |
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

## 4. Non-functional

- .NET 10 LTS, C# latest, nullable enabled.
- Packable class libraries (`IsPackable=true`) except Tests.
- Deterministic builds.
- Helpers do not reference WPF, Themes, or Controls.
- Helpers do not write log files. Callers use Vestigium.Logging.

## 5. Open items

- Exact ClosedXML surface (read-only vs write, template workbooks).
- Encryption key-storage contract (DPAPI vs raw key material).
- Whether Services targets Service Control Manager only, or also `IHostedService`.
