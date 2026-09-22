# Vestigium.Helpers.Processes — Developers Guide

**Document ID:** VEST-HLP-PROCESSES-DEV-000  
**Version:** 1.4  
**Status:** Matches the engine on `main` (PR02)  
**Date:** 11 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Campaigns:** [`Campaigns_v1.2.md`](Campaigns_v1.2.md)  
**Backlog:** [`PR02-Rev1-Backlog_Processes.md`](PR02-Rev1-Backlog_Processes.md)

TFM `net10.0-windows`. Façade `ProcessHelper`. APPID `Processes`.

## Call shapes

```csharp
var rows = ProcessHelper.List();
var one  = ProcessHelper.Get(pid);
var hits = ProcessHelper.Search("vestigium", ProcessSearchMode.Contains);
var kql  = ProcessHelper.Search("(PID == 10 || Name LIKE '%edge%') && GPU.Usage GT 20");
var tree = ProcessHelper.GetTree(pid);
var tids = ProcessHelper.GetThreads(pid);
var some = ProcessHelper.SearchThreads(pid, "TID == 12");
var hot  = ProcessHelper.MatchSystem("SYS.ProcessCount GT 0");

using var watch = ProcessHelper.Watch(pid, TimeSpan.FromSeconds(1), ProcessWatchFields.All);
using var qwatch = ProcessHelper.Watch("CPU.Usage GT 5", TimeSpan.FromSeconds(1), ProcessWatchFields.All);
```

Kql `Search` upgrades Slim → Full when the AST names a Full field (`CommandLine`, `Description`, …).  
Term and Kql search sort **Name then Pid** before `maxResults`.

`CPU.Usage` and IO deltas need two samples (query watcher / campaign). First tick is unknown.

## Lifetime

Start-As takes `ProcessStartAs`. Logs `file user domain loadProfile logon`. Never the password.  
`Kill` / `KillTree` / `KillSearch` skip denylist, PPL, and `IntegrityLevel.Protected`. Confirm does not override. `KillTree` skips `AmbiguousParent` children.

## Campaign

In-process only. Host must stay running. No `schtasks`.

Need **Query or Match.Term**. If both are set, Query wins (`Campaign query-overrides-match name=…`).

JSONL process lines include `cpuPercent` and IO/memory deltas (null on the first tick of a window).

## Comments and autostart

Comment persist keys are **SHA-256 of the normalized image path** (SysWOW64/Sysnative → System32, case-folded). The JSON file does not store raw paths as keys.

Autostart is **best-effort**: HKCU/HKLM Run + RunOnce and the user Startup folder. It is not a full Autoruns clone. Missing or unmatched → `None`.

## Logging

APPID `Processes`. Watcher ticks do not write per-sample HelperLog lines. Tick faults log `type=` only.
