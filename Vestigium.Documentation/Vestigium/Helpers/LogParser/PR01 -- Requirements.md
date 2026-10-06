# Vestigium.Helpers.LogParser — Requirements

**Document ID:** VEST-HLP-LOGPARSER-REQ-PR01
**Version:** 1.0
**Status:** Lock for PR01.
**Date:** 6 October 2026
**Package:** `Vestigium.Helpers.LogParser` 1.0.0 (not packed)
**TFM:** `net10.0`
**Depends on:** `Vestigium.Logging` 1.7.1
**Project on disk:** `src/Vestigium.Helpers.LogParser` (stub at `c80a440`, `Class1.cs` only)

**One sentence:** The shared bag every LogParser reader fills. Host, ports, hit count, source, address-or-name.

**This version is not** a HAR reader, a URL scraper, a DNS probe, or a logger the children can subclass. `LogParser.Domain` is not a package in this work. The project exists on disk. It is out. Owner deletes it.

## Decisions

| # | Decision | Locked as |
|---|---|---|
| 1 | Role | Types only. No `Read` method in this package. |
| 2 | Children | `LogParser.Har` and `LogParser.Url` reference this package. They do not copy the records. |
| 3 | Domain | Dropped. Bare names live in `LogParser.Url`. |
| 4 | Logging | Internal facade. No `Initialize`. No-op when the host has not initialized. Pin `Vestigium.Logging` 1.7.1. |
| 5 | Identity | APPID `LogParser`. Host directory is `%ProgramData%\Vestigium\Logs\LogParser` (`LogParserCatalog.LogDirectory`). The library does not call `Initialize` and does not create the folder. One process has one log directory. A DnsIQ host that does not set this path still writes to the DnsIQ folder. |

## Must change

### R01-01 Records

| Type | Role |
|---|---|
| `LogFormat` | `Unknown`, `Har`, `Url`. |
| `LogHostSource` | Flags: `Request`, `Redirect`, `Location`, `Page`, `Url`. |
| `LogHost` | Host (ASCII, lower, no trailing dot), ports seen, hit count, sources, `IsAddress` when the token is already an IP. |
| `LogReadResult` | Format, entry count, page count, hosts, warnings. |

No `ILogParser`. Two static readers in two packages. An interface does not buy a swap.

Host rules the children must be able to express with these types:

- Case-fold. Strip one trailing dot.
- Deduplicate on host. Union ports. Union sources.
- IP literal is listed with `IsAddress=true`. The DNS probe skips it. This package does not probe.

### R01-02 Logging

Same shape as `AnalyticsLog`.

- `internal` static `LogParserLog`. Not public.
- Writes only when `VestigiumLogger.IsInitialized`.
- Event APPID is `LogParser`. Category `LogParser`.
- Event catalog JSON under `EventCatalog/`. Count by 5. Do not reuse another library's range. Allocate at implementation and record the range in the catalog file.
- This package logs its own failures only. A child does not call this facade. Each child has its own internal log.

### R01-03 Package

- `net10.0`. Nullable. No WPF. No `System.Net.Sockets`.
- Delete `Class1.cs`.
- Package id `Vestigium.Helpers.LogParser`. Version 1.0.0 when packed. Not packed in this requirements pass.
- No Demo project.

## Must not

- HAR JSON
- URL or bare-domain scrape
- DNS, TCP, file dialogs
- `VestigiumLogger.Initialize`
- A public logger
- `LogParser.Domain` as a dependency or a format value

## Acceptance

1. A referencing project can construct `LogHost` and `LogReadResult` without a reader type in this package.
2. `LogFormat` has `Har` and `Url` and does not have `Domain`.
3. Uninitialized host: a log call does not throw.
4. `dotnet test` for this project is green and off the wire.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 6 Oct 2026 | Initial lock. Domain out. Types only. |
