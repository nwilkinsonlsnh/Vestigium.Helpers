# Vestigium.Helpers.Processes — Requirements

**Document ID:** VEST-HLP-PROCESSES-SRS-000  
**Version:** 1.3  
**Status:** Current. Incorporates the v1.3 addendum. Archived v1.0 lives under 000.  
**Date:** 21 September 2026  
**Package:** `Vestigium.Helpers.Processes`  
**TFM:** `net10.0-windows`

Companion addendum: [`Requirements_v1.3_Addendum.md`](Requirements_v1.3_Addendum.md).

---

## 1. Purpose

Give a Windows host one façade (`ProcessHelper`) for the process table: list, get, search, tree, threads, watch, start, stop, comments, autostart, campaigns. Kql is the filter dialect. This package owns the rows.

---

## 2. Locked decisions

| # | Decision |
|---|---|
| 1 | Windows only. |
| 2 | Missing PID: `Get` / `TryGet` return null / false. No fake row. |
| 3 | Term search is StartsWith / EndsWith / Contains. Kql search is `Search(query)`. |
| 4 | `maxResults` 1..4096. Campaign `MaxMatches` 1..256. |
| 5 | Watch interval 250 ms through 60 s. |
| 6 | Kill skips denylist, PPL, and `IntegrityLevel.Protected`. Confirm does not override. |
| 7 | Never log the Start-As password. Never log per-tick watcher samples. |
| 8 | Campaigns are in-process. No `schtasks`. Host must stay running. |
| 9 | Comment persist keys are SHA-256 of the normalized image path. |
| 10 | Autostart is best-effort (Run / RunOnce / Startup folder), not Autoruns. |
| 11 | Library never calls `VestigiumLogger.Initialize`. APPID `Processes`. |
| 12 | Kql pack Process for process search; Thread / System for those helpers. |

---

## 3. Goals

**G1.** List / Get / Search the table.  
**G2.** Tree and threads for one PID.  
**G3.** Watch one PID or a Kql query.  
**G4.** Start / StartAs / Kill with fail-closed denies.  
**G5.** Campaign windows write JSONL samples.  
**G6.** Field availability is Ok / Denied / Unsupported / Gone — not empty strings pretending to be values.

---

## 4. Still out

Linux process table, scheduled-task campaigns, dumping protected memory, a full Autoruns clone.
