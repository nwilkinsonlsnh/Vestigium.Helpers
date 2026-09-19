# Vestigium.Helpers.Analytics — PR04 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR04  
**Version:** 1.1  
**Status:** Closed 19 September 2026  
**Date:** 19 September 2026  
**Predecessor:** `PR03_ImplementationPlan.md` (closed)

PR03 stayed closed. Hosts / PingIQ stayed out of this library.

## Steps

| Step | Priority | Recommendation | Status |
|---|---|---|---|
| **PR04.001** | P0 | `PercentileInterval` type + door | Done |
| **PR04.002** | P0 | Order-statistic binomial CI | Done |
| **PR04.003** | P1 | Percentile-CI fixtures (n=9) | Done |
| **PR04.004** | P1 | Gaussian KDE `PdfPoints` | Done |
| **PR04.005** | P1 | KDE fixtures | Done |
| **PR04.006** | P1 | Nelson 5–8 on Full | Done |
| **PR04.007** | P2 | Nelson fixtures + close | Done |

## Shipped doors

```csharp
var ci = series.PercentileInterval(0.95);          // order stats, or sample range if γ unmet
var pdf = series.PdfPoints();                      // 64 Gaussian KDE points
var we = series.RunRules();                        // WE 1–4 + Nelson 5–8
```

`{1..9}` is a Nelson 5 hit (strict rise). A wiggle series is the quiet WE 1–4 check.

## Still out of this library

Host / PingIQ adapters, bootstrap percentile CI, other kernels, EWMA / CUSUM.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR04.001–007 from the parked L5 / L6 / Nelson list. |
| 1.1 | 19 Sep 2026 | Closed. |
