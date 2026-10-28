# Vestigium.Helpers.ClosedXml — Design

**Document ID:** VEST-HLP-CLOSEDXML-DSN-000  
**Version:** 1.0  
**Status:** Locked companion to SRS v1.1  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records *why* ClosedXml is shaped this way. It does not add requirements.

---

## 1. Intent

Give every Vestigium host one way to dump a reviewable `.xlsx` without installing Excel and without learning ClosedXML. The public types are `WorkbookHelper`, `WorkbookSession`, `SheetSession`, and `SheetTable`. `XLWorkbook` stays owned by the session.

```
host buffer / NumericSeries
    → WorkbookHelper.Create / Open / OpenOrCreate
         ├ Sheet(name).WriteTable / AppendRows / ReadUsedRange
         ├ WriteSeries(book, series)     Analytics snapshot → standard sheets
         ├ AddChart / named ranges / pictures / Merge
         └ Save / SaveAs / SaveTo        optional OOXML chart pack on the way out
Operators open the file in Excel or LibreOffice. The producing app is optional.
```

`Vestigium.Helpers.Charts` is a WPF preview. This library does not reference it. Native Excel charts are injected as OOXML after ClosedXML writes the zip.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Write-first `.xlsx` | Operators need a file they can sort. Arbitrary workbook ingest is a different product. |
| Target the file, not a vendor | Office Open XML. Excel, LibreOffice, Sheets all open it. No Interop. |
| One session owns one `XLWorkbook` | ClosedXML is not thread-safe. Dispose owns the book. |
| Facade + session, not a static bag | `WorkbookHelper` keeps Identity / Probe / path helpers. Work lives on `WorkbookSession`. |
| `SheetTable` is the payload | Avoid a second ORM. Headers + rows. Typed cells on write; guess on read. |
| Sanitize names, neutralize formulas | Sheet names 1–31 legal chars. Leading `= + - @` become text. Log the neutralize. |
| Reject non-finite numbers | Do not write `#NUM!` by accident. |
| Desktop exports, ProgramData logs | Humans get `Desktop\Vestigium\Exports\{host-APPID}`. JSONL stays under `%ProgramData%`. |
| Tests never touch the real Desktop | `SaveAs(temp)`. Path helpers may compute Desktop; they must not create it until Save. |
| Charts are a post-save pack | ClosedXML cannot author `c:barChart`. `ChartPacker` injects parts. `IncludeCharts = false` skips. |
| Charts do not survive Open + Save yet | ClosedXML round-trip drops injected parts. SRS §15.2 v2.2. Do not pretend they persist. |
| No Charts NuGet from this project | Demo may host `ChartView`. Library stays `net10.0`. |
| CSV is a sibling | `Vestigium.Helpers.Csv`. Neither library parses the other format. |
| Logging APPID stamps library identity | Folder follows the host. `SessionId` is the correlation id. Never `Initialize`. |

---

## 3. Shape

| File | Role |
|---|---|
| `WorkbookHelper.cs` | Identity, Probe, Create / Open / OpenOrCreate, paths, `WriteSeries`, Merge door |
| `WorkbookSession.cs` | Owns `XLWorkbook`, sheets, names, pictures, chart queue, Save / Pack |
| `SheetSession.cs` | WriteTable, AppendRows, ReadUsedRange, chrome, pictures |
| `SheetTable.cs` | Headers + rows payload |
| `CellWriter.cs` | CLR → cell; neutralize; reject non-finite |
| `CellReader.cs` | Used-range → typed guess |
| `HeaderFormats.cs` | `ms` / `pct` / `utc` from header names |
| `ExcelTableStyles.cs` | Light / Medium / Dark gallery ids |
| `ExcelTableStylePreview.cs` | Demo / gallery colors |
| `SheetNames.cs` | Sanitize sheet and defined names |
| `SeriesWorkbook.cs` | Analytics snapshot → Summary / Charts / Bands / Confidence / Histogram / Sample |
| `SheetChart.cs` | Queued native-chart description |
| `ChartPacker.cs` | Inject OOXML chart parts after ClosedXML save |
| `ClosedXmlLog` / `ClosedXmlCatalog` / `ClosedXmlEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Write vs pack vs paint

| This library writes | This library packs on Save | Sibling Charts paints |
|---|---|---|
| Values, tables, chrome | `xl/charts/chart*.xml` + anchors | `FrameworkElement` on a WPF form |
| `WriteSeries` sheets | Column / bar / line / pie / scatter | Histogram, Control, Ecdf, … |
| Named ranges, pictures | Only on the way *out* | Does not touch `.xlsx` |

Do not compute UCL / P95 / capability here. Call Analytics, then write the numbers.

---

## 5. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` | Required reference is null. |
| `ArgumentException` | Blank path / appId / sheet for a chart; empty chart series; self-merge; inverted named-range corners. |
| `ArgumentOutOfRangeException` | Non-finite number; sheet position < 1. |
| `FileNotFoundException` | `Open` on a missing file. Do not silently create. |
| `KeyNotFoundException` | Missing sheet or named range. |
| `InvalidOperationException` | Last-sheet delete; empty named range; cannot allocate a unique sheet name; disposed session. |
| `ObjectDisposedException` | Use after `Dispose`. |

`OpenOrCreate` is the only door that creates when the path is missing.

---

## 6. What closed to reach 1.0 (library as shipped)

SRS versions on the Requirements page. Code on `main` already includes the write path plus later items that landed in the same tree:

| SRS | Outcome |
|---|---|
| v1.0 | Session, `SheetTable` write, chrome, Desktop export, injection guard, `WriteSeries`, Identity / Probe |
| v1.1 | Native Excel charts on `WriteSeries`, Charts sheet, `IncludeCharts` |
| v1.2 | `ReadUsedRange`, typed cell guess |
| v1.3 | Print chrome, header-name formats, P95 highlight, `ReorderSheets` / `MoveSheet` |
| v2.0 | `OpenTemplate` alias, named ranges, pictures, merge by sheet name |
| v2.1 | `AddChart` pie + scatter, Table Design gallery |

This Design describes that shipped shape. It does not reopen those rows.

---

## 7. Still out

Preserve chart parts on Open + Save, hyperlinks, cell comments, data-validation lists, read named ranges, password-protected workbooks, CSV, `.xls`, VBA, pivots, Power Query, slicers, sparklines, ClosedXML.Report tokens, streaming million-row writers, thread-safe sharing, writes to `%ProgramData%`.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Sep 2026 | First standalone Design. Content lifted from SRS v1.1 + shipped session/packer shape. |
