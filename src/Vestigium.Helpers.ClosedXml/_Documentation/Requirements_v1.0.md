# Vestigium.Helpers.ClosedXml — Requirements Specification

**Document ID:** VEST-HLP-CLOSEDXML-SRS-000  
**Version:** 1.0  
**Status:** Accepted (write-first; implementation follows this document)  
**Date:** 7 September 2026  
**Package:** `Vestigium.Helpers.ClosedXml`  
**Engine:** ClosedXML 0.105.1 (or the current suite pin)  
**TFM:** `net10.0` (not Windows-only)

Companion: [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

---

## 1. Purpose

Give every Vestigium host (PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost, Analytics demos, future tools) one way to dump reviewable data into a real Excel workbook.

The file must open in Microsoft Excel and in LibreOffice / OpenOffice. The format is Office Open XML (`.xlsx`). This library does not target a vendor. It targets the file.

Operators should be able to sort, filter, and stare at numbers without installing the Vestigium app that produced them.

v1 builds the **write** path. After write is solid, v1.1 reads back what this library wrote. That is how we learn the read surface — by round-tripping our own files, not by ingesting arbitrary operator workbooks first.

---

## 2. Decisions locked in this version

These came from the 7 September 2026 design pass.

| # | Decision | Locked as |
|---|---|---|
| 1 | Consumers | Any Vestigium application. First concrete users: Analytics demo, then PingIQ-style sample dumps. The library stays generic so a new host does not need a new Excel helper. |
| 2 | CSV | **Out of this library.** A sibling `Vestigium.Helpers.Csv` skeleton exists for that work. ClosedXML is `.xlsx` / `.xlsm` only. |
| 3 | Starting point | Create a **blank** workbook, **or** open an existing `.xlsx` and append sheets / rows. See §6. |
| 4 | Read vs write | **Write first.** Then read what we wrote. Do not design a general Excel importer in v1. |
| 5 | Demo | `Vestigium.Helpers.ClosedXml.Demo` uses `Vestigium.Helpers.Analytics` to mint a numeric series and writes several worksheets. |
| 6 | Default export folder | `%USERPROFILE%\Desktop\Vestigium\Exports\{APPID}\` on Windows. Caller may pass any other path. Tests never use the Desktop. |
| 7 | Logging | `HelperLog` only. Libraries never call `VestigiumLogger.Initialize`. APPID = `ClosedXml`. Debug enter on public session/sheet methods. Error then throw on guard failures. `SessionId` on every line. |
| 8 | Charts | Native Excel charts on WriteSeries (v1.1). ClosedXML cannot author them; the helper writes OOXML chart parts after save. |

---

## 3. What "branded workbook" meant (clarification)

It did **not** mean "Microsoft Excel file vs OpenOffice file."

ClosedXML reads and writes **Office Open XML**. That is the `.xlsx` zip package. Excel, LibreOffice, and Google Sheets all open it. There is no separate "Open Office mode."

"Branded" meant: start from a workbook someone already styled (logo in row 1, company colors, frozen header, print footer) and fill data below or on a new sheet.

v1 does **not** implement a template engine (no token replacement, no ClosedXML.Report). v1 only:

- `Create()` — empty workbook, our chrome.
- `Open(path)` — existing `.xlsx`, append or replace sheets we own.

If a later milestone needs "open `Vestigium-Letterhead.xlsx` and fill named ranges," that is roadmap v2. It is still `.xlsx`, not a different office suite.

`.xls` (Excel 97-2003 BIFF) is out of scope forever in this library. ClosedXML cannot write it.

`.xlsm` may be opened and saved if the caller must preserve an existing file that already has macros. This library will not add, edit, or run VBA.

---

## 4. Goals

**G1.** One disposable session owns one `XLWorkbook`.  
**G2.** Write a rectangular table (headers + rows) onto a named sheet.  
**G3.** Write several sheets in one workbook (summary, raw samples, bands, histogram, confidence).  
**G4.** Save to a caller path or to the default Desktop export folder.  
**G5.** Apply a small, repeatable chrome set: header bold, autofilter, freeze top row, autosize with a cap, tab color.  
**G6.** Neutralize formula injection when writing untrusted text.  
**G7.** Log Pending / Success / Failed through `HelperLog`.  
**G8.** Keep `WorkbookHelper.Identity` and `WorkbookHelper.Probe()` so existing smoke tests stay green.  
**G9.** After write works, read the used range of a sheet this library wrote and return a `SheetTable`.

---

## 5. Non-goals (v1)

| Item | Why |
|---|---|
| CSV / TSV parse or write | Sibling `Vestigium.Helpers.Csv`. |
| `.xls` | ClosedXML does not support it. |
| Macro authoring | We are not a VBA IDE. |
| Pivot caches, Power Query, slicers | Different product. |
| Excel charts | **Shipped.** `WriteSeries` embeds column / line charts. ClosedXML does not author them — we inject chart parts. `IncludeCharts = false` turns them off. |
| Sparklines | Still out. |
| Conditional-formatting rule designer | One "highlight high outliers" rule may land in v1.2, not v1.0. |
| POCO / attribute mapping | Avoid a second ORM. Callers pass `SheetTable`. |
| ClosedXML.Report token templates | v2. |
| Streaming 1M-row writers | ClosedXML loads the package. Huge extracts need another stack. |
| Thread-safe workbook sharing | ClosedXML is not thread-safe. One session, one owner. |
| Password-protected workbooks | Out of v1. |
| Writing to `%ProgramData%` | Exports are for humans. Logs stay in ProgramData. Workbooks go to Desktop (or a caller path). |

---

## 6. Create vs open vs append

```
WorkbookSession.Create(name?)
    new XLWorkbook()
    optional first sheet

WorkbookSession.Open(path)
    existing .xlsx
    does not delete sheets the caller did not ask to replace

SheetSession.ReplaceTable(table)
    used range of that sheet is overwritten

SheetSession.AppendRows(rows)
    writes after the current last used row
    header row is left alone if it already exists
```

Append is "add rows to this sheet" or "add a new sheet to this workbook." It is not "merge two workbooks."

If `Open` is given a missing file, throw `FileNotFoundException`. Do not silently create. Callers who want create-or-open use `OpenOrCreate(path)`.

---

## 7. Default export location

```
Windows:  Environment.GetFolderPath(SpecialFolder.Desktop)
          \Vestigium\Exports\{APPID}\
Example:  C:\Users\Wilkinson\Desktop\Vestigium\Exports\ClosedXml\
          C:\Users\Wilkinson\Desktop\Vestigium\Exports\PingIQ\
```

Rules:

- `{APPID}` is the **host** APPID when a Vestigium app calls the helper (PingIQ, DnsIQ, ...). The ClosedXml demo uses `ClosedXml`.
- The helper creates the directory if it is missing (on `Save` / `SaveAs`, not when you only ask for the path).
- File name default: `vestigium-{APPID}-{yyyyMMdd-HHmmss}.xlsx` so runs do not clobber each other.
- Caller path always wins over the default.
- Tests pass a temp directory. The library must not touch the real Desktop from xUnit.
- Non-Windows: if `SpecialFolder.Desktop` is empty, fall back to `Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), "Desktop", "Vestigium", "Exports", appId)`. If that is still unusable, require an explicit path.

Logs remain:

```
%ProgramData%\Vestigium\Logs\{APPID}\vestigium-{APPID}-*.json
```

Exports and logs are different trees on purpose.

---

## 8. Public surface (v1 write)

Names may move a token during implementation. The shapes may not.

### 8.1 Facade (keeps the suite contract)

```csharp
public static class WorkbookHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.ClosedXml"
    public static string Probe();            // Pending + Success via HelperLog

    public static string DefaultExportDirectory(string appId);
    public static string NewExportPath(string appId, string? stem = null);

    public static WorkbookSession Create(string? firstSheetName = null, string? appId = null);
    public static WorkbookSession Open(string path, string? appId = null);
    public static WorkbookSession OpenOrCreate(string path, string? firstSheetName = null, string? appId = null);

    public static void WriteSeries(WorkbookSession book, NumericSeries series, string? prefix = null, int? populationSize = null, string? tableStyle = null);
}
```

`appId` stamps `HelperLog` and the default `Save()` folder. Tests always pass a temp path to `SaveAs`.

### 8.2 Session

```csharp
public sealed class WorkbookSession : IDisposable
{
    public IReadOnlyList<string> SheetNames { get; }
    public string TableStyle { get; set; }           // Light/Medium/Dark gallery; default Medium2
    public bool IncludeCharts { get; set; }          // default true; native Excel charts on save
    public void AddChart(SheetChart chart);
    public IReadOnlyList<SheetChart> Charts { get; }
    public SheetSession Sheet(string name);          // get or create
    public SheetSession AddSheet(string name);
    public bool RemoveSheet(string name);

