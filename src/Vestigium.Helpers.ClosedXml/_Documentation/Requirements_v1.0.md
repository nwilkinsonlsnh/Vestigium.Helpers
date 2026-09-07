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
| 7 | Logging | `HelperLog` only. Libraries never call `VestigiumLogger.Initialize`. APPID = `ClosedXml`. |
| 8 | Charts | Not in this library for v1. Same rule as Analytics: numbers and tables first. Charts are a later helper or a later milestone. |

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
| Excel charts, sparklines | Deferred with Analytics charting. |
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
- The helper creates the directory if it is missing.
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

    public static WorkbookSession Create(string? firstSheetName = null);
    public static WorkbookSession Open(string path);
    public static WorkbookSession OpenOrCreate(string path, string? firstSheetName = null);
}
```

### 8.2 Session

```csharp
public sealed class WorkbookSession : IDisposable
{
    public IReadOnlyList<string> SheetNames { get; }
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
    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows);
    public void ApplyChrome(SheetChrome chrome);
}
```

v1.1 adds:

```csharp
    public SheetTable ReadUsedRange(SheetReadOptions? options = null);
```

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
}
```

Do not leak a kitchen-sink `IXLStyle` through the helper. If a host needs a one-off style, it is not this library's problem in v1.

---

## 9. Analytics bridge (demo + optional helper)

The ClosedXml **library** may reference Analytics. The ClosedXml **demo** will.

v1 demo workbook sheets (minimum):

| Sheet | Contents |
|---|---|
| `Summary` | n, min, Q1, median, Q3, max, mean, stddev, skew, excess kurtosis, P90, P95, P99 |
| `Bands` | Full / Q1 / Q2 / Q3 / Q4 / IQR with n, min, P50, max, mean, stddev, skew, excess kurtosis |
| `Confidence` | level, parameter, estimate, lower, upper, width, method for 90 / 95 / 99 and FPC mean |
| `Histogram` | bin index, lower, upper, count, relative frequency |
| `Sample` | observation index, value, timestamp |

A `WorkbookHelper.WriteSeries(WorkbookSession, NumericSeries, string? prefix = null)` should land in v1.0 so PingIQ does not copy the demo sheet layout.

---

## 10. Demo contract

`Vestigium.Helpers.ClosedXml.Demo`:

1. `HelperDemoHost.Run` with APPID `ClosedXml`.
2. Call `WorkbookHelper.Probe()`.
3. Draw or reuse an Analytics series (crypto sample is fine; 200-1000 points).
4. Write the sheets in §9.
5. `Save()` to `%DESKTOP%\Vestigium\Exports\ClosedXml\vestigium-ClosedXml-{stamp}.xlsx`.
6. Print the full path to the console.
7. JSONL still goes to `%ProgramData%\Vestigium\Logs\ClosedXml\`.

The demo references Analytics. The library may as well. Core `Vestigium.Helpers` does not reference ClosedXml or Analytics.

---

## 11. Safety

- Sheet names: 1-31 characters, not `[]:*?/\\`. Helper sanitizes or throws. Empty becomes `Sheet1`.
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

v1.1 adds read-back equality tests on the Analytics demo shape (summary row count, histogram bin count).

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

### v1.1 — Read what we wrote

- `ReadUsedRange` -> `SheetTable`
- Typed cell guess: number, text, bool, DateTime
- Round-trip tests: write demo shape, read, compare counts and a handful of values
- Still not a general "any Excel file from accounting" importer

### v1.2 — Operator chrome

- Print: landscape, fit-to-width, footer with APPID + timestamp
- Column number formats per header name (`ms`, `pct`, `utc`)
- Optional single conditional-format on a named column (high outliers)
- Sheet order helper

### v2.0 — Templates and pictures

- Open a caller-supplied letterhead `.xlsx` and write into a named range or a reserved sheet
- Embed a logo image at a fixed cell
- Multiple workbooks merged by sheet name (append-only)

### v2.1 — Charts (maybe a helper library)

- Only after a charting discussion. Prefer a future `Vestigium.Helpers.Charts` that consumes Analytics chart-ready points. Do not hide chart builders inside ClosedXml by accident.

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
