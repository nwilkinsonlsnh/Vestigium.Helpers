# Vestigium.Helpers.Analytics — Stabilization Plan

**Document ID:** VEST-HLP-ANALYTICS-PLAN-000  
**Version:** 1.0  
**Status:** Accepted intent — implement before any §16.2 feature  
**Date:** 19 September 2026  
**Binding contract:** `Requirements_v1.0.md` (SRS v1.5)  
**Companion:** `DevelopersGuide_v1.0.md`

Stabilize first, then expand. This plan covers only the tighten-up items from the 19 Sep 2026 review. Run rules, percentile CIs, `PdfPoints()`, and two-series compare stay parked until this plan is done.

---

## 0. Rule for this pass

| Do | Do not |
|---|---|
| Close host footguns | Add Nelson / Western Electric |
| Make existing APIs safe to loop | Add a CI for P95 |
| Freeze snapshot data | Add `PdfPoints()` |
| Align docs with the code | Add two-series compare |
| Isolate MathNet behind one file | Hand-roll t / χ² |
| Keep Charts compiling | Touch ScottPlot / charting NuGets |

Math stays the math already shipped. Behavior changes below are deliberate and listed with a migration note.

---

## 1. Outcome

When this plan is closed:

- A host can walk `series.Bands` without an exception from empty Q2–Q4.
- Moving-range limits exist only where encounter order is a real process (Full).
- Tukey outliers are paint-ready (value **and** encounter index).
- `Values` / `Sorted` / `Times` cannot be mutated behind the snapshot.
- A time-series line follows the clock, not arrival order.
- SRS, developers guide, package metadata, and XML docs all say `Vestigium.Logging`.
- `Confidence.cs` no longer names `MathNet.Numerics.Distributions`.

Then we step back and score §16.2 features against a stable surface.

---

## 2. Work items (implement in this order)

Order is dependency, not importance. W1 unlocks safe band loops. W3 needs the same scan W1 already touches. Docs come last so they describe the code that shipped.

### W1 — Safe control limits on small / empty bands

**Problem.** `ControlLimits.Compute` throws when `n < 2`, `s` is 0/null, or MR̄ is 0. `SeriesSlice.ControlLimits` and `NumericSeries.ControlLimits` always call it. `Q4` on `{5,5,5}` has count 0. A host that does `foreach (var band in series.Bands) band.ControlLimits()` dies. Confidence already returns `IsDefined = false` for the same case.

**Decision.**

- Keep the throwing methods. Explicit “I know this is a process” stays fail-fast.
- Add a parallel try API that never throws for insufficient data:

```csharp
bool SeriesSlice.TryControlLimits(
    out ControlLimits? limits,
    ControlLimitMethod method = MeanPlusKSigma,
    double k = 3,
    double? floor = null)

bool NumericSeries.TryControlLimits(
    out ControlLimits? limits,
    ControlLimitMethod method = MeanPlusKSigma,
    double k = 3,
    double? floor = null)
```

- Return `false` / `limits = null` when the method cannot run (`n < 2`, missing mean, `s` not positive, MR̄ = 0).
- Still throw on caller error: `k <= 0`, `method == CallerSupplied` (must use `FromCaller`), malformed floor band after clamp.
- Do **not** add `IsDefined` to `ControlLimits`. Charts already consumes a fully built object. Null from Try is the undefined signal.

**Files.** `ControlLimits.cs`, `SeriesSlice.cs`, `NumericSeries.cs`, `ControlLimitsTests.cs`.

**Tests.**

- `{5,5,5}`: `TryControlLimits()` is false; throwing `ControlLimits()` still throws.
- `{1}`: same.
- `{1..9}`: Try succeeds and equals the throwing call.
- `k = 0`: Try still throws.
- Empty Q4 on the degenerate series: Try is false, no log-as-unexpected.

**Logging.** Try that returns false logs the existing `LimitsRejected` Error (same reasons as today) and does not log `LimitsThrown`.

---

### W2 — Moving range only on Full

