# Vestigium.Helpers.Processes

Windows process table: list, get, search, watch, lifetime, campaigns. Kql queries bind through `Vestigium.Helpers.Kql` — this package owns the rows.

TFM is `net10.0-windows`.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Processes` 1.0.0 |
| TFM | `net10.0-windows` |
| APPID | `Processes` (`ProcessesCatalog.AppId`) |
| EVENTID | Reserved 15000–15499 |
| Depends on | Json, Kql, `System.Diagnostics.PerformanceCounter` 9.0.8, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Processes" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Processes;

var rows = ProcessHelper.List();
var one  = ProcessHelper.Get(pid);
var hits = ProcessHelper.Search("vestigium", ProcessSearchMode.Contains);
var kql  = ProcessHelper.Search("(PID == 10 || Name LIKE '%edge%') && GPU.Usage GT 20");
```

## Surface

| Call | Returns | Notes |
|---|---|---|
| `List` / `Get` / `TryGet` | rows | Missing PID is null / false. |
| `Search(term, mode)` | rows | StartsWith / EndsWith / Contains. Cap 1..4096. |
| `Search(query)` | rows | Kql pack Process. |
| `GetTree` / `GetChildren` / `GetThreads` | tree / rows | |
| `Watch(pid, …)` / `Watch(query, …)` | watcher | Interval 250 ms–60 s. |
| `Start` / `StartAs` | start result | Never log the password. |
| `Kill` / `KillTree` / `KillSearch` | kill result | Skips denylist, PPL, Protected. |
| `CreateCampaign` | campaign | In-process only. No `schtasks`. |
| `ProcessesCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Windows only. This is not a Linux process table.
- Missing PID is not a fake row.
- Confirm does not override denylist / PPL / Protected.
- Never log command lines as secrets, passwords, or per-tick watcher samples.
- Campaigns need the host process to stay running.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Processes
    cfg.LogDirectory = logDir;
    ProcessesCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/processes.json`.

## Related

Filter dialect: `Vestigium.Helpers.Kql`. Campaign recipes: `Vestigium.Helpers.Json`.

Long-form documents live in [Vestigium.Documentation / Helpers / Processes](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Processes/004%20--Developers%20Guide) |
