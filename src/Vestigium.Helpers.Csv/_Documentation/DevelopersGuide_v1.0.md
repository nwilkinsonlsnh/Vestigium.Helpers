# Vestigium.Helpers.Csv — Developers Guide

**Document ID:** VEST-HLP-CSV-DEV-000  
**Version:** 1.0  
**Status:** Shipped v1.0  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Csv/`.

## Design

**Intent.** Flat delimited text for hosts that do not need a workbook. Excel still opens CSV. LibreOffice still opens CSV. A text editor still opens CSV.

**Locked decisions.** See SRS §2. The ones that must not drift:

- This is not ClosedXml. No `.xlsx` in this project.
- RFC 4180 quoting. CRLF on write. UTF-8 with BOM by default (Excel on Windows).
- Injection prefix matches ClosedXml (`= + - @` → leading `'`).
- One table per file. Desktop export tree shared with ClosedXml.
- `HelperLog` APPID `Csv`. Library never calls `Initialize`.

**Status.** Shipped. Public surface is `CsvHelper` + `CsvSession` + `CsvTable` + `CsvOptions` (settable `char` delimiter, RFC 4180 quoting, injection prefix, HelperLog).

## After acceptance

```csharp
using var file = CsvHelper.Create(HelperLog.AppIds.Csv);
file.WriteTable(CsvTable.Create(["Name", "Value"], [["alpha", 1], ["=1+1", 2]]));
var path = file.Save();
var back = CsvHelper.Open(path, HelperLog.AppIds.Csv).Read();
```

Delimiter:

```csharp
CsvHelper.WriteTable(table, path, CsvOptions.WithDelimiter('|'));
CsvHelper.WriteTable(table, path, CsvOptions.Tab);
```

Gallery: `dotnet run --project src/Vestigium.Helpers.Csv.Demo`. JSONL under `%ProgramData%\Vestigium\Logs\Csv\`.

## Files

| File | Role |
|---|---|
| `CsvHelper.cs` | Identity, Probe, paths, Create/Open, WriteTable, Read, WriteSeries |
| `CsvSession.cs` | In-memory table + Save / SaveAs / WriteTo |
| `CsvTable.cs` | Headers + rows |
| `CsvOptions.cs` | Delimiter `char` + presets + `WithDelimiter` |
| `CsvCodec.cs` | RFC 4180 emit / parse / injection |
| `CsvFormatException.cs` | Line-numbered format errors |
| `SeriesCsv.cs` | Analytics sample dump |

Do not add a ClosedXml project reference.

## Roadmap

Shipped: write / read / quoting / injection / settable delimiter / Sample.csv dump.

Next: summary.csv beside Sample, header-name formats, true append without rewriting the header.

Never: Excel, DataFrame, FileIo replacement.

Do not reference ClosedXml from this project.