**Problem.** `Q4.ControlLimits(MovingRange)` computes `|x_i − x_{i−1}|` on the filtered Q4 multiset. Those points were not neighbors in the process. That is not a Shewhart individuals chart.

**Decision.**

- `ControlLimitMethod.MovingRange` is legal only on `SliceKind.Full`.
- `Q1` / `Q2` / `Q3` / `Q4` / `Iqr` + MovingRange → `ArgumentException` (throwing API) or `false` (Try API).
- Mean ± kσ remains legal on every non-empty band that has a positive sample s.
- Time-sliced `NumericSeries` (result of `Slice`) is still Full of that window. MR on that Full is legal; the window *is* the process.
- XML docs and the developers guide state: run MR and later run-rules on Full encounter order, never on a value band.

**Files.** `SeriesSlice.cs` (guard before `Compute`), `ControlLimitsTests.cs`, docs in W7.

**Tests.**

- `{1..9}.Full` + MR: unchanged fixture (MR̄ = 1, outside `{0,1,7,8}`).
- `{1..9}.Q4` + MR: throws / Try false.
- `{1..9}.Q4` + MeanPlusKSigma: still works when n ≥ 2 and s > 0.

---

### W3 — Tukey outlier indexes

**Problem.** `HighOutliers` is a list of values. Charts / PingIQ cannot mark the spike on `SampleOrderPoints` or a time plot without scanning `Values` again. `ControlLimits` already publishes indexes. Outliers should too.

**Decision.** Add, on `DescriptiveStatistics` and `SeriesSlice`:

```csharp
IReadOnlyList<int> LowOutlierIndexes   // encounter order in *this slice*
IReadOnlyList<int> HighOutlierIndexes
IReadOnlyList<int> OutlierIndexes      // union, encounter order
```

Rules:

- Indexes are into that slice’s `Values`, not into a parent series.
- On `series.Full` they are series indexes. That is the paint path.
- On `series.Q4` they are indexes into `Q4.Values`.
- Same fence test as today (`< TukeyLowerFence`, `> TukeyUpperFence`).
- Parallel to the value lists: same length, same order.
- Do not invent a new type. Indexes are enough.

**Files.** `DescriptiveStatistics.cs` (single pass that records index + value), `SeriesSlice.cs` (pass-through), `NumericSeriesTests.cs`.

**Tests.**

- `{1,2,3,4,5,100}`: `HighOutliers` contains `100`, `HighOutlierIndexes` is `{5}`.
- `{10,11,11,12,12,12,13,13,14,40}`: high index is `9`.
- No low outliers → empty index list, not null.
- Empty slice → empty lists.

ClosedXml / FileIo keep using `.Count` on the value lists. No required change there.

---

### W4 — Freeze snapshot lists

**Problem.** `Bind` stores a `List<decimal>` as `IReadOnlyList<decimal>`. A host can cast and mutate; `Sorted` and every slice go stale. SRS A5 says the snapshot is immutable.

**Decision.**

- On bind, store `values.ToArray()` (or `AsReadOnly()` on a copy) for `Values`, `Sorted`, and `Times`.
- `DescriptiveStatistics.Values` / `Sorted` are the same story (already arrays in some paths; make it uniform).
- Do not expose the builder list.
- No public setter growth. Keep `private set` only if Bind still needs it; prefer `init` + constructed object if it stays readable.

**Files.** `NumericSeries.cs`, `DescriptiveStatistics.cs`, `NumberConvert.cs` only if the factory list would otherwise leak.

**Tests.**

- After construct, `Assert.Throws` or `Assert.False(series.Values is List<decimal> list && CanWrite)` — simplest check: cast to `IList<decimal>` and `IsReadOnly` is true, or cast to `decimal[]` and document arrays as the freeze form.
- Mutating a copy the caller still holds from their input enumerable does not change `series.Values` (already true if we copy at the door).

---

### W5 — Time series follows the clock

**Problem.** `TimeSeriesPoints()` walks encounter order. Out-of-order probes scribble a line. `PlotBuilder` already does `var timed = series.TimeSeriesPoints()` and uses `timed[0].At` as the origin. If `[0]` is not the earliest clock, the x-axis origin is wrong.

