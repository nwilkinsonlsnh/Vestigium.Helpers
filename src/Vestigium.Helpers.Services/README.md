# Vestigium.Helpers.Services

Windows Service Control Manager: list, search, control, logon, recovery, watch, campaigns. Kql queries use `KqlPack.Service`. This package owns the SCM rows.

TFM is `net10.0-windows`.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Services` 1.0.0 |
| TFM | `net10.0-windows` |
| APPID | `Services` (`ServicesCatalog.AppId`) |
| EVENTID | Reserved 15500–15999 (used through 15525) |
| Depends on | Kql, Processes, `System.ServiceProcess.ServiceController` 9.0.8 |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Services" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Services;

var rows = ServiceHelper.List();
var one  = ServiceHelper.Get("EventLog");
var hits = ServiceHelper.Search("sql", ServiceSearchMode.Contains);
var kql  = ServiceHelper.Search("Name LIKE 'sql%'");
```

## Surface

| Call | Returns | Notes |
|---|---|---|
| `List` / `ListHidden` / `Get` / `TryGet` | rows | Missing name is null / false. |
| `Search(term, mode)` / `Search(query)` | rows | Cap 256. |
| `GetDependsOn` / `GetDependedBy` / `GetDependencyTree` | tree | Cap 256 unique names. |
| `Start` / `Stop` / `Restart` / `Pause` / `Continue` | `ServiceControlResult` | Does not throw on Access Denied. |
| `SetStartType` / `SetLogon` / `SetRecovery` | result | Protected names refuse these. |
| `Watch` / `WatchQuery` | watcher | Interval 250 ms–60 s. |
| `CreateCampaign` / `LoadCampaign` | campaign | In-process. Samples under ProgramData. |
| `ServicesCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

`ProtectedNames` cannot be Stopped, Restarted, or have StartType / Logon / Recovery changed. EventLog is on that list.

## Rules that do not move

- Windows only.
- Control returns a result. Access Denied is not an exception.
- Confirm on destructive calls. Confirm does not override `ProtectedNames`.
- Never log the account password. `ServiceInfo` has no password field. JSONL writes `password=***`.
- Campaigns need the host process to stay running.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Services
    cfg.LogDirectory = logDir;
    ServicesCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/services.json`.

## Related

Filter dialect: `Vestigium.Helpers.Kql`. Process join: `Vestigium.Helpers.Processes`.

Long-form documents live in [Vestigium.Documentation / Helpers / Services](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Services/004%20--Developers%20Guide) |
