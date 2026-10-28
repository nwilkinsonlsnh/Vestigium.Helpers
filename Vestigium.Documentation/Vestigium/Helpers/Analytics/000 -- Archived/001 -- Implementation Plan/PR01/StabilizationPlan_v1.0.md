# Vestigium.Helpers.Analytics — Stabilization Plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-000  
**Version:** 1.1  
**Status:** Closed — implemented 19 Sep 2026  
**Date:** 19 September 2026  
**Binding contract:** `Requirements_v1.0.md` (SRS v1.6)  
**Companion:** `DevelopersGuide_v1.0.md`; `HygieneAndFeaturesPlan_v1.0.md`

Do not reopen this plan. W1–W8 are in the code. Leftover W7 work (remaining public XML, one-file SRS) moved to hygiene plan S4 and is done there. Next library work is hygiene-and-features S5, not a host adapter.

---

## Closed outcome

- Hosts can walk `series.Bands` with `TryControlLimits` without an empty-Q4 throw.
- Moving-range limits are legal only on `SliceKind.Full`.
- Tukey outliers publish values and encounter indexes.
- `Values` / `Sorted` / `Times` are frozen copies.
- `TimeSeriesPoints()` follows the clock, then encounter index.
- Docs and package metadata say `Vestigium.Logging` + `AnalyticsCatalog`.
- `Confidence.cs` does not name `MathNet.Numerics.Distributions`.
- Public members used by callers have XML docs (S4).
- `Requirements_v1.0.md` is again one v1.6 document with §0–§17 (S4).

Original work items W1–W8 stay below as the record of what was implemented.

---

## Original rule for the pass

| Do | Do not |
|---|---|
| Close caller footguns | Add Nelson / Western Electric |
| Make existing APIs safe to loop | Add a CI for P95 |
| Freeze snapshot data | Add `PdfPoints()` |
| Align docs with the code | Add two-series compare |
| Isolate MathNet behind one file | Hand-roll t / χ² |
| Keep Charts compiling | Touch ScottPlot / charting NuGets |

---

## Work items (implemented)

| ID | Item | Slice |
|---|---|---|
| W1 | `TryControlLimits` on series and slice | S2 |
| W2 | Moving range only on Full | S2 |
| W3 | Tukey outlier indexes | S2 |
| W4 | Freeze snapshot lists | S1 |
| W5 | `TimeSeriesPoints` clock order | S2 |
| W6 | `QuantileFunctions` wraps MathNet | S1 |
| W7 | Docs, package tags, XML | S3 + S4 |
| W8 | Stabilize test net | S2 |

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | First plan. Stabilize W1–W8 before any §16.2 feature. |
| 1.1 | 19 Sep 2026 | Closed. W7 leftovers finished as hygiene S4. |