**Decision.**

- `TimeSeriesPoints()` returns timestamped points **sorted by `At.UtcTicks`, then original encounter index** (stable).
- Null `At` still omitted.
- No timestamps → empty list, no throw.
- Encounter order remains `SampleOrderPoints()` plus `Times`.
- Do not add a second time-series API in this pass. One rule, documented.

**Files.** `NumericSeries.cs`, `NumericSeriesTests.cs`, Charts only if a test assumed encounter order (none found; PlotBuilder benefits).

**Tests.**

- Probes at t0+2s, t0, t0+1s → points ordered t0, t0+1s, t0+2s.
- Equal timestamps keep earlier encounter index first.
- Mixed null `At` still dropped.
- Existing slice test (`Count == 3`) still passes.

---

### W6 — Isolate MathNet

**Problem.** The package stays. The call sites should not. `Confidence.cs` is the only file that names `MathNet.Numerics.Distributions`. A9 still binds t / χ² to MathNet.

**Decision.** New internal file:

```text
src/Vestigium.Helpers.Analytics/QuantileFunctions.cs
```

```csharp
internal static class QuantileFunctions
{
    public static double StudentTInv(double df, double p);
    public static double StudentTCdf(double df, double t);
    public static double ChiSquaredInv(double df, double p);
    public static double NormalInv(double p);   // standard normal
}
```

- Only this file may `using MathNet.Numerics.Distributions`.
- Location-scale stay 0, 1 as today (`StudentT.InvCDF(0, 1, df, p)`).
- `Confidence.cs` calls the wrapper only.
- No behavior change. Existing `{1..9}` 95 % mean interval fixture (`2.8949` … `7.1051`) is the lock.

**Files.** new `QuantileFunctions.cs`, `Confidence.cs`, `Vestigium.Helpers.Analytics.csproj` unchanged (same PackageReference).

**Tests.** No new math tests required if `Mean_confidence_interval_uses_student_t` stays green. Optional: wrapper smoke that `NormalInv(0.975)` ≈ 1.95996.

---

### W7 — Docs and package metadata catch-up

**Problem.** SRS §1 / §11 still say `HelperLog`. Implementation and the developers guide use `Vestigium.Logging` + `AnalyticsCatalog` (EVENTID 10500+). Package tags in the SRS omit control-chart. Most public members have no `///` because `CS1591` is suppressed.

**Decision.** Docs follow code. Do not resurrect HelperLog.

| Doc | Change |
|---|---|
| SRS §1, A3, §11, §16.1 | Replace HelperLog with `Vestigium.Logging`. Hosts call `VestigiumLogger.Initialize` + `AnalyticsCatalog.Register`. Library never initializes. |
| SRS §4 / §8.2 / §10.1 | Add TryControlLimits, outlier indexes, TimeSeriesPoints sort rule, MR-on-Full-only. |
| SRS §17 | Version 1.6 row: stabilization pass. |
| Developers guide | Same logging sentence. Show `TryControlLimits`. Warn MR is Full-only. Note `MeanConfidenceLevelContaining` is just-covering γ, not “sample confidence.” |
| csproj | Description already mentions control limits. Align SRS package tags with csproj: `analytics;statistics;quartile;confidence-interval;control-chart;shewhart;vestigium`. |
| XML docs | Required on every public type and public member hosts call. Leave `CS1591` **on** for this project after the comments exist (remove it from `NoWarn` here only). |

Public surface that must have `///`:

- `AnalyticsHelper`, `NumericSeries`, `SeriesSlice`
- `Observation`, `SeriesWindow`, `SeriesWindowKind`, `SliceKind`
- `ConfidenceLevel`, `ConfidenceInterval`, `ConfidenceReport`
- `ControlLimits`, `ControlLimitMethod`
- `FrequencyTable`, `FrequencyBin`, `HistogramBin`
- `ChartPoint`, `ParetoPoint`, `TimedValue`
- `AnalyticsCatalog`, `AnalyticsEvents` (short: catalog block, register in host init)

