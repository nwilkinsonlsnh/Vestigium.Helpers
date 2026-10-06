# Vestigium.Helpers.LogParser — Implementation Plan

**Document ID:** VEST-HLP-LOGPARSER-PLAN-PR02
**Version:** 1.0
**Status:** Open.
**Date:** 6 October 2026
**Governs:** [PR01 -- Requirements.md](PR01%20--%20Requirements.md)

**Goal:** Replace the stub with the host bag and the internal log facade. Pack is a later slice, after Har and Url can reference it.

**Not:** A reader. A Domain package. A DNS call.

## Current state

`src/Vestigium.Helpers.LogParser` is an empty SDK project. `Class1.cs`. No `Vestigium.Logging` reference. No package metadata. Children do not reference it yet.

## Decisions

| Decision | Why |
|---|---|
| Types only | Har and Url are the doors. A `Read` here becomes a third door with no format. |
| Internal log | House rule. Host initializes. Library no-ops. |
| Domain stays deleted | Owner call, 6 Oct 2026. Bare names are Url's contract. |

## Surface

```
LogFormat
LogHostSource
LogHost
LogReadResult
```

No public `LogParserLog`. Children do not link to it.

## Slices

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | LP-01 | Delete `Class1.cs`. Add the four types. | Done |
| 2 | LP-02 | Pin `Vestigium.Logging` 1.7.1. `LogParserLog` + `EventCatalog`. No-op test. | Done |
| 3 | LP-03 | Har and Url pin `Vestigium.Helpers.LogParser` 1.0.0. No project reference. | Done |
| 4 | LP-04 | Pack 1.0.0. On nuget.org. | Done |

### LP-01

Records as R01-01. `LogHost` is a sealed record. Ports are a read-only set. Sources are flags.

### LP-02

Do not call `Initialize` in the library or in the test. The no-op test asserts a log call with the logger down does not throw.

### LP-03

Package reference `Vestigium.Helpers.LogParser` 1.0.0. Not a project reference. DnsIQ does not reference the project. Suite consumes the pack.

### LP-04

Pushed 6 Oct 2026. Flat container lists 1.0.0. Readme warning only.

## Files

```
src/Vestigium.Helpers.LogParser/
  LogFormat.cs
  LogHostSource.cs
  LogHost.cs
  LogReadResult.cs
  LogParserLog.cs
  LogParserCatalog.cs
  EventCatalog/logparser.json
  Vestigium.Helpers.LogParser.csproj
tests/Vestigium.Helpers.LogParser.Tests/
```

Do not add a Demo. Do not edit `LogParser.Domain` except to leave it out of the solution if it is still included.

## Watch

| Role | Watch |
|---|---|
| Alvin | No reader in this project. No public logger. |
| Theodore | Uninitialized log does not throw. `Class1` is gone. |
| Simon | `Domain` is not a `LogFormat` value. |

## Next action

HAR-01 and URL-01. Readers against the 1.0.0 package. Do not add a project reference.
