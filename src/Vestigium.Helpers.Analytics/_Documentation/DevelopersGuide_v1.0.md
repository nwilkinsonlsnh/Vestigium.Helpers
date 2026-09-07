# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 1.2  
**Status:** Active  
**Date:** 7 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Analytics/`.

Source of truth: [`Requirements_v1.0.md`](Requirements_v1.0.md) v1.1 (accepted).

## Intended types

| Type | Role |
|---|---|
| `AnalyticsHelper` | `Identity` + `Probe()` only |
| `NumericSeries` | Immutable instance over a copied, sorted sample |
| `NumericSlice` | Descriptor for `Full`, `Q1`, `Q2`, `Q3`, `Q4`, `Iqr` |
| `ConfidenceInterval` | Level, estimate, lower, upper, method |

Quartiles use Excel `PERCENTILE.INC` / NIST R7.

Confidence quantiles (Student t, chi-square) come from `MathNet.Numerics`. Do not hand-roll the inverse CDF.

Default confidence level is 0.95. `ConfidenceInterval(level)` is a read against the same snapshot — it does not rebuild the series.
