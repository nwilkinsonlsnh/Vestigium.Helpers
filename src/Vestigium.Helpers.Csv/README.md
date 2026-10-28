# Vestigium.Helpers.Csv

RFC 4180 CSV / TSV for Vestigium hosts. One session owns one rectangle. ClosedXml stays `.xlsx`. This package is delimited text only.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Csv` 1.0.0 |
| TFM | `net10.0` |
| APPID | `Csv` (`CsvCatalog.AppId`) |
| EVENTID | Reserved 11500–11999 (used through 11560) |
| Depends on | `Vestigium.Helpers.Analytics`, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Csv" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Csv;

using var file = CsvHelper.Create("PingIQ");
file.WriteTable(CsvTable.Create(["ms", "utc"], [[12.4, at], [11.9, at]]));
var path = file.SaveAs(CsvHelper.NewExportPath("PingIQ", "rtt"));
```

TSV: pass `CsvOptions.Tab`. Default write is UTF-8 with BOM so Excel sees Unicode.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `CsvHelper.Create(appId?, options?)` | `CsvSession` | Empty table until `WriteTable`. |
| `CsvHelper.Open(path, …)` | `CsvSession` | Missing file throws. |
| `CsvHelper.OpenOrCreate(path, …)` | `CsvSession` | Creates only when the path is missing. |
| `CsvHelper.WriteTable(table, path)` | `string` | One-shot write. |
| `CsvHelper.Read(path)` | `CsvTable` | Round-trip of a file this library wrote. |
| `CsvHelper.WriteSeries(file, series)` | void | Sample dump: index, value, timestamp. |
| `file.WriteTable` / `AppendRows` / `Read` | table or void | Append needs a table first. |
| `file.Save` / `SaveAs` / `WriteTo` | path or void | Creates parent directories. |
| `CsvCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

Dialects: `CsvOptions.Rfc4180`, `.Tab`, `.Semicolon`, `.Pipe`.

## Rules that do not move

- Delimited text only. No ClosedXML. No multi-sheet CSV.
- Quote is `"`. Write CRLF. Read CR / LF / CRLF.
- Leading `= + - @` are neutralized. Non-finite numbers are rejected.
- Delimiter cannot be quote, CR, LF, NUL, or a surrogate.
- One session, one owner. The library never calls `VestigiumLogger.Initialize`.
- Tests never touch the real Desktop.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Csv
    cfg.LogDirectory = logDir;
    CsvCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`. Correlation id is `SessionId`.

Named events live in `EventCatalog/csv.json`.

## Related

Workbook sibling: `Vestigium.Helpers.ClosedXml`. Numbers: `Vestigium.Helpers.Analytics`.

Long-form documents live in [Vestigium.Documentation / Helpers / Csv](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Csv/004%20--Developers%20Guide) |
