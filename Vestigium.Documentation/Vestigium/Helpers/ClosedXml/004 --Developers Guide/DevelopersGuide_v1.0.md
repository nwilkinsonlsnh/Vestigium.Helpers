# Vestigium.Helpers.ClosedXml — Developers Guide

**Document ID:** VEST-HLP-CLOSEDXML-DEV-000  
**Version:** 1.1  
**Status:** Design companion to SRS v1.1 (write + charts + read + operator chrome + letterhead)  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.ClosedXml/`.

## Design

**Intent.** One way for every Vestigium host to dump reviewable data into a real `.xlsx`. Operators sort, filter, and stare at numbers without installing the app that produced them. The file must open in Excel and LibreOffice.

**Locked decisions.**

| Decision | Why |
|---|---|
| Write first, then read what we wrote | The read surface is a round-trip of our files, not a general importer. |
| CSV is a sibling | ClosedXML is `.xlsx` only. `Vestigium.Helpers.Csv` is the next lossless SRS. |
| Native Excel charts by zip splice | ClosedXML 0.105 cannot author charts. We inject OOXML after save. |
| ChartView is a demo host, not a library reference | Excel charts stay here. WPF preview uses Charts. Two drawing surfaces, one series. |
| Desktop export, ProgramData logs | Workbooks are for humans. JSONL is for the padlock logger. |
| Injection prefix on `= + - @` | Untrusted text never becomes a formula. |
| Table Design is Excel's gallery | Light / Medium / Dark names. Default Medium 2. No kitchen-sink `IXLStyle`. |

**Gap that drives v2.2.** Opening a charted file with `WorkbookHelper.Open` and `Save` writes tables only — chart parts are injected on the way out. Preserve-on-round-trip is the next real Excel problem.

## Contract

[`Requirements_v1.0.md`](Requirements_v1.0.md) is write-first. No CSV in this project.

ClosedXML 0.105 cannot create charts. This library still ships them: after ClosedXML writes the tables we splice native Excel chart parts into the `.xlsx` zip so Excel and LibreOffice render them.

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

## Logging (Vestigium.Logging)

The padlock **Logging** folder in the solution *is* the engine. ClosedXml does not call `VestigiumLogger.Initialize` and does not write files itself.

Call path:

`WorkbookHelper` / `WorkbookSession` / `SheetSession` → `HelperLog.Write` → `VestigiumLog.Write` → JSONL under `%ProgramData%\Vestigium\Logs\{APPID}\`.

- F5 `Vestigium.Helpers.ClosedXml.Demo` (that process initializes logging with APPID `ClosedXml`).
- Then open `C:\ProgramData\Vestigium\Logs\ClosedXml\vestigium-ClosedXml-*.json`.
- You should see Debug `enter Create` / `enter WriteAt`, Information `Wrote sheet=...`, and on failure Error `reject` or `failed` with the exception text.

If you run ClosedXml from a unit test or a console that never called `HelperLog.InitializeHost`, the library still throws — it just has nowhere to write. That is the host contract, not a missing reference.

Tests must `SaveAs` a temp path. Never `Save()` onto the real Desktop from xUnit.

## Dump a NumericSeries

```csharp
WorkbookHelper.WriteSeries(book, series, populationSize: 100_000);
```

Sheets: Summary, Charts, Bands, Confidence, Histogram, Sample. Prefix them with the third argument if one workbook holds several series.

The Charts sheet is a dashboard: mean confidence table plus four Excel charts (histogram, band means, mean CI, sample line). Histogram also gets a chart on its own sheet. Set `book.IncludeCharts = false` before `WriteSeries` to skip chart parts.

ClosedXML cannot round-trip charts. Opening a charted file with `WorkbookHelper.Open` and `Save` writes tables only — chart parts are injected on the way out.

## Read what we wrote

```csharp
using var book = WorkbookHelper.Open(path, HelperLog.AppIds.ClosedXml);
var table = book.Sheet("Summary").ReadUsedRange();
// table.Headers / table.Rows — cells guessed as number, text, bool, or DateTime
```

This is a round-trip of files this helper wrote. It is not a general importer for arbitrary accounting workbooks.

## Operator chrome

WriteTable (default on) applies:

- Print: landscape, fit-to-width, footer `APPID` + Excel date/time
- Column formats from header names: `ms` → `0.0`, `pct` → `0.00%`, `utc` / `timestamp` → `yyyy-mm-dd hh:mm:ss`
- Optional single conditional format: `HighlightColumn` + `HighlightGreaterThan` (WriteSeries uses Sample `Value` > P95)

```csharp
book.Sheet("Rtt").WriteTable(table, new SheetWriteOptions
{
    HighlightColumn = "ms",
    HighlightGreaterThan = 80
});
book.ReorderSheets("Summary", "Charts", "Sample");
book.MoveSheet("Sample", 2);
```

Pass `OperatorPrint = false` to skip the print footer.

## Letterhead, pictures, merge

Not a token template engine. Open the operator's `.xlsx`, fill a named range or a reserved sheet, stamp a logo, merge another dump by sheet name.

```csharp
using var letterhead = WorkbookHelper.OpenTemplate(path, HelperLog.AppIds.ClosedXml);
letterhead.WriteNamedRange("Data", table);          // origin of the defined name; chrome around it stays
letterhead.AddPicture("Letterhead", logoPng, row: 1, column: 4, widthPx: 120, heightPx: 36);
letterhead.Save();