    public string Save();                            // default Desktop path, current host APPID
    public string SaveAs(string path);
    public void SaveTo(Stream stream);
}
```

`Save()` without a path is allowed only when the session knows an APPID (set on create, or taken from `HelperLog` host if initialized). Tests always call `SaveAs(tempPath)`.

### 8.3 Sheet

```csharp
public sealed class SheetSession
{
    public string Name { get; }
    public void WriteTable(SheetTable table, SheetWriteOptions? options = null);
    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows, SheetWriteOptions? options = null);
    public void ApplyChrome(SheetChrome chrome);
}
```

v1.1 adds:

```csharp
    public SheetTable ReadUsedRange(SheetReadOptions? options = null);
```

`WriteTable` replaces the used range of that sheet (G2 / ReplaceTable).

### 8.4 Table

```csharp
public sealed class SheetTable
{
    public IReadOnlyList<string> Headers { get; init; }
    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
    public string? Name { get; init; }          // Excel table name when CreateExcelTable is on
}
```

Cell values allowed on write:

| CLR | Excel |
|---|---|
| `null` | blank |
| `string` | text (injection-safe, §11) |
| `bool` | boolean |
| integral / `decimal` / `double` / `float` | number (reject NaN / Infinity) |
| `DateTime` / `DateTimeOffset` | datetime (UTC offset written as UTC `DateTime`) |
| `TimeSpan` | number of days, formatted `[h]:mm:ss` when chrome asks |

Anything else is `ToString()` as text, then injection-safe.

### 8.5 Options

```csharp
public sealed class SheetWriteOptions
{
    public bool HasHeaderRow { get; init; } = true;
    public bool CreateExcelTable { get; init; } = true;
    public bool Autosize { get; init; } = true;
    public double AutosizeMaxWidth { get; init; } = 40;
    public bool FreezeHeader { get; init; } = true;
    public bool AutoFilter { get; init; } = true;
    public string? NumberFormat { get; init; }     // applied to numeric body cells
    public string? DateFormat { get; init; }
    public string? TabColor { get; init; }         // #RRGGBB or RRGGBB
    public string? TableStyle { get; init; }       // Light1–21, Medium1–28, Dark1–11, None
}
```

Do not leak a kitchen-sink `IXLStyle` through the helper. Table coloring uses Excel's built-in `TableStyleLight*` / `TableStyleMedium*` / `TableStyleDark*` names via `TableStyle` / `WorkbookSession.TableStyle`. Default is Medium2 (Excel's default). If a host needs a one-off cell style, it is not this library's problem in v1.

---

## 9. Analytics bridge (demo + optional helper)

The ClosedXml **library** may reference Analytics. The ClosedXml **demo** will.

v1 demo workbook sheets (minimum):

| Sheet | Contents |
|---|---|
| `Summary` | n, min, Q1, median, Q3, max, mean, stddev, skew, excess kurtosis, P90, P95, P99 |
| `Charts` | Mean confidence table plus four Excel charts (histogram, band means, mean CI, sample). Histogram also carries a chart on its own sheet. |
| `Bands` | Full / Q1 / Q2 / Q3 / Q4 / IQR with n, min, P50, max, mean, stddev, skew, excess kurtosis |
| `Confidence` | level, parameter, estimate, lower, upper, width, method for 90 / 95 / 99 and FPC mean |
| `Histogram` | bin index, lower, upper, count, relative frequency |
| `Sample` | observation index, value, timestamp |

A `WorkbookHelper.WriteSeries(WorkbookSession, NumericSeries, string? prefix = null)` should land in v1.0 so PingIQ does not copy the demo sheet layout.

---

## 10. Demo contract

`Vestigium.Helpers.ClosedXml.Demo` is a WPF gallery (`HelperWpfHost.Start`, APPID `ClosedXml`):

1. Draw an Analytics series (crypto sample: 1,000 unique integers from 1..100,000).
2. **Write workbook** dumps the sheets in §9 to `%DESKTOP%\Vestigium\Exports\ClosedXml\vestigium-ClosedXml-{stamp}.xlsx`.
3. Table Design style defaults to Medium 2; the gallery ComboBox lists Excel's Light / Medium / Dark names.
4. **Charts** previews the four series that land as native Excel charts (injected OOXML after ClosedXML save).
5. **Inject** shows that leading `= + - @` become text.
6. JSONL still goes to `%ProgramData%\Vestigium\Logs\ClosedXml\`.

The demo references Analytics. The library may as well. Core `Vestigium.Helpers` does not reference ClosedXml or Analytics.

---

## 11. Safety

- Sheet names: 1-31 characters, not `[]:*?/\`. Helper sanitizes or throws. Empty becomes `Sheet1`.
- Path: reject blank paths. `SaveAs` creates the parent directory.
- Formula injection: if a string value starts with `=`, `+`, `-`, or `@`, prefix `'` so Excel stores text. Log Verbose that a value was neutralized.
- Numeric non-finites throw. Do not write `#NUM!` by accident.
- Do not evaluate attacker-supplied formulas. v1 write puts values, not formulas. No `AllowFormulas` switch in v1.0.
- `Dispose` disposes the `XLWorkbook`.

