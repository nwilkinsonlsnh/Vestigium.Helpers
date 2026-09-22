# Vestigium.Helpers.WinReg

Windows Registry read/write helpers. Not `reg.exe`. Not `regedit`. Windows only.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.WinReg` 1.0.0 |
| TFM | `net10.0-windows` |
| APPID | `WinReg` (`WinRegCatalog.AppId`) |
| EVENTID | Reserved 16000–16499 |
| Depends on | `System.Security.Cryptography.ProtectedData` 10.0.0, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 - Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/002%20-%20Requirements%20Document) |

This folder on disk is `002 - Requirements Document` (one dash). Related links use that name.

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.WinReg" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.WinReg;

var key = RegistryHelper.Local.GetKey(RegistryHiveKind.CurrentUser, "Software");
var hits = RegistryHelper.Search(RegistryHiveKind.CurrentUser, "Software", "Vestigium");
```

Remote: `RegistryHelper.For(machine)` after `CanConnect`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `Local` / `For` / `CanConnect` | client | ConnectTimeout default 3 s. |
| `GetKey` / `GetValue` / `ListSubKeys` | info or null | Missing is null. `/` throws. |
| `Search` | hits | Cap 256 / depth 32. |
| Writes (`SetValue`, `DeleteKey`, …) | `RegistryWriteResult` | Need `confirm: true`. |
| `Export` / `Import` | result | |
| `MountHive` / `DismountHive` | mount / result | HKLM or HKU, local, confirm. |
| `WriteIndex` / `Compare` | index / diff | Offline JSONL. No payloads in the index. |
| `WinRegCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Windows only. No `reg.exe` / `regedit` spawn.
- Writes need `confirm: true`. Results, not surprise exceptions, on deny.
- Logs path + value **name** only. Never log value data.
- Journal is `vest-regjnl/1`. Compact / Purge fail `InUse` while open.
- Compare is index vs index. Restore without payload is `Unsupported`.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not WinReg
    cfg.LogDirectory = logDir;
    WinRegCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/winreg.json`.

## Related

Long-form documents live in [Vestigium.Documentation / Helpers / WinReg](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/001%20--%20Implementation%20Plan) |
| 002 - Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/002%20-%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/WinReg/004%20--Developers%20Guide) |