Inner loops stay silent. Do not document `DescriptiveStatistics` publicly (it stays internal).

**Files.** both `_Documentation` files, `Vestigium.Helpers.Analytics.csproj`, the public `.cs` files above.

---

### W8 — Test net for the stabilize pass

Additions that are not owned by a single W item, all in `NumericSeriesTests` / `ControlLimitsTests` (extend, do not invent a third file unless the file gets noisy).

| Test | Why |
|---|---|
| `NamedPercentiles()` keys and values equal `Percentile(p)` | SRS acceptance #9, currently untested |
| `ProportionAbove(7)` on `{1..9}` → estimate `2/9` | SRS acceptance #14 |
| `SampleSizeForMeanMargin(0.5)` on `{1..9}` returns n ≥ 2; margin `≤ 0` throws | shipped API with no test |
| Empty band `Confidence()` → report with `IsDefined = false`, no throw | already specified, pin it |
| `Q4.Percentile` / `NamedPercentiles` on empty → throw (SRS 5.2) | pin reject |

Do not start run-rule tests in this pass.

---

## 3. Suggested implementation slices

Ship as three commits (or three PRs) so each is reviewable.

| Slice | Items | Theme |
|---|---|---|
| **S1** | W4, W6 | Internals: freeze lists, wrap MathNet. No host-visible behavior except immutability. |
| **S2** | W1, W2, W3, W5, W8 | Host-visible tighten: Try limits, MR guard, outlier indexes, clock order, tests. |
| **S3** | W7 | Docs + XML + package tags. Last so they describe S1+S2. |

S1 can merge alone. S2 is the user-facing stabilize. S3 closes the plan.

---

## 4. Acceptance for “this plan is done”

1. All existing Analytics / ControlLimits / Charts tests still green.
2. New tests in W1–W5 and W8 green.
3. `grep MathNet.Numerics.Distributions` inside Analytics returns only `QuantileFunctions.cs`.
4. `series.Values is List<decimal>` is false (or the list is read-only).
5. `NumericSeries.From(new[] { 5, 5, 5 }).Q4.TryControlLimits(out _)` is false.
6. `NumericSeries.From(Enumerable.Range(1, 9)).Q4.ControlLimits(MovingRange)` throws `ArgumentException`.
7. `{1,2,3,4,5,100}.Full.HighOutlierIndexes` equals `{5}`.
8. Out-of-order timestamps come out of `TimeSeriesPoints()` in clock order.
9. SRS no longer mentions HelperLog as the Analytics door.
10. Building the project with XML-doc warnings enabled for Analytics produces no CS1591 on public members.

---

## 5. Out of scope (parked)

Do not pull these in “while we are here”:

- Nelson / Western Electric run rules (§16.2 v1.5)
- Confidence interval for a percentile (§16.2 v1.5)
- `PdfPoints()` (§16.2 v1.6)
- Two-series compare (§16.2 v1.6)
- Shapiro–Wilk / Anderson–Darling
- Bootstrap / BCa mean interval
- Changing MathNet version or dropping the package
- Time-bucket histograms, streaming, charting NuGet

After S3, stop and evaluate those against the stabilized API.

---

## 6. Risk notes

| Change | Risk | Mitigation |
|---|---|---|
| `TimeSeriesPoints` sort | Charts x-origin moves if probes were out of order | That is the bugfix. PlotBuilder uses `timed[0]` as t0. |
| Try vs throw on limits | Two ways to call the same math | Throwing methods stay; Try is additive. |
| MR rejected on bands | A host already calling `Q4.ControlLimits(MovingRange)` breaks | Unlikely (Q4 n is often tiny); the throw message must say use Full. |
| Enable CS1591 | Noise from internal leftovers | Keep internals undocumented; only public API is in scope. |
| Freeze to arrays | Someone cast to `List<decimal>` | No current in-repo caller does. |

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | First plan. Stabilize W1–W8 before any §16.2 feature. |