---

## 12. Logging

| Event | Level | Status |
|---|---|---|
| Session create / open | Information | Pending |
| Table written (sheet, rows, cols) | Information | Success |
| Save path | Information | Success |
| Injection neutralized | Verbose | Success |
| Missing file on Open | Error | Failed |
| Save failure | Error | Failed |

Category = `Helpers`. Subcategory = `ClosedXml`. APPID = host APPID (demo: `ClosedXml`).

---

## 13. Tests

xUnit, serial logger collection, temp directories only.

v1.0:

- `Identity` is `Vestigium.Helpers.ClosedXml`.
- `Probe` writes Pending then Success when the host is initialized.
- Create -> `WriteTable` -> `SaveAs(temp)` produces a file whose length is > 0 and that ClosedXML can reopen.
- Known table (headers `Name,Value` and two rows) is checked by reopening with ClosedXML and asserting A1 / A2.
- Default export path helper returns a folder ending in `Vestigium\\Exports\\{appId}` and does not create it until Save.
- Injection: write `=1+1` as a string, reopen, cell is text not a computed `2`.
- NaN rejected.

v1.1 charts (this drop):

- `WriteSeries` zip contains `xl/charts/chart*.xml` (`c:barChart` / `c:lineChart`) and drawing anchors.
- Charts worksheet sits at position 2.
- `IncludeCharts = false` skips the Charts sheet and chart parts.

