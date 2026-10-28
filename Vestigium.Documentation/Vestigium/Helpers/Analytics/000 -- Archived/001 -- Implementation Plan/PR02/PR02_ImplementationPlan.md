# Vestigium.Helpers.Analytics — PR02 implementation plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-PR02  
**Version:** 1.1  
**Status:** Closed 19 September 2026  
**Date:** 19 September 2026  
**Scope:** Review findings after S5. Library-only. No host adapters. No L4–L7.

PR02 was tighten-only. Features stay parked.

## Steps

| Step | Priority | Recommendation | Status |
|---|---|---|---|
| **PR02.001** | P0 | Freeze `OutOfControlIndexes` and `MovingRanges` | Done |
| **PR02.002** | P0 | Cap OOC indexes in log properties (≤ 32) | Done |
| **PR02.003** | P0 | One empty-percentile exception | Done |
| **PR02.004** | P1 | Descriptor overflow → reject, not unexpected | Done |
| **PR02.005** | P1 | Stop forcing APPID `"Analytics"` | **Withdrawn.** Row `appId` is the library identity. Folder follows the host. Reverted to match Json / Encryption / FileIo / Network and the suite Developers Guide. |
| **PR02.006** | P2 | Sanitize `Name` in log properties | Done. `AnalyticsLog.SanitizeName` + `Props` key `name`. |
| **PR02.007** | P2 | Tests + close | Done. Fixtures in `AnalyticsPR02Tests.cs`. |

## Tests that close this plan

| Fixture | Covers |
|---|---|
| `PR02_001_computed_limit_lists_are_frozen` | Frozen MR indexes / ranges |
| `PR02_001_against_lists_are_frozen` | Frozen `Against` lists |
| `PR02_002_log_indexes_join_when_at_most_32` | Log join |
| `PR02_002_log_indexes_truncate_past_32` | Log cap |
| `PR02_003_empty_percentile_is_one_invalid_operation` | One exception, one sentence |
| `PR02_004_descriptor_overflow_is_argument_out_of_range` | Overflow reject |
| `PR02_006_sanitize_name_strips_controls_and_truncates` | Name hygiene |
| `PR02_006_name_log_property_is_sanitized` | `Props["name"]` |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02_
```

## Out of PR02 (still parked)

| Item | Why |
|---|---|
| Soft cap on `n` | Silent truncate is a lie. |
| Moments as `decimal` | Design choice. |
| L4 run rules / L5 percentile interval / L6 `PdfPoints` / L7 two-series | Features. Re-evaluate after this close. |
| Host / PingIQ adapters | Different project. |
| `NumericSeries.Name` store vs log | Logs are sanitized. Snapshot `Name` is still `Trim()` only. |

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | PR02.001–007 from the post-S5 review. |
| 1.1 | 19 Sep 2026 | Closed. 001–004 and 006 shipped. 005 withdrawn. |
