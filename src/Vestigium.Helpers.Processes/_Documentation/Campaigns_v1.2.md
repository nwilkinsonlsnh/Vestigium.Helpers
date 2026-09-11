# Vestigium.Helpers.Processes — Campaign JSONL and windows

**Document ID:** VEST-HLP-PROCESSES-CAMP-000  
**Version:** 1.3  
**Status:** Accepted (PR02 Phase D)  
**Date:** 11 September 2026  
**Parent SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

---

## 1. Purpose

Leave the **host process** running. Sample counters only inside named local-time windows. This library is **in-process only**. It is not Task Scheduler. If the host is not running at 08:00, that window is missed.

---

## 2. Locks

| # | Lock |
|---|---|
| 1 | In-process runner. Host stays alive. No `schtasks`. |
| 2 | Filter is **Query** (Kql) and/or **Match** (StartsWith/EndsWith/Contains). At least one is required. If both are set, **Query wins**. |
| 3 | Samples = `{CampaignRoot}\{name}\samples.jsonl`. Recipe = `recipe.json`. |
| 4 | Default root `%ProgramData%\Vestigium\Processes\Campaigns\`. Tests inject `ProcessTestHooks.CampaignRoot`. |
| 5 | Sample interval 250 ms–60 s. Window duration 1 minute–24 hours. |
| 6 | `MaxMatches` default 64, max 256. |
| 7 | Previous-sample map lives only while a window is open. Closing every window clears deltas. |

---

## 3. Recipe

```text
Name
Query?                          // Kql filter; optional if Match.Term is set
Match?                          // term + mode; optional if Query is set
Fields, IncludeSystemCounters, SampleInterval, Windows, TimeZoneId, MaxMatches
```

---

## 4. JSONL process line

`kind`, `campaign`, `windows`, `ts`, `pid`, `name`, `imagePath`, `cpuTime`, `cpuPercent`, `privateBytes`, `privateBytesDelta`, `workingSet`, `ioReadBytes`, `ioWriteBytes`, `ioReadBytesDelta`, `ioWriteBytesDelta`.

`cpuPercent` and `*Delta` are `null` on the first tick of a window (no previous sample).
