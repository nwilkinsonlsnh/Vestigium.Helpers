# Vestigium.Helpers.Kql

KQL-inspired filter dialect and field catalog. Parse, bind against a pack session, evaluate rows. This package does not enumerate processes, services, or counters. `Vestigium.Helpers.Processes` references Kql — not the other way around.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Kql` 1.0.0 |
| TFM | `net10.0` |
| APPID | `Kql` (`KqlLoggingCatalog.AppId`) |
| EVENTID | Reserved 14000–14499 (used through 14020) |
| Depends on | `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/002%20--%20Requirements%20Document) |

`KqlCatalog` is the **field** catalog. `KqlLoggingCatalog` is the **event** catalog. Do not mix them.

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Kql" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Kql;

using var session = KqlHelper.Create(KqlPack.Process);
var compiled = KqlHelper.Compile("(PID == 10 || Name LIKE '%edge%') && GPU.Usage GT 20", session);
```

Hosts that have rows call `ProcessHelper.Search(query)` — that lives in Processes.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `KqlHelper.Create(packs)` / `Create(options)` | `KqlSession` | Pack + group field set. |
| `Parse(text)` | parse result | Syntax only. `A \| where B` is a parse error. |
| `Compile(text, session)` | compile result | Bind names. Unknown field names the enabled pack. |
| `session.TryGetField(name)` | bool + field | Canonical, suffix, and aliases. Case-insensitive. |
| `session.Fields` | field list | What this session can bind. |
| `KqlLoggingCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

Packs: Process, Service, Thread, System, Adapter.

Operators: `==` `!=` `<>` `LIKE` `IN` `BETWEEN` `GT` `LT` `GE` `LE` `<` `>` plus `&&` `\|\|` `!`.

## Rules that do not move

- Not a process enumerator. No reference to `Vestigium.Helpers.Processes`.
- Missing / denied / blank string values are **unknown**, never `""`. Top-level unknown is not a hit.
- Never log RHS strings, command lines, or passwords.
- Pipe (`\| where`) is not supported.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Kql
    cfg.LogDirectory = logDir;
    KqlLoggingCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/kql.json`.

## Related

Row hosts: `Vestigium.Helpers.Processes`, `Vestigium.Helpers.Services`.

Long-form documents live in [Vestigium.Documentation / Helpers / Kql](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Kql/004%20--Developers%20Guide) |
