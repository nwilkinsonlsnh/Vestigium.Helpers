# Vestigium.Helpers.Network

Workstation inventory and protocol jobs for diagnostic hosts. Not a CLI. Not `ping.exe`. Not a plot package. This library will not grow a plot API.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Network` 1.0.1 |
| TFM | `net10.0` |
| APPID | `Network` (`NetworkCatalog.AppId`) |
| EVENTID | Reserved 14500–14999 (used through 14555) |
| Depends on | `Vestigium.Helpers.Json` 1.0.1, `Vestigium.Helpers.Analytics` 1.0.1, `Vestigium.Helpers.FileIo` 1.1.1, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/002%20--%20Requirements%20Document) |

Does not plot. OUI completeness is a URL fetched on request. The embedded snapshot is a stub and is not the IEEE registry.

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="1.0.1" />
```

```csharp
using Vestigium.Helpers.Network;

var box = NetworkHelper.GetWorkstation();
var echo = await NetworkHelper.IcmpEcho("192.0.2.1").RunAsync();
var dns  = await NetworkHelper.LookupAsync("example.com");
var routes = NetworkHelper.GetRoutes();
```

`Ping` / `Trace` are aliases for `IcmpEcho` / `IcmpTrace`.

## Surface

| Call | Returns | Notes |
|---|---|
| `GetWorkstation` / `GetAdapters` / `GetSnapshot` | inventory | Local stack. |
| `IcmpEcho` / `IcmpTrace` | `NetworkJob<T>` | Then `RunAsync`. |
| `LookupAsync` | DNS result | |
| `GetConnections` / `GetRoutes` / `GetNeighbors` | lists | Route print works on Windows and Linux. |
| `AddRoute` / `ChangeRoute` / `RemoveRoute` | void | Default `0.0.0.0/0` and `::/0` throw `NetworkRouteDenied`. |
| `CreateEchoCampaign` / `CreateShareCampaign` | campaign | Share campaigns use FileIo probes. No password field. |
| `ClassifyAddress` / `DescribePrefix` / `PlanByHosts` | prefix math | |
| `ParseMac` / `LookupOuiAsync` / `LookupOuiPacked` | MAC / OUI | Live lookup is the URL. Packed is a stub. |
| `Bandwidth` / `BillP95` / `BillPercentile` | amounts | Network facts. This library does not plot. |
| `NetworkCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Not `ping.exe` / `tracert.exe`. Jobs are BCL + IP Helper / ICMP.
- Route writes need admin / `CAP_NET_ADMIN`. Defaults are denied.
- NetBIOS is Windows-only.
- OUI completeness is `LookupOuiAsync` against the caller URL. The IEEE registry is not packed. The embedded snapshot is not grown.
- Never log credentials. Share campaigns have no password field.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Network
    cfg.LogDirectory = logDir;
    NetworkCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/network.json`.

## Related

Sizes: `Vestigium.Helpers.Analytics`. Share probes: `Vestigium.Helpers.FileIo`. Campaign recipes: `Vestigium.Helpers.Json`.

Long-form documents live in [Vestigium.Documentation / Helpers / Network](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/004%20--Developers%20Guide) |
