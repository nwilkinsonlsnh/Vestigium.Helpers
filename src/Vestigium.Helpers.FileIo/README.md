# Vestigium.Helpers.FileIo

Validated file jobs. Robocopy is the behavior reference; FileIo is the record. This package does not spawn `robocopy.exe`.

Recon fills five size buckets. Default collision is UniqueName (`.##`). Cap is `NameCap` — the original dest is never overwritten unless the caller picks Overwrite.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.FileIo` 1.1.1 |
| TFM | `net10.0` |
| APPID | `FileIo` (`FileIoCatalog.AppId`) |
| EVENTID | Reserved 12500–12999 (used through 12610) |
| Depends on | `Vestigium.Helpers.Analytics` 1.0.1, `Vestigium.Helpers.Hashing` 1.4.0, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.FileIo" Version="1.1.1" />
```

```csharp
using Vestigium.Helpers.FileIo;

var job = FileIoHelper.Copy(export, archive);
var result = await job.RunAsync();

var audit = FileIoHelper.Copy(export, archive, new FileIoJobOptions { AuditMode = true });
_ = await audit.RunAsync(); // recon + decisions, no disk change
```

`JobId` is `fio-` + 12 hex. Correlation id on every log line.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `FileIoHelper.Copy / Move / Delete / Mirror` | `FileIoJob` | Then `RunAsync`. |
| `job.Pause` / `Resume` / `Cancel` | void | Pause finishes the current 64 KiB buffer. Cancel deletes dest this job created. |
| `AnalyzeDirectory` | analysis | Tree sizes without a copy. |
| `WriteProbe` / `ReadProbe` | probe result | Sized fixture under a caller path. |
| `CompareFiles(left, right)` | compare result | Hashing sibling does the digest. |
| `PruneEmptyDirectories` | int | Root is kept. |
| `SecureDelete(path, recipe)` | void | Then delete. Flash is best-effort. |
| `FileIoCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

Collision: `UniqueName` (default), `Skip`, `Overwrite`.

## Rules that do not move

- No `robocopy.exe`. No `ReadAllBytes` on a payload.
- UniqueName never overwrites the original dest. Cap is an error, not a clobber.
- Audit Mode writes Would* decisions and changes no disk.
- Cancel removes only files this job created.
- No payload bytes and no `Exception` objects in JSONL.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not FileIo
    cfg.LogDirectory = logDir;
    FileIoCatalog.Register(cfg);
    AnalyticsCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`. Dest unique-content index: `%ProgramData%\Vestigium\FileIo\Indexes\`.

Named events live in `EventCatalog/fileio.json`.

## Related

Sizes: `Vestigium.Helpers.Analytics`. Digests: `Vestigium.Helpers.Hashing`.

Long-form documents live in [Vestigium.Documentation / Helpers / FileIo](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/FileIo/004%20--Developers%20Guide) |
