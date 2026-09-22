# Vestigium.Helpers.Charts — PR05 implementation plan

**Document ID:** VEST-HLP-CHARTS-PLAN-PR05  
**Version:** 1.1  
**Status:** Closed 19 September 2026  
**Date:** 19 September 2026  
**Binding:** Charts `Requirements_v1.0.md` C1–C6  
**Predecessor:** Analytics PR03 / PR04 (closed)

Charts still only draws. Analytics still only computes.

## Steps

| Step | Priority | Recommendation | Status |
|---|---|---|---|
| **PR05.001** | P0 | Honor `level` on `MeanInterval` | Done |
| **PR05.002** | P0 | Mean-interval fixture | Done |
| **PR05.003** | P1 | Paint host `RunRuleReport` on Control | Done |
| **PR05.004** | P1 | Paint host `SpecLimits` LSL/USL | Done |
| **PR05.005** | P1 | Histogram `ShowKde` from `PdfPoints` | Done |
| **PR05.006** | P2 | `ChartKind.PercentileInterval` | Done |
| **PR05.007** | P2 | Fixtures + close | Done |

## Doors

```csharp
ChartView.MeanInterval(series, 0.80);
ChartView.Control(series, limits, series.RunRules());
ChartView.Control(series, limits, new ChartOptions { Spec = SpecLimits.From(0, 15) });
ChartView.Histogram(series, new ChartOptions { ShowBellCurve = true, ShowKde = true });
ChartView.PercentileInterval(series, p: 0.95, level: 0.95);
```

Out of this plan: Refresh, dark palette, SaveSvg, multi-series, host/PingIQ.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Opened. |
| 1.1 | 19 Sep 2026 | Closed. |