v1.2 adds read-back equality tests on the Analytics demo shape (summary row count, histogram bin count).

v1.3 adds print chrome, header-name formats (`ms` / `pct` / `utc`), one greater-than highlight, and `ReorderSheets` / `MoveSheet`.

---

## 14. Dependencies

| Package | Role |
|---|---|
| ClosedXML 0.105.1 (suite pin) | Engine |
| Vestigium.Helpers | Guards, HelperLog |
| Vestigium.Helpers.Analytics | Allowed on the library; required on the demo |

No EPPlus. No Excel Interop. No Microsoft.Office.Interop.Excel. Those require Excel installed.

---

## 15. Roadmap

### v1.0 — Write (this document)

- Session create / open / open-or-create / dispose
- `SheetTable` write
- Chrome: header, freeze, autofilter, autosize cap
- Desktop export path
- Injection guard
- Demo writes Analytics across several sheets
- Tests against a known workbook via ClosedXML reopen

### v1.1 — Charts (this drop)

- Native Excel column / line charts on `WriteSeries`
- Charts sheet dashboard (histogram, band means, mean CI, sample)
- Histogram sheet local chart
- `WorkbookSession.IncludeCharts` (default true)
- Still no sparklines, pivot charts, or a separate Charts helper library

### v1.2 — Read what we wrote (this drop)

