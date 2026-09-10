# Vestigium.Helpers.Processes — Developers Guide

**Document ID:** VEST-HLP-PROCESSES-DEV-000  
**Version:** 1.2  
**Status:** Companion to accepted SRS v1.2  
**Date:** 10 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Processes/`. The SRS wins if this file and the SRS disagree.

## Current tree

| Path | Role |
|---|---|
| `ProcessHelper.cs` | Skeleton façade (`Identity`, `Probe`) |
| `_Documentation/Requirements_v1.0.md` | Accepted SRS v1.2 |
| `_Documentation/ImplementationPlan_v1.0.md` | Phase scoreboard |
| `../Vestigium.Helpers.Processes.Demo/` | Shared gallery chrome until the Processes tabs land |

Do not grow `ProcessHelper` until Plan Phase 1. Phase 0 is paper + taxonomy only.

## Intended call shapes

```csharp
var rows = ProcessHelper.List();
var one  = ProcessHelper.Get(pid);
var hits = ProcessHelper.Search("vestigium", ProcessSearchMode.Contains);
var tree = ProcessHelper.GetTree(pid);
var tids = ProcessHelper.GetThreads(pid);

using var watch = ProcessHelper.Watch(pid, TimeSpan.FromSeconds(1), ProcessWatchFields.All);
watch.Sampled += (_, sample) => { /* marshal to UI */ };

var started = ProcessHelper.Start(new ProcessStartRequest { FileName = @"C:\Windows\System32\notepad.exe" });
var killed  = ProcessHelper.Kill(started.Pid);
```

Start-As takes `ProcessStartAs`. Passwords never go through `HelperLog`.

## Campaign call shape (Phase 7)

```csharp
var recipe = new ProcessCampaignRecipe
{
    Name = "day-parts",
    Match = new ProcessSearchRequest("vestigium", ProcessSearchMode.Contains),
    SampleInterval = TimeSpan.FromSeconds(1),
    IncludeSystemCounters = true,
    Windows =
    [
        new("midnight", new TimeOnly(0, 0),  TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
        new("morning",  new TimeOnly(8, 0),  TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
        new("noon",     new TimeOnly(12, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
    ]
};

var campaign = ProcessHelper.CreateCampaign(recipe);
campaign.Sampled += (_, tick) => { /* UI; file already appended */ };
await campaign.RunAsync(cancellation);
```

Default disk: `%ProgramData%\Vestigium\Processes\Campaigns\day-parts\recipe.json` and `samples.jsonl`. Tests set `ProcessTestHooks.CampaignRoot`. The host process must stay running; this library does not install a scheduled task.

`12:00` is noon. Midnight is `00:00`. Recipes store `TimeOnly`, not AM/PM strings.

## Native notes (implementation, not SRS)

- Slim list: toolhelp / `NtQuerySystemInformation` plus `PROCESS_QUERY_LIMITED_INFORMATION` so protected rows still appear.
- Command line: PEB read when allowed; otherwise `Availability = Denied`.
- Signer: WinVerifyTrust / catalog. Do not call `sigcheck.exe`.
- GPU: PDH GPU Engine counters or documented adapter APIs. Missing → `Unsupported`.
- Start-As: create-with-logon. No credential UI.
- Watchers: one timer, skip overrun ticks, raise `Exited` when `Get` returns null.

## Logging

APPID `Processes`. Sparse. SRS §12. Tests inject `LogDirectory`.

## TFM

O1 accepted: `net10.0-windows`. Flip the csproj in **Phase 1**, not Phase 0.

## Roadmap

Build mode is [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md). One phase per commit.
