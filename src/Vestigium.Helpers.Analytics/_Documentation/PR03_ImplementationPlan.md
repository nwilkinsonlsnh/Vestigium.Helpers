# Vestigium.Helpers.Analytics — PR03 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR03  
**Version:** 1.0  
**Status:** Open — expand after PR02 close  
**Date:** 19 September 2026  
**Binding:** `Requirements_v1.0.md`  
**Predecessor:** `PR02_ImplementationPlan.md` (closed)

Library-only. No host adapters. No PingIQ. No drawing. No L5 percentile interval. No L6 KDE/`PdfPoints`.

Do the rows in number order. Stop after PR03.007 unless we reopen L5/L6.

## Steps

| Step | Priority | Recommendation | Why | Files |
|---|---|---|---|---|
| **PR03.001** | P0 | `SpecLimits` + outside-spec indexes | LSL/USL are numbers, same family as `FromCaller`. Validate at least one finite spec; if both present then USL > LSL. Score encounter order. Null/empty values → count 0. Freeze indexes. | New `SpecLimits.cs` (or on `ControlLimits` only if it stays fences — prefer a new type) |
| **PR03.002** | P0 | `ProcessCapability` Pp / Ppk from sample s | Overall capability on this snapshot. Pp = (USL−LSL) / (6s). Ppk = min((USL−x̄)/(3s), (x̄−LSL)/(3s)). One-sided spec → that side only; the two-sided Pp is null. Null when s is not positive. | New `ProcessCapability.cs`, `SeriesSlice` / `NumericSeries` door |
| **PR03.003** | P0 | Cp / Cpk from MR̄ / d2 | Within capability for individuals. σ̂ = MR̄ / d2, d2 = `ControlLimits.D2Span2`. Same one-sided rules as 002. Moving range on Full only. | `ProcessCapability.cs`, reuse MR from `ControlLimits.Compute` |
| **PR03.004** | P1 | Capability fixtures | Two-sided and one-sided specs. Constant series → capability null, not a throw. Outside-spec indexes frozen. MR refused on Q4. | `AnalyticsPR03Tests.cs` |
| **PR03.005** | P1 | Western Electric run rules on Full | Rules 1–4 only (point beyond 3σ; 2 of 3 beyond 2σ same side; 4 of 5 beyond 1σ same side; 8 on one side of CL). Encounter order. Each hit publishes rule + indexes. Illegal on value bands. | New `RunRules.cs` |
| **PR03.006** | P1 | Run-rule fixtures | Known 1–9 plus a spike; empty-band refuse; indexes frozen. | `AnalyticsPR03Tests.cs` |
| **PR03.007** | P2 | Two-series + close | `NumericSeries.Compare(NumericSeries other)`: paired difference when counts match (new snapshot of x−y); two-sample t on means when they do not (or always publish the t as well). Report: n1, n2, mean delta, two-sided p when defined. Tests. Mark this plan Closed. | New `SeriesCompare.cs`, tests, this file |

## Priority key

| Priority | Meaning |
|---|---|
| P0 | Completes the process-against-a-band story. |
| P1 | Completes Shewhart on Full. Same release as 001–004. |
| P2 | First time two snapshots talk. Same release if cheap. |

## Formulae (do not invent others)

Snapshot has mean x̄ and sample s. Specs LSL / USL optional but not both missing.

```
Pp  = (USL − LSL) / (6 s)                 // both specs, s > 0
Ppu = (USL − x̄) / (3 s)                   // USL present, s > 0
Ppl = (x̄ − LSL) / (3 s)                   // LSL present, s > 0
Ppk = min of the sides that exist

σ̂_w = MR̄ / d2                              // Full, MR̄ > 0, d2 = D2Span2
Cp  = (USL − LSL) / (6 σ̂_w)
Cpu = (USL − x̄) / (3 σ̂_w)
Cpl = (x̄ − LSL) / (3 σ̂_w)
Cpk = min of the sides that exist
```

Outside spec: `y < LSL` or `y > USL` (strict). Same freeze as PR02.001. Log index cap same as PR02.002.

Run rules use the **same** CL / σ as the limits method the caller passed (mean±kσ or MR). Default mean±3σ. Rule 1 is already `OutOfControlIndexes`; still emit it as a named rule so charts have one list.

Two-sample t: Welch, two-sided, MathNet only through `QuantileFunctions.StudentTCdf`. Paired series is a new `NumericSeries` of differences, not a mutation.

## Out of PR03

| Item | Why |
|---|---|
| L5 CI for a percentile | Different estimator. Re-evaluate after close. |
| L6 `PdfPoints` / KDE | Histogram + ECDF already exist. |
| Nelson 5–8, EWMA, CUSUM | Another release. |
| Host / PingIQ / drawing | Different project. |
| Changing MathNet policy | Closed in S1. |

## Close rule

PR03 is closed when 001–006 are in source, 007 tests pass, and this document says Closed.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR03.001–007 capability → run rules → two-series. |
