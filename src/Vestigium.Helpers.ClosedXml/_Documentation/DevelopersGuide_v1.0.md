# Vestigium.Helpers.ClosedXml — Developers Guide

**Document ID:** VEST-HLP-CLOSEDXML-DEV-000  
**Version:** 1.0  
**Status:** Active (SRS accepted; engine not implemented yet)  
**Date:** 7 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.ClosedXml/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. Write-first, then read-back. No CSV in this project.

## Current code

Until the engine PR lands, the public surface is still the suite skeleton:

- `WorkbookHelper.Identity` = `Vestigium.Helpers.ClosedXml`
- `WorkbookHelper.Probe()` logs Pending / Success through `HelperLog`

Do not add `XLWorkbook` calls to `Probe` until the session type exists. Smoke tests only assert Identity.

## After v1.0 write ships

```csharp
using var book = WorkbookHelper.Create("Summary");
book.Sheet("Summary").WriteTable(table);
var path = book.SaveAs(WorkbookHelper.NewExportPath(HelperLog.AppIds.ClosedXml));
```

Default folder: `%DESKTOP%\Vestigium\Exports\{APPID}\`.

Tests must pass `SaveAs` a temp path. Never Save() to the real Desktop from xUnit.

## Demo

`src/Vestigium.Helpers.ClosedXml.Demo` will reference Analytics and write Summary / Bands / Confidence / Histogram / Sample sheets. JSONL still goes to `%ProgramData%\Vestigium\Logs\ClosedXml\`.

## CSV

That work is `Vestigium.Helpers.Csv`. Do not add a CSV parser here.
