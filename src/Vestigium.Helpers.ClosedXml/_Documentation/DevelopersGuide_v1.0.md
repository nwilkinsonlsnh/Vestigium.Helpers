# Vestigium.Helpers.ClosedXml — Developers Guide

**Document ID:** VEST-HLP-CLOSEDXML-DEV-000  
**Version:** 1.0  
**Status:** Companion to SRS v1.0 (write engine shipped)  
**Date:** 7 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.ClosedXml/`.

## Contract

[`Requirements_v1.0.md`](Requirements_v1.0.md) is write-first. No CSV in this project. Charts stay out.

## Write a table

```csharp
using Vestigium.Helpers;
using Vestigium.Helpers.ClosedXml;

using var book = WorkbookHelper.Create("Summary", HelperLog.AppIds.ClosedXml);
book.Sheet("Summary").WriteTable(SheetTable.Create(
    ["Name", "Value"],
    [
        ["alpha", 1],
        ["beta", 2]
    ]));
var path = book.Save(); // %DESKTOP%\Vestigium\Exports\ClosedXml\
```

Tests must `SaveAs` a temp path. Never `Save()` onto the real Desktop from xUnit.

## Dump a NumericSeries

```csharp
WorkbookHelper.WriteSeries(book, series, populationSize: 100_000);
```

Sheets: Summary, Bands, Confidence, Histogram, Sample. Prefix them with the third argument if one workbook holds several series.

## Open and append

```csharp
using var book = WorkbookHelper.Open(path, HelperLog.AppIds.PingIQ);
book.Sheet("Samples").AppendRows([[rttMs, DateTimeOffset.UtcNow]]);
book.Save();
```

Missing files throw `FileNotFoundException`. Use `OpenOrCreate` if the first run should mint the workbook.

## Chrome

Default `SheetWriteOptions`: bold header, freeze row 1, autofilter or an Excel table, autosize capped at 40, tab color when you set `TabColor`.

Table coloring is Excel's own gallery. Set `book.TableStyle = "Medium9"` (or `Light1`–`Light21`, `Medium1`–`Medium28`, `Dark1`–`Dark11`). That maps to `XLTableTheme.TableStyleMedium9` and shows up under Table Design when the file opens. Default is **Medium 2**, Excel's default blue. Pass `tableStyle:` on `WriteSeries` to stamp every Analytics sheet.

```csharp
book.TableStyle = "Dark7";
WorkbookHelper.WriteSeries(book, series, populationSize: 100_000, tableStyle: "Medium2");
```

Text that starts with `=`, `+`, `-`, or `@` is stored with a leading apostrophe. The library never evaluates caller formulas.

NaN and Infinity throw. Empty series from Analytics never reach this helper — Analytics already rejects them.

## Files

| File | Role |
|---|---|
| `WorkbookHelper.cs` | Identity, Probe, Create/Open/Save paths, WriteSeries |
| `WorkbookSession.cs` | Owns `XLWorkbook` |
| `SheetSession.cs` | WriteTable / AppendRows / chrome |
| `SheetTable.cs` | Headers + rows + write options |
| `CellWriter.cs` | Types + formula-injection prefix |
| `SeriesWorkbook.cs` | Analytics dump: Summary, Bands, Confidence, Histogram, Sample |
| `ExcelTableStyles.cs` | Excel Table Design gallery (Light / Medium / Dark) |

## Demo

`dotnet run --project src/Vestigium.Helpers.ClosedXml.Demo`

Workbook: `%DESKTOP%\Vestigium\Exports\ClosedXml\vestigium-ClosedXml-{stamp}.xlsx`  
JSONL: `%ProgramData%\Vestigium\Logs\ClosedXml\`

## CSV

That work is `Vestigium.Helpers.Csv`. Do not add a CSV parser here.
