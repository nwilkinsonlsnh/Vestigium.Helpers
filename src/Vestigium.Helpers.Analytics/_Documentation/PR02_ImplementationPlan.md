# Vestigium.Helpers.Analytics — PR02 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR02  
**Version:** 1.0  
**Status:** Open — stabilize-then-expand still holds; this is tighten, not new features  
**Date:** 19 September 2026  
**Scope:** Review findings after S5. Library-only. No host adapters. No L4–L7.

PR02 is the next push-release. Each row is one implementable step. Do them in number order. Stop after PR02.007 unless we reopen L4–L7.

## Steps

| Step | Priority | Recommendation | Why | Files |
|---|---|---|---|---|
| **PR02.001** | P0 | Freeze `OutOfControlIndexes` and `MovingRanges` | Same hole W4 closed on `Values`. Hosts can cast `List<int>` / `double[]` and rewrite the painted points. | `ControlLimits.cs` (`Compute`, `Against`) |
| **PR02.002** | P0 | Cap out-of-control index lists in log properties | `string.Join` of every index can write a six-figure log line. Count always; indexes only when count ≤ 32. | `ControlLimits.cs` |
| **PR02.003** | P0 | One empty-percentile exception | `Percentile`, `Quantiles.Inclusive`, and `PercentileRank` disagree on type and wording. Hosts catching one miss the others. Use `InvalidOperationException` and one sentence. | `Quantiles.cs`, `DescriptiveStatistics.cs`, `SeriesSlice.cs`, `SeriesSlice.Rank.cs` |
| **PR02.004** | P1 | Treat descriptor overflow as reject, not unexpected | `sum`, midrange, Tukey fences can overflow legal decimals. Today that is `OverflowException` + unexpected log. Map to overflow event + `ArgumentOutOfRangeException`. | `DescriptiveStatistics.cs`, `NumberConvert.cs` |
| **PR02.005** | P1 | Stop forcing APPID `"Analytics"` on every write | Host initialized as another APPID still gets Analytics-tagged rows. Let Logging use the process APPID. | `AnalyticsLog.cs` |
| **PR02.006** | P2 | Sanitize `Name` in log properties | Hostile or huge `Name` can split a text log. Truncate and strip CR/LF. Values themselves stay off the log. | `NumericSeries.cs`, `AnalyticsLog.cs` |
| **PR02.007** | P2 | Tests + close PR02 | Fixtures for freeze, log cap contract (count only — do not assert log sink), empty-percentile type, overflow reject. Mark this plan closed. No new features. | `AnalyticsS5Tests.cs` or `AnalyticsPR02Tests.cs`, this file |

## Priority key

| Priority | Meaning |
|---|---|
| P0 | Functional lie or mutable snapshot. Patch before any other work. |
| P1 | Fail-fast / logging correctness. Patch in the same release. |
| P2 | Hygiene. Same release if cheap; do not block 001–005. |

## Out of PR02

| Item | Why it stays out |
|---|---|
| Soft cap on `n` | Document later. Silent truncate is a lie. |
| Moments as `decimal` | Design choice, not a hole. |
| L4 Western Electric / L5 percentile interval / L6 `PdfPoints` / L7 two-series | Features. Re-evaluate after PR02. |
| Host / PingIQ adapters | Different project. |

## Close rule

PR02 is closed when 001–006 are in source, 007 tests pass, and this document says Closed.

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Analytics
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR02.001–007 from the post-S5 review. |
