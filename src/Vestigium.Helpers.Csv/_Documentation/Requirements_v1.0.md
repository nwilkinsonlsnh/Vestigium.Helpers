# Vestigium.Helpers.Csv — Requirements Specification

**Document ID:** VEST-HLP-CSV-SRS-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 7 September 2026

## Purpose

Read and write delimited text (CSV / TSV) for Vestigium hosts that need a flat file instead of an Excel workbook.

This is **not** ClosedXML. `Vestigium.Helpers.ClosedXml` writes `.xlsx` only. Do not implement CSV inside that project.

## Target

- Framework: `net10.0`
- Windows-only: no
- Logging: `HelperLog.AppIds.Csv`, library never calls `Initialize`

## This milestone

Placeholder public type only (`CsvHelper.Identity`, `CsvHelper.Probe`). Do not grow the API until a lossless SRS is accepted and versioned.

Likely v1 topics when that pass happens: delimiter, header row, quoting, UTF-8 BOM, injection prefix, Desktop export path shared with ClosedXml (`%DESKTOP%\Vestigium\Exports\{APPID}\`).

## Non-goals (even later)

- Parsing `.xlsx`
- Being a DataFrame library
- Replacing `Vestigium.Helpers.FileIo` general file I/O
