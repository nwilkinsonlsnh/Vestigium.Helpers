# Vestigium.Helpers.Analytics — PR04 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR04  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Predecessor:** `PR03_ImplementationPlan.md` (closed)

PR03 is closed. These are not PR03.008+. Same library-only rule: no PingIQ, no drawing, no host adapters inside `Vestigium.Helpers.Analytics`.

Do the rows in number order. Stop after PR04.007 unless we open a host project.

## Steps

| Step | Priority | Recommendation | Why | Files |
|---|---|---|---|---|
| **PR04.001** | P0 | Percentile interval type | Point P95 is not a CI. Add `PercentileInterval` (p, γ, lower, upper, method name). Door: `slice.PercentileInterval(p, level)`. Empty band / p not in [0,1] / γ not in (0,1) use the existing reject paths. | New `PercentileInterval.cs` |
| **PR04.002** | P0 | Order-statistic (binomial) CI for a percentile | Distribution-free: find smallest j and largest k so the binomial coverage of `X(j)…X(k)` for quantile p is at least γ. Use `QuantileFunctions` only if we need a regularized incomplete beta; otherwise evaluate binomial sums in decimals-as-double for n that already fit this library. No bootstrap in this step. | `PercentileInterval.cs`, `QuantileFunctions.cs` only if beta/binomial CDF is added there |
| **PR04.003** | P1 | Percentile-CI fixtures | n=9, p=0.5 and p=0.95, γ=0.95. Empty / bad p / bad γ. Interval always uses snapshot values (order stats), never interpolates a new point as a bound. | `AnalyticsPR04Tests.cs` |
| **PR04.004** | P1 | `PdfPoints` Gaussian KDE | Grid of (x, density). Kernel N(0,1), bandwidth Silverman `1.06 s n^(-1/5)` when s>0; null grid when s is not positive (no throw). Count default 64, cap 512. x-range [min − 3h, max + 3h]. No extra NuGet. | New `KernelDensity.cs` |
| **PR04.005** | P1 | KDE fixtures | `{1..9}` produces 64 finite non-negative y that integrate roughly to 1. Constant series → empty points, no throw. Count 0 or >512 rejected. | `AnalyticsPR04Tests.cs` |
| **PR04.006** | P1 | Nelson rules 5–8 on Full | 5: six increasing or six decreasing. 6: fifteen in zone C (\|z\| < 1). 7: fourteen alternating up/down. 8: eight beyond 1σ (both sides allowed) with none in zone C. Same CL/σ as PR03.005. Append to `RunRuleReport.Hits`. Value bands still illegal. | `RunRules.cs` |
| **PR04.007** | P2 | Nelson fixtures + close | Known runs for 5 and 8; quiet `{1..9}` still has no hits; indexes frozen. Mark this plan Closed. | `AnalyticsPR04Tests.cs`, this file |

## Priority key

| Priority | Meaning |
|---|---|
| P0 | Honest interval for a percentile. |
| P1 | Chart food + finish Shewhart. Same release. |
| P2 | Close the library slice. |

## Estimators (do not invent others)

**L5 — order statistic CI.** Sorted sample X(1) ≤ … ≤ X(n). For quantile p and coverage γ, choose the tightest (j, k) with 1 ≤ j ≤ k ≤ n such that

```
P(X(j) ≤ ξ_p ≤ X(k)) = Σ_{i=j}^{k} C(n,i) p^i (1−p)^{n−i}  ≥ γ
```

Bounds are `X(j)` and `X(k)`. If no pair reaches γ, return the full range X(1)…X(n) and set `ReachedCoverage` false. Do not bootstrap. Do not call it “P95 SLA.”

**L6 — Gaussian KDE.**

```
h = 1.06 s n^{-1/5}
ŷ(x) = (1 / (n h)) Σ φ((x − x_i) / h)
```

φ is standard normal density (`exp(-u²/2) / √(2π)`). Grid inclusive of endpoints.

**Nelson 5–8** use the same σ as the limits method passed to `RunRules`. Ties on rule 5 (equal consecutive values) break the strict run. Zone C is \|y − CL\| < σ, not ≤.

## Out of PR04 (still not this project)

| Item | Why |
|---|---|
| Host / PingIQ adapters | Different project. If we do it, it is `Vestigium.Helpers.Analytics.Hosting` or an app repo — not this library. |
| Bootstrap percentile CI | Second estimator. Only after 002 is trusted. |
| Adaptive / Epanechnikov KDE | One kernel first. |
| EWMA / CUSUM | Another release. |
| Changing MathNet policy | Closed in S1. |

## Close rule

PR04 is closed when 001–006 are in source, 007 tests pass, and this document says Closed.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR04.001–007 from the parked L5 / L6 / Nelson list. Hosts stay out. |
