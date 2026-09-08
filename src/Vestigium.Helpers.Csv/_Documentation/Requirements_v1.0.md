# Vestigium.Helpers.Csv — Requirements Specification

**Document ID:** VEST-HLP-CSV-SRS-000  
**Version:** 1.0  
**Status:** Proposed — lossless pass; do not grow the API until this document is accepted  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Csv`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** `DevelopersGuide_v1.0.md`

This is the next library after ClosedXml / Analytics / Charts. ClosedXml stays `.xlsx` only. This package is delimited text only.

---

## 1. Purpose

Read and write CSV / TSV for Vestigium hosts that need a flat file instead of an Excel workbook. Operators open it in Excel, LibreOffice, or a text editor. PingIQ dumps a sample; a later service emits a daily extract.

```csharp
using var file = CsvHelper.Create(HelperLog.AppIds.Csv);
file.WriteTable(CsvTable.Create(["ms", "utc"], [[12.4, at], [11.9, at]]));
var path = file.Save(); // %DESKTOP%\Vestigium\Exports\Csv\
```

---

## 2. Locked decisions

| # | Decision | Locked as |
|---|---|---|
| 1 | Not ClosedXml | This library must not reference ClosedXML. ClosedXml must not parse CSV. A host may call both. |
| 2 | Dialect | RFC 4180. Comma default. Tab = TSV. Semicolon allowed (Excel locales). |
| 3 | Quote | `"` . Embedded quotes are doubled. Quote a field if it contains the delimiter, `"`, CR, or LF. |
| 4 | Newlines | Write CRLF (Excel). Read CR / LF / CRLF. |
| 5 | Encoding | UTF-8. Optional BOM (`Utf8Bom = true`) so Excel on Windows sees Unicode. Default **on** for write (operators open these in Excel). Tests may turn it off. |
| 6 | Header | Default present. `HasHeaderRow = false` is legal. |
| 7 | Injection | Same as ClosedXml: leading `= + - @` on a string cell get a leading `'`. |
| 8 | Export path | Same tree as ClosedXml: `%DESKTOP%\Vestigium\Exports\{APPID}\`. Tests pass a temp path. |
| 9 | One table per file | A CSV is one rectangle. Several extracts are several files (a host folder). No “multi-sheet CSV.” |
| 10 | Logging | `HelperLog` APPID `Csv`. Library never calls `Initialize`. |
| 11 | Analytics | Allowed to reference Analytics. `CsvHelper.WriteSeries(file, series)` writes **Sample** (index, value, timestamp) as the v1 dump — not six Excel tabs. Summary descriptors are a later file if a host asks. |
| 12 | Numbers | Invariant culture. Reject NaN / Infinity. Dates as ISO-8601 UTC. |

---

## 3. Public surface (v1)

Names may move a token. The shapes may not.

```csharp
public static class CsvHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Csv"
    public static string Probe();

    public static string DefaultExportDirectory(string appId);
    public static string NewExportPath(string appId, string? stem = null);

    public static CsvSession Create(string? appId = null, CsvOptions? options = null);
    public static CsvSession Open(string path, string? appId = null, CsvOptions? options = null);
    public static CsvSession OpenOrCreate(string path, string? appId = null, CsvOptions? options = null);

    public static void WriteSeries(CsvSession file, NumericSeries series);
}

public sealed class CsvSession : IDisposable
{
    public CsvOptions Options { get; }
    public void WriteTable(CsvTable table);
    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows);
    public CsvTable Read();
    public string Save();
    public string SaveAs(string path);
}

public sealed class CsvTable
{
    public IReadOnlyList<string> Headers { get; init; }
    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
}

public sealed class CsvOptions
{
    public char Delimiter { get; init; } = ',';
    public bool HasHeaderRow { get; init; } = true;
    public bool Utf8Bom { get; init; } = true;
    public bool NeutralizeInjection { get; init; } = true;
}
```

`Delimiter` of `'\t'` is TSV. `';'` is legal. `"` and control characters as delimiter throw.

---

## 4. Read vs write

Write first, then read what we wrote — same lesson as ClosedXml. v1 `Read()` round-trips a file this library wrote. It is not a general “any export from accounting” importer (ragged rows, mixed encodings, MacRoman). Ragged rows: missing cells become null; extra cells throw.

Empty file with `HasHeaderRow = true` throws (no header). Empty file with no header is zero rows.

---

## 5. Demo

`Vestigium.Helpers.Csv.Demo` is a WPF gallery (`HelperWpfHost.Start`, APPID `Csv`):

1. Draw the Analytics crypto sample (or a small fixture if Analytics is too heavy — prefer the same 1k unique integers).
2. **Write CSV** dumps Sample to Desktop `\Vestigium\Exports\Csv\`.
3. **Read** reopens that file through `Read()`.
4. **Inject** shows `=1+1` stored as text.
5. JSONL under `%ProgramData%\Vestigium\Logs\Csv\`.

---

## 6. Tests

xUnit, serial logger collection, temp directories only.

- Identity is `Vestigium.Helpers.Csv`.
- Probe writes Pending then Success when the host is initialized.
- Create → WriteTable → SaveAs(temp) → Open → Read equals the table (headers + two rows).
- Field with a comma round-trips quoted.
- Field with an embedded `"` round-trips as `""`.
- `=1+1` writes as `'=1+1` when neutralization is on; reopen is text, not a formula if opened in Excel (we assert the leading apostrophe in the file bytes / parsed string).
- TSV (`Delimiter = '\t'`) round-trips.
- NaN rejected.
- Tests never touch the real Desktop.

---

## 7. Non-goals (v1)

- `.xlsx` / ClosedXML
- DataFrame / LINQ-to-CSV mapping attributes
- Replacing `Vestigium.Helpers.FileIo`
- Streaming 1M-row writers (read the file; ClosedXML-scale is enough)
- Auto-detect delimiter by sniffing (caller sets it)
- Password / encryption (sibling Encryption)

---

## 8. Roadmap

### 8.1 v1 (this document, after acceptance)

Session create / open / save, `CsvTable` write + read, RFC 4180 quoting, injection prefix, UTF-8 BOM, comma / tab / semicolon, Desktop export, `WriteSeries` as Sample.csv, gallery, tests.

### 8.2 Next

| Version | Item |
|---|---|
| **v1.1** | `WriteSeries` also emits `summary.csv` (five-number + mean + P95) next to Sample |
| **v1.1** | Header-name formats on write (`ms` → one decimal, `utc` → ISO) matching ClosedXml names |
| **v1.2** | Append to an existing file without rewriting the header |
| **later** | Gzip on save |

### 8.3 Never here

Excel workbooks, `.xls`, being a statistics library, being FileIo.

---

## 9. Sibling: ClosedXml

Created as a **full** library before this SRS. Csv must not reference ClosedXml. ClosedXml must not parse CSV. A later “sheet → csv dump” may live in a host that uses both.

---

## 10. Acceptance

This SRS is accepted when:

1. This file is on `main` under `src/Vestigium.Helpers.Csv/_Documentation/`.
2. A follow-up implementation PR can be reviewed against this text without inventing `.xlsx` or a DataFrame.

Implementation is a separate change from this document.
