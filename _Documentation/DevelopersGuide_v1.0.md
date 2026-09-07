# Vestigium.Helpers — Developers Guide

**Document ID:** VEST-HLP-DEV-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 7 September 2026

## Open the solution

1. Clone `https://github.com/nwilkinsonlsnh/Vestigium.Helpers`.
2. Open `Vestigium.Helpers.slnx` in Visual Studio 2026.
3. Build. Run `Vestigium.Helpers.Tests`.

## Where things live

| Need | Place |
|---|---|
| Shared guards | `src/Vestigium.Helpers/HelperGuard.cs` |
| Excel wrapper | `src/Vestigium.Helpers.ClosedXml/WorkbookHelper.cs` |
| Registry wrapper | `src/Vestigium.Helpers.WinReg/RegistryHelper.cs` |
| Umbrella SRS | `_Documentation/Requirements_v1.0.md` |
| Per-library docs | `src/Vestigium.Helpers.*/_Documentation/` |

## Conventions copied from the suite

- `.slnx` (not legacy `.sln`)
- `Directory.Build.props` sets nullable, implicit usings, latest C#
- MIT license, same copyright line as Logging / Controls / Themes
- GitHub Actions `windows-latest` + `dotnet-version: 10.0.x`

## Adding a helper later

1. New class library `src/Vestigium.Helpers.{Name}` targeting `net10.0` unless it is Windows-only.
2. `_Documentation/Requirements_v1.0.md` and `DevelopersGuide_v1.0.md` before implementation.
3. Add the project under the `/Library/` folder in `Vestigium.Helpers.slnx`.
4. Reference it from Tests. Do not add it to the core `Vestigium.Helpers` project.

## Build mode

Public types in this commit exist so the solution compiles. Treat each library SRS as the source of truth before growing an API.