- `ReadUsedRange` -> `SheetTable`
- Typed cell guess: number, text, bool, DateTime
- Round-trip tests: write demo shape, read, compare counts and a handful of values
- Still not a general "any Excel file from accounting" importer

### v1.3 — Operator chrome (this drop)

- Print: landscape, fit-to-width, footer with APPID + timestamp
- Column number formats per header name (`ms`, `pct`, `utc`)
- Optional single conditional-format on a named column (high outliers)
- Sheet order helper

### v2.0 — Templates and pictures (this drop)

- Open a caller-supplied letterhead `.xlsx` and write into a named range or a reserved sheet
- Embed a logo image at a fixed cell
- Multiple workbooks merged by sheet name (append-only)

### v2.1 — Chart builder API (this drop)

- Public `AddChart` is already on the session. This drop grows kinds (pie, scatter) without a separate package.

### Explicitly never here

- CSV (see `Vestigium.Helpers.Csv`)
- `.xls`
- VBA authoring
- Database / DataTable first-class API

---

## 16. Sibling: Vestigium.Helpers.Csv

Created as a **skeleton** in the same change set as this SRS:

- `src/Vestigium.Helpers.Csv`
- `src/Vestigium.Helpers.Csv.Demo`
- `HelperLog.AppIds.Csv`
- Placeholder `CsvHelper.Identity` + `Probe()`
- Skeleton SRS until that library gets its own lossless pass

Csv must not reference ClosedXml. ClosedXml must not parse CSV. A later "sheet <-> csv dump" may live as a one-pager that uses both, in a host, not inside either library.

---

## 17. Glossary

| Term | Meaning |
|---|---|
| Workbook | One `.xlsx` package |
| Sheet | One tab |
| Used range | Smallest rectangle ClosedXML reports as having content |
| Append | Add rows or add a sheet; do not delete foreign sheets |
| Chrome | Header/filter/freeze/autosize that make the file usable |
| Injection | Spreadsheet formula smuggling via a leading `=` in a text cell |
| APPID | Vestigium host id (`PingIQ`, `ClosedXml`, ...) |
| Desktop export | `%DESKTOP%\\Vestigium\\Exports\\{APPID}\\` |

---

## 18. Acceptance

This SRS is accepted when:

1. This file is on `main` under `src/Vestigium.Helpers.ClosedXml/_Documentation/`.
2. Implementation of §8 write + §10 demo + §13 v1.0 tests follows without inventing CSV or charts.
3. A follow-up implementation PR can be reviewed against this text.

Implementation is a separate change from this document.
