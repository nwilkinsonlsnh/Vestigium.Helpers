# Vestigium.Helpers.Analytics — Hygiene and library-complete features

**Document ID:** VEST-HLP-ANALYTICS-PLAN-001  
**Version:** 1.1  
**Status:** S4 closed 19 Sep 2026 — S5 next  
**Date:** 19 September 2026  
**Binding contract:** `Requirements_v1.0.md` (SRS v1.6)  
**Predecessor:** `StabilizationPlan_v1.0.md` (closed)

This plan is **library-only**. It does not name a host product, does not add an orchestration type, and does not teach a consumer how to talk to Analytics. Consumers stay callers of `NumericSeries` / `SeriesSlice` / `ControlLimits`. If a product later needs a façade, that façade lives outside this project.

S4 (H1–H4) is done: public XML on `NumericSeries`, confidence types, and `ControlLimits`; stabilize plan closed; developers guide points here. The long-form SRS is the binding contract in `Requirements_v1.0.md`. Next slice is **S5** (L1 `ControlLimits.Against`, L2 `PercentileRank`).

---

## 0. Rule for this pass

| Do | Do not |
|---|---|
| Finish docs and XML so stabilize is actually closed | Add a host-specific wrapper, adapter, or orchestration class |
| Complete numbers this library already implies | Mention or encode a product workflow |
| Keep every new API on `NumericSeries`, `SeriesSlice`, or `ControlLimits` | Add a second entry type besides `NumericSeries` |
| Keep Charts as a consumer of numbers, not a reason for an API | Reference ScottPlot / any drawing surface |
| Park density and two-series until hygiene + small holes are done | Pull §16.2 items in “while we are here” without a slice |

Language in XML, SRS, and this plan talks about **samples, slices, fences, ranks**. Not applications.

---

## 1. Two tracks

| Track | Goal | When |
|---|---|---|
| **H — Hygiene** | Docs match code. Public surface has `///`. Stabilize plan can be marked closed. | First. No math. **S4 done.** |
| **L — Library holes** | Numbers the public types already imply but do not publish. | After H. **S5 next.** |

---

## 2. Track H — Hygiene (closed)

H1 XML on `NumericSeries`, `ConfidenceLevel` / `Interval` / `Report`, `ControlLimits`.  
H2 One SRS file at `Requirements_v1.0.md` (v1.6 §0–§17).  
H3 Stabilize plan status Closed.  
H4 Developers guide points at this plan, not a host adapter.

---

## 3. Track L — Holes in this type system

### L1 — Score a finished band against values

```csharp
ControlLimits ControlLimits.Against(IReadOnlyList<decimal> encounterOrder)
```

Copies Center / Upper / Lower / Method / K / Floor / MR fields. Recomputes `OutOfControlIndexes` / `OutOfControlCount` the same way `Compute` does. Does not recompute the fences. Empty sequence → empty indexes, count 0.

### L2 — Percentile rank

```csharp
double SeriesSlice.PercentileRank(decimal x)
```

`(count of values ≤ x) / n`. Empty slice throws like `Percentile`. Sample rank, not a confidence level.

### L3 — Stop after L1 and L2

---

## 4. Later library items (not S5)

L4 run rules on Full. L5 interval for a percentile. L6 `PdfPoints()`. L7 two-series compare. Still library work. Still not a host wrapper.

---

## 5. Explicitly out of this library

Any type whose name or XML mentions a product. An orchestration / adapter under this folder. Chart controls. Time-bucket histograms. Streaming. Shapiro–Wilk as a gate. Bootstrap as the default mean interval.

---

## 6. Implementation slices

| Slice | Items | Theme |
|---|---|---|
| **S4** | H1–H4 | Closed 19 Sep 2026 |
| **S5** | L1, L2 | Score caller fences; percentile rank |
| **S6** | stop | Re-evaluate L4–L7 |

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Hygiene H1–H4, library holes L1–L2, later L4–L7 parked. |
| 1.1 | 19 Sep 2026 | S4 closed. |
