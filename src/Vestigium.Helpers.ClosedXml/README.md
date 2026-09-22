# Vestigium.Helpers.ClosedXml

ClosedXML write-first Excel (`.xlsx`) for Vestigium hosts. One session owns one workbook. Sheets, tables, named ranges, pictures, and queued charts go through that session.

It does not spawn Excel. ClosedXML types stay behind the session. Charts paints WPF; this library writes workbooks.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.ClosedXml` 1.0.0 |
| TFM | `net10.0` |
| APPID | `ClosedXml` (`ClosedXmlCatalog.AppId`) |
| EVENTID | Reserved 11000–11499 (used through 11090) |
| Depends on | `ClosedXML` 0.105.1, `Vestigium.Helpers.Analytics`, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.ClosedXml" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;

var series = NumericSeries.From(rtts, "rtt-ms");
using var book = WorkbookHelper.Create("RTT", appId: "PingIQ");
WorkbookHelper.WriteSeries(book, series);
var path = book.SaveAs(WorkbookHelper.NewExportPath("PingIQ", "rtt"));
```

Default export folder: `Desktop\Vestigium\Exports\{appId}\`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `WorkbookHelper.Create(firstSheet?, appId?)` | `WorkbookSession` | New in-memory workbook. |
| `WorkbookHelper.Open(path, appId?)` | `WorkbookSession` | Existing `.xlsx`. |
| `WorkbookHelper.OpenOrCreate(path, …)` | `WorkbookSession` | Opens if present, else creates and saves. |
| `WorkbookHelper.WriteSeries(book, series)` | void | Analytics snapshot onto standard sheets. |
| `WorkbookHelper.NewExportPath(appId, stem?)` | `string` | Timestamped path under the export folder. |
| `book.Sheet(name)` | `SheetSession` | Get or add. Names are sanitized. |
| `sheet.WriteTable(...)` | void | Formula-like text is neutralized. Non-finite numbers rejected. |
| `book.AddChart(chart)` | void | Queued; embedded on `Save` / `SaveAs` when `IncludeCharts`. |
| `book.Save()` / `SaveAs(path)` / `SaveTo(stream)` | path or void | Creates parent directories. |
| `ClosedXmlCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Write-first `.xlsx`. This is not Microsoft Excel automation.
- ClosedXML is not thread-safe. One session, one owner.
- A workbook keeps at least one worksheet.
- Formula-looking cell text is neutralized. Non-finite numbers are rejected.
- Self-merge is rejected.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not ClosedXml
    cfg.LogDirectory = logDir;
    AnalyticsCatalog.Register(cfg);
    ClosedXmlCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`. Correlation id is `SessionId`.

Named events live in `EventCatalog/closedxml.json`.

## Related

Numbers: `Vestigium.Helpers.Analytics`. Disk log: `Vestigium.Logging`.

Long-form documents live in [Vestigium.Documentation / Helpers / ClosedXml](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/ClosedXml/004%20--Developers%20Guide) |