using var target = WorkbookHelper.Open(left, HelperLog.AppIds.ClosedXml);
using var source = WorkbookHelper.Open(right, HelperLog.AppIds.ClosedXml);
WorkbookHelper.Merge(target, source);               // matching sheets append; unknown sheets copy
```

`DefineName("Data", "Letterhead", 5, 1, 20, 4)` marks the fill if the letterhead does not already have one. Merge is append-only: a sheet that exists only on the target is never deleted.

`AddChart` now accepts `ChartKind.Pie` and `ChartKind.Scatter` in addition to column / bar / line. WriteSeries puts a histogram pie and a sample scatter on the Charts sheet.

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
| `WorkbookHelper.cs` | Identity, Probe, Create/Open/OpenTemplate/Save paths, WriteSeries, Merge |
| `WorkbookSession.cs` | Owns `XLWorkbook`; MoveSheet / ReorderSheets / named ranges / pictures / merge |
| `SheetSession.cs` | WriteTable / WriteAt / AppendRows / ReadUsedRange / chrome / AddPicture |
| `SheetTable.cs` | Headers + rows + write / read options (including `Letterhead`) |
| `CellWriter.cs` | Types + formula-injection prefix |
| `CellReader.cs` | Typed guess: number, text, bool, DateTime |
| `HeaderFormats.cs` | ms / pct / utc from header names |
| `SeriesWorkbook.cs` | Analytics dump: Summary, Charts, Bands, Confidence, Histogram, Sample |
| `ExcelTableStyles.cs` | Excel Table Design gallery (Light / Medium / Dark) |
| `ExcelTableStylePreview.cs` | Header / band / band-alt chips for the WPF gallery |
| `SheetChart.cs` | Chart spec (column / bar / line / pie / scatter) |
| `ChartPacker.cs` | Injects OOXML chart + drawing parts after ClosedXML save |

## Demo

`dotnet run --project src/Vestigium.Helpers.ClosedXml.Demo` opens the WPF gallery (same chrome as Vestigium.Logging). **Write** is Excel's Light / Medium / Dark Table Design chips (header + band + band-alt) plus a live sample. **Write workbook** dumps the Analytics sample to Desktop. **Charts** hosts `Vestigium.Helpers.Charts` (`ChartView.Histogram` / `Line` / `Pie` / `Scatter` / `Box`) as a preview; Excel still gets native OOXML charts on save — this library does not reference Charts. **Read** reopens that file through `ReadUsedRange`. **Chrome** shows print, header formats, the P95 highlight, and sheet order. **Template** fills named range `Data`, stamps a logo, and merges two workbooks.

Workbook: `%DESKTOP%\Vestigium\Exports\ClosedXml\vestigium-ClosedXml-{stamp}.xlsx`  
JSONL: `%ProgramData%\Vestigium\Logs\ClosedXml\`

## CSV

That work is `Vestigium.Helpers.Csv`. Do not add a CSV parser here. Csv is the next library to leave skeleton — see that project's SRS.

## Roadmap

Shipped through v2.1 (write, Excel charts, read-back, chrome, letterhead, pictures, merge, pie/scatter, Table Design, demo ChartView host).

Next (SRS §15.2):

1. **Preserve chart parts on Open + Save** — the round-trip currently drops them.
2. **Hyperlinks** and **cell comments**.
3. **Data validation** (dropdown lists) and **read named ranges**.

Never: CSV, `.xls`, VBA, pivots, sparklines, token templates.
