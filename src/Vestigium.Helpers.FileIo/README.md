# Vestigium.Helpers.FileIo

Validated file jobs. Robocopy is the behavior reference; FileIo is the record. This package does **not** spawn `robocopy.exe`.

**Version:** 1.1.0

## Verbs

`Copy`, `Move`, `Delete`, `Mirror`. Recon fills five size buckets. Default collision is UniqueName (`.##`). Cap is `NameCap` — the original dest is never overwritten.

**Audit Mode** runs recon and decisions and changes no disk. JSONL still has WouldCopy / WouldUniqueName / WouldDelete.

Pause finishes the current 64 KiB buffer. Cancel aborts and deletes dest this job created.

## Dependencies

| Package | Version | Role |
|---|---|---|
| `Vestigium.Helpers.Analytics` | 1.0.1 | `NumericSeries` snapshots at finalize |
| `Vestigium.Logging` | 1.7.1 | JSONL. Host calls `VestigiumLogger.Initialize` + `FileIoCatalog.Register`. |
| `Vestigium.Helpers.Hashing` | project reference until that package is on the same feed | Digests for unique-content and Compare |

Do not publish this nupkg to nuget.org while Hashing is still a `<ProjectReference>`.

## Logging door

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = FileIoCatalog.AppId;
    cfg.LogDirectory = logDir;
    FileIoCatalog.Register(cfg);
    AnalyticsCatalog.Register(cfg);
});
```

EVENTID block 12500–12610. `correlationId` is `JobId` (`fio-` + 12 hex). No payload bytes. No `Exception` objects.

## Release notes 1.1.0

Consumes Analytics 1.0.1. Logging 1.7.1 via `Directory.Build.props`. Custom EVENTID 12500–12610. Dest unique-content index is jsonl. Analyze and Job share `FileIoMask`. No robocopy.exe. No FileIo Demo project.
