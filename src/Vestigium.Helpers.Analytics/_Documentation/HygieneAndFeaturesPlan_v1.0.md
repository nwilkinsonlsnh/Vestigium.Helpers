# Vestigium.Helpers.Analytics — Hygiene and library-complete features

**Document ID:** VEST-HLP-ANALYTICS-PLAN-001  
**Version:** 1.0  
**Status:** Accepted intent  
**Date:** 19 September 2026  
**Binding contract:** `Requirements_v1.0.md` (SRS v1.6 amendments + v1.5 long form)  
**Predecessor:** `StabilizationPlan_v1.0.md` (W1–W8 implemented in code)

This plan is **library-only**. It does not name a host product, does not add an orchestration type, and does not teach a consumer how to talk to Analytics. Consumers stay callers of `NumericSeries` / `SeriesSlice` / `ControlLimits`. If a product later needs a façade, that façade lives outside this project.

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
| **H — Hygiene** | Docs match code. Public surface has `///`. Stabilize plan can be marked closed. | First. No math. |
| **L — Library holes** | Numbers the public types already imply but do not publish. | After H. |

H can merge alone. L is three small additive APIs, then a stop. Larger §16.2 items (run rules, percentile interval, `PdfPoints`, two-series) are listed so they are not forgotten, and are **explicitly not in the first L slice**.

---

## 2. Track H — Hygiene

### H1 — XML on the remaining public members

**Problem.** CS1591 is enabled. `SeriesSlice`, `FrequencyTable`, `Observation` family, `AnalyticsHelper`, catalog/events have comments. These still do not:

- `NumericSeries` (factories, properties, confidence helpers, chart views, `TryControlLimits`)
- `ConfidenceLevel`, `ConfidenceInterval`, `ConfidenceReport`
- `ControlLimits` properties, `FromCaller`, `IsOutOfControl`

Internals (`DescriptiveStatistics`, `NumberConvert`, `QuantileFunctions`, `AnalyticsLog`, `Quantiles`) stay uncommented.

**Done when.** `dotnet build src/Vestigium.Helpers.Analytics` reports no CS1591.

### H2 — One SRS file again

**Problem.** `Requirements_v1.0.md` on `main` is the v1.6 amendment page. The section-by-section contract (§0–§16) still lives in blob `4dd84d76`. Two files is how a rule gets lost.

**Decision.** Restore the v1.5 long form and fold the v1.6 amendments into the same headings (A3, A5, A9, §4, §5.3, §8.2, §10.1, §10.2, §11, §16.1, §17). One file, version **1.6**, status accepted.

Source for the long form: git history before 19 Sep 2026, or the merged copy produced during stabilize. Do not invent new requirements while merging.

**Done when.** `Requirements_v1.0.md` contains §0–§17 as continuous prose, header version 1.6, no “go read the old blob” instruction.

### H3 — Close the stabilize plan

**Decision.** In `StabilizationPlan_v1.0.md`:

- Status → **Closed — implemented 19 Sep 2026**
- Note leftover: H1 XML and H2 SRS merge moved here (they were W7 leftovers)
- Do not reopen W1–W6, W8

**Done when.** A reader of the stabilize plan is not told to implement W7 again.

### H4 — Developers guide pointer

One sentence at the top of the developers guide: shipped surface is SRS v1.6; next library work is this plan, not a host adapter.

No new samples that name a product.

---

## 3. Track L — Holes in *this* type system

These are not product features. They close gaps on types we already ship.

### L1 — Score a finished band against values

**Problem.** `ControlLimits.FromCaller` returns CL/UCL/LCL with `OutOfControlCount = 0` and empty indexes. `Compute` already walks encounter order and fills those fields. Caller-supplied fences cannot answer “which points sit outside this band?” without the host duplicating that loop.

**Decision.** Add scoring on the limits object, not a new type:

```csharp
ControlLimits ControlLimits.Against(IReadOnlyList<decimal> encounterOrder)
```

Rules:

- Copies Center / Upper / Lower / Method / K / Floor / MR fields.
- Recomputes `OutOfControlIndexes` / `OutOfControlCount` the same way `Compute` does (`y > Upper || y < Lower`).
- Does not recompute the fences.
- Empty or null sequence → empty indexes, count 0, no throw.
- `NumericSeries` / `SeriesSlice` may expose a convenience that passes `Values`. That is the same method, not a second contract.

