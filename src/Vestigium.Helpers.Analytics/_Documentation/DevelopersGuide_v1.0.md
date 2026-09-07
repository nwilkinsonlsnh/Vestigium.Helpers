# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 1.1  
**Status:** Draft  
**Date:** 7 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Analytics/`.

Do not grow the public API until [`Requirements_v1.0.md`](Requirements_v1.0.md) is accepted.

## Intended types

| Type | Role |
|---|---|
| `AnalyticsHelper` | `Identity` + `Probe()` only |
| `NumericSeries` | Immutable instance over a copied, sorted sample |
| `NumericSlice` | Descriptor for `Full`, `Q1`, `Q2`, `Q3`, `Q4`, `Iqr` |

Quartiles use Excel `PERCENTILE.INC` / NIST R7 linear interpolation so host output can be checked against a workbook.
