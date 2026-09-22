# Vestigium.Helpers.Analytics — PR03 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR03  
**Version:** 1.1  
**Status:** Closed 19 September 2026  
**Date:** 19 September 2026  
**Binding:** `Requirements_v1.0.md`  
**Predecessor:** `PR02_ImplementationPlan.md` (closed)

Library-only. No host adapters. No PingIQ. No drawing. No L5. No L6.

## Steps

| Step | Priority | Recommendation | Status |
|---|---|---|
| **PR03.001** | P0 | `SpecLimits` + outside-spec indexes | Done |
| **PR03.002** | P0 | Pp / Ppk from sample s | Done |
| **PR03.003** | P0 | Cp / Cpk from MR̄ / d2 | Done |
| **PR03.004** | P1 | Capability fixtures | Done |
| **PR03.005** | P1 | Western Electric 1–4 on Full | Done |
| **PR03.006** | P1 | Run-rule fixtures | Done |
| **PR03.007** | P2 | Two-series + close | Done |

## Shipped doors

```csharp
var spec = SpecLimits.From(0, 30).Against(values);
var cap = series.Capability(SpecLimits.From(0, 15));   // Pp from s, Cp from MR̄/d2 on Full
var we = series.RunRules();                            // rules 1–4
var cmp = a.Compare(b);                                // Welch always; paired when n matches
```

## Still parked

L5 percentile CI, L6 KDE/`PdfPoints`, Nelson 5–8, EWMA, CUSUM, host adapters.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR03.001–007 capability → run rules → two-series. |
| 1.1 | 19 Sep 2026 | Closed. All seven steps shipped. |