This is how `FromCaller` becomes a complete `ControlLimits`, not a partial one.

**Tests.**

- `FromCaller(12, 30, 0).Against({12, 12, 40})` → indexes `{2}`.
- Scoring does not change Center / Upper / Lower.
- Empty list → count 0.

### L2 — Percentile rank (inverse of `Percentile`)

**Problem.** `Percentile(p)` is value-at-rank. The inverse — rank-of-value — is missing. Without it a caller cannot ask “what fraction of this snapshot is ≤ x?” except by walking `Sorted` themselves.

**Decision.** On `SeriesSlice` (and Full via the series if you want a one-liner):

```csharp
double PercentileRank(decimal x)
```

Rules:

- Empty slice throws the same `InvalidOperationException` as `Percentile`.
- Definition: `(count of values ≤ x) / n`. Ties included.
- Range is `[0, 1]`. A value below Min is `0` only if nothing is ≤ x; a value ≥ Max is `1`.
- This is a sample rank, not a confidence level, not P95-the-SLA-word. XML must say that.

Do **not** add `EncounterIndexOfMax` unless H/L1/L2 are done and it is still one line. Max is already `Values` scanned; not required for wrap-up.

**Tests.**

- `{1..9}.PercentileRank(5)` = `5/9`.
- `{1..9}.PercentileRank(9)` = `1`.
- `{1..9}.PercentileRank(0)` = `0`.
- Empty Q4 throws.

### L3 — Stop

After L1 and L2, stop and re-read the public surface. Do not start L4+ in the same slice.

---

## 4. Later library items (not this slice)

These belong in Analytics when we choose to expand. They still do not belong to a host project. They stay parked until H + L1 + L2 are closed and we look again.

| ID | Item | Why it is library work | Why not now |
|---|---|---|---|
| L4 | Run rules as indexes on Full given a finished `ControlLimits` | Completes Shewhart next to MovingRange. Output is rule id + encounter indexes. Legal only on `SliceKind.Full`. | New contract. Needs its own SRS row and fixtures. |
| L5 | Confidence interval for a percentile | Completes §9 next to mean/median/variance. Method string names the cut (P95), not “95 % confidence.” | Easy to name wrong. Separate design note. |
| L6 | `PdfPoints()` — sampled N(μ, s) on the value axis | Density numbers live with Mean/StdDev, not in a drawing library. | Only when a consumer will bind them. |
| L7 | Two-series compare report (difference of means, interval overlap) | Two `NumericSeries` in, one small report out. | Hosts can hold two series today. New type. |

L4–L7 keep the same constraints as the rest of the library: no charting NuGet, no time-bucket histogram, no streaming, Full-only for process rules, undefined stats are `null` / Try-false, MathNet only through `QuantileFunctions`.

---

## 5. Explicitly out of this library

Not delayed. Not here.

- Any type whose name or XML mentions a product, probe kind, or protocol
- An orchestration / adapter / “communicate with Analytics” project under this folder
- Chart controls, colors, palettes, drawing NuGets
- Time-bucket histograms, rolling windows, EMA, append-one-value
- Shapiro–Wilk as a gate on limits
- Bootstrap as the default mean interval
- Autocorrelation, FFT, regression, ANOVA
- Bayesian intervals, telemetry export, files, workbooks

---

## 6. Implementation slices

| Slice | Items | Theme |
|---|---|---|
| **S4** | H1, H2, H3, H4 | Hygiene only. Build clean. One SRS. Plans updated. |
| **S5** | L1, L2 | Score caller fences; percentile rank. |
| **S6** | stop | Re-evaluate L4–L7 against the surface S5 left. |

S4 is the missing tail of stabilize W7. S5 is the last wrap-up that is still “this type was incomplete.” S6 is a decision, not a coding week.

---

## 7. Acceptance

**S4**

1. Analytics project builds with documentation file generation and no CS1591.
2. `Requirements_v1.0.md` is one v1.6 document with §0–§17.
3. Stabilize plan status is Closed.
4. Developers guide does not point at a host adapter.

**S5**

5. `FromCaller(...).Against(...)` fills indexes; fences unchanged.
6. `PercentileRank` matches the fixtures in L2.
7. No new public type whose job is to wrap another product.
8. Existing stabilize tests stay green.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Hygiene H1–H4, library holes L1–L2, later L4–L7 parked. No host/product work in this library. |
