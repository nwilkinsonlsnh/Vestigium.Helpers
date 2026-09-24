# Vestigium.Helpers.Json

System.Text.Json helpers for payload documents and JSONL. Pretty + camelCase for `.json`. Pointer and dotted path. An edit session so you can see the RFC 6902 patch before disk changes.

Not the audit logger. Not FileIo. One path, 64 KiB streams.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Json` 1.0.0 |
| TFM | `net10.0` |
| APPID | `Json` (`JsonCatalog.AppId`) |
| EVENTID | Reserved 13500–13999 (used through 13625) |
| Depends on | `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Json" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Json;

var doc = JsonHelper.Open(path);
doc.Snapshot();
doc.Set("network.timeoutSeconds", 15);
var patch = doc.Diff();          // RFC 6902 — show it; do not log values
doc.Commit();
doc.Save();
```

JSONL: `JsonHelper.OpenJsonl(path)` then `AppendRecord` / `Record(i)` / `Save`.

Compare two files: `JsonHelper.Compare(leftPath, rightPath)`. Same kind. Diff only.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `Identity` / `Probe` | string | In-memory only. Probe does not write the export folder. |
| `ToJson` / `FromJson` | string / T | RFC 8259. No BOM. `FromJson` honors `JsonReadOptions.MaxDepth` (default 64). |
| `Parse(string)` / `Parse(Stream)` / `Parse(ReadOnlySpan<byte>)` | `JsonNode` | RFC 8259. JSON null is not a document root. Stream honors the 32 MiB cap. |
| `Create` / `Open` / `OpenJsonl` / `OpenExport` | `JsonSession` | `Open` is one `.json` document; a `.jsonl` path is rejected. `Create("*.jsonl")` starts an empty list. |
| `WriteFile` | void | Collision default Fail. Atomic default true. 32 MiB document cap. |
| `Compare` | `JsonPatch` | Two payload paths of the same kind. JSONL compared as arrays. Diff only — no Apply. Also `JsonPatch.Compare(from, to)`. |
| `DefaultExportDirectory` / `NewExportPath` | string | Path only. Does not create the file. |
| `session.Get` / `TryGet` / `Set` | T / bool / void | `/a/b` and `a.b` are the same member. Get miss throws. Get and TryGet do not log. |
| `AppendRecord` / `Record` / `RecordCount` | void / node / int | JSONL only. |
| `Snapshot` / `Diff` / `Commit` / `Revert` / `Cancel` | void / `JsonPatch` / void | Disk sees committed only. |
| `Save` / `SaveAs` / `SaveWorking` | path string | `SaveWorking` writes working without Commit. |
| `JsonCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

Default export: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests override `JsonTestHooks.ExportRoot`. Probe never writes it.

## Rules that do not move

- RFC 8259 only. No comments, no trailing commas, no BOM.
- Pretty + camelCase for `.json`. JSONL Save is compact (one value + newline).
- Collision default Fail. No UniqueName here — call FileIo in the host.
- Object member compare is ordinal: `network.Timeout` and `network.timeout` are two keys. Hosts that want Windows-ish keys call FileIo UniqueName, not this package.
- Get of a missing path throws `KeyNotFoundException`. `TryGet` returns false. Get is quiet in the log.
- Never log payload bodies, field values, PEM, or `Exception` objects.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Json
    cfg.LogDirectory = logDir;
    JsonCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\\Vestigium\\Logs\\{host-APPID}\\`.

Named events live in `EventCatalog/json.json`. Event IDs are per subcategory in 13500–13999 (step 5), used through 13625.

## Related

Audit trail: `Vestigium.Logging`. Trees: `Vestigium.Helpers.FileIo`. Tables: `Vestigium.Helpers.Csv`.

Long-form documents live in [Vestigium.Documentation / Helpers / Json](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Json/004%20--Developers%20Guide) |
