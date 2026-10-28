# Vestigium.Helpers.Csv — Design

**Document ID:** VEST-HLP-CSV-DSN-000  
**Version:** 1.0  
**Status:** Locked companion to SRS v1.0  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records *why* Csv is shaped this way. It does not add requirements.

---

## 1. Intent

Give a host a flat file when an `.xlsx` is too much. One session owns one rectangle. Operators open it in Excel, LibreOffice, or a text editor.

```
host rows / NumericSeries
    → CsvHelper.Create / Open / OpenOrCreate
         ├ WriteTable / AppendRows / Read
         ├ WriteSeries            Sample: index, value, timestamp
         └ Save / SaveAs / WriteTo
ClosedXml is the workbook sibling. Neither library parses the other format.
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Separate package from ClosedXml | `.xlsx` and delimited text have different engines and different failure modes. |
| RFC 4180 dialect | Predictable quoting. Caller sets delimiter; no sniff. |
| Quote locked to `"` | Embedded quotes double. Quote when the field holds delimiter, quote, CR, or LF. |
| Write CRLF; read CR / LF / CRLF | Excel on write. Tolerant on read. |
| UTF-8, BOM on by default | Windows Excel sees Unicode. Tests may turn the BOM off. |
| One table per file | A CSV is one rectangle. Several extracts are several files. |
| Same Desktop export tree as ClosedXml | Humans look in one place. Logs stay under ProgramData. |
| Injection prefix matches ClosedXml | Leading `= + - @` become text. |
| Invariant numbers; ISO-8601 UTC dates | Cross-locale files. Reject NaN / Infinity. |
| `WriteSeries` is Sample only | Six Excel tabs belong in ClosedXml. Summary.csv is roadmap. |
| Codec behind the session | `CsvCodec` is the parser/writer. Hosts talk to `CsvHelper` / `CsvSession`. |
| Never `Initialize` | Folder follows the host APPID. `SessionId` is the correlation id. |

---

## 3. Shape

| File | Role |
|---|---|
| `CsvHelper.cs` | Identity, Probe, Create / Open / OpenOrCreate, paths, one-shot Read/Write, WriteSeries |
| `CsvSession.cs` | Owns one table and optional path |
| `CsvTable.cs` | Headers + rows |
| `CsvOptions.cs` | Dialect: delimiter, header, BOM, neutralize, trim |
| `CsvCodec.cs` | RFC 4180 read / write bytes and text |
| `CsvFormatException.cs` | Parse failures |
| `SeriesCsv.cs` | Analytics Sample dump |
| `CsvLog` / `CsvCatalog` / `CsvEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` | Required reference is null. |
| `ArgumentException` | Blank path / appId. |
| `ArgumentOutOfRangeException` | Illegal delimiter; non-finite number. |
| `FileNotFoundException` | `Open` on a missing file. |
| `InvalidOperationException` | Append before WriteTable; use after dispose. |
| `CsvFormatException` | Ragged extra cells; missing header when `HasHeaderRow`; unterminated quote. |

Missing cells on a short row become null. Extra cells throw.

---

## 5. What closed to reach 1.0

Session create / open / save, `CsvTable` write + read, RFC 4180 quoting, injection prefix, UTF-8 BOM, comma / tab / semicolon / pipe, Desktop export, `WriteSeries` as Sample, Identity / Probe.

---

## 6. Still out

`summary.csv` beside Sample, header-name formats, append-without-rewrite on disk, gzip, delimiter sniff, DataFrame mapping, `.xlsx`, FileIo replacement, streaming million-row writers, encryption.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Sep 2026 | First standalone Design. Content lifted from SRS v1.0 + shipped codec/session. |
