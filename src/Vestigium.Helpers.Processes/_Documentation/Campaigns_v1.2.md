# Vestigium.Helpers.Processes — Campaign JSONL and windows

**Document ID:** VEST-HLP-PROCESSES-CAMP-000  
**Version:** 1.2  
**Status:** Accepted (normative addendum to SRS v1.2)  
**Date:** 10 September 2026  
**Parent SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)

If this file and the SRS disagree on campaigns, **this file wins**. Implementation is Plan Phase 7.

---

## 1. Purpose

Leave the gallery or ProbeHost running. Sample counters only inside named local-time windows. Subscribers see live ticks. An append-only JSONL keeps the history for later review.

Example:

```text
midnight  00:00  → 10 minutes of counters
morning   08:00  → 10 minutes of counters
noon      12:00  → 10 minutes of counters
```

`12:00` is noon. Midnight is `00:00`. Recipes store `TimeOnly`, not AM/PM strings.

The match set is the same search API as the SRS: StartsWith / EndsWith / Contains.

---

## 2. Locks

| # | Lock |
|---|---|
| 1 | In-process runner. Host stays alive. No `schtasks`, no Task Scheduler COM, no Windows service in this library. |
| 2 | Two channels. Audit = HelperLog. Samples = `{CampaignRoot}\{name}\samples.jsonl`. |
| 3 | Recipe = `{CampaignRoot}\{name}\recipe.json` via `Vestigium.Helpers.Json`. |
| 4 | Default root `%ProgramData%\Vestigium\Processes\Campaigns\`. Tests inject `ProcessTestHooks.CampaignRoot`. |
| 5 | Sample interval 250 ms–60 s, default 1 s. |
| 6 | Window duration 1 minute–24 hours. Empty window list is rejected (use `Watch` for continuous). |
| 7 | Match re-evaluated every tick. Mid-window births are included. |
| 8 | `MaxMatches` default 64, max 256. Overflow logs `Campaign` / Warning / `Truncated`. |
| 9 | Identity on a line: pid + startTimeUtc + name + imagePath. |
| 10 | If the host is not running, the window is missed. No sleep-wake. |

---

## 3. Recipe

```text
sealed class ProcessCampaignRecipe
{
    string Name;
    ProcessSearchRequest Match;     // term + StartsWith|EndsWith|Contains + fields
    ProcessWatchFields Fields;      // default All watchable resource fields
    bool IncludeSystemCounters;     // default true
    TimeSpan SampleInterval;        // default 1 s
    IReadOnlyList<ProcessCampaignWindow> Windows;
    TimeZoneInfo TimeZone;          // default Local
    int MaxMatches;                 // default 64, max 256
}

readonly record struct ProcessCampaignWindow(
    string Name,
    TimeOnly StartLocal,
    TimeSpan Duration,
    ProcessCampaignDays Days);
```

Overlap is allowed. One tick lists every open window name. A window that wraps midnight is `[start, start+duration)` in local time.

Days flags: Sunday…Saturday, Weekdays, Weekend, All.

---

## 4. Subscribe and file

```text
sealed class ProcessCampaign : IDisposable
{
    string CampaignId { get; }
    ProcessCampaignRecipe Recipe { get; }
    ProcessCampaignState State { get; }    // Idle, Waiting, Sampling, Stopped
    string SamplePath { get; }
    event EventHandler<ProcessCampaignTick>? Sampled;
    event EventHandler<ProcessCampaignWindowEvent>? WindowChanged;
    Task RunAsync(CancellationToken cancellation = default);
    void Stop();
}
```

Façade: `ProcessHelper.CreateCampaign` / `ListCampaigns` / `LoadCampaign`.

JSONL is append-only. One compact line per process row per tick, plus one `kind=system` line when `IncludeSystemCounters` is true.

HelperLog writes start, window-open, window-close, truncated, stop. Not per sample.

---

## 5. Seed recipe (Demo)

```text
Name = "day-parts"
Match = Contains "vestigium" on Name|ImagePath
Interval = 1 s
IncludeSystemCounters = true
Windows =
  midnight  00:00  10 min  All days
  morning   08:00  10 min  All days
  noon      12:00  10 min  All days
```
