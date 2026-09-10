# Vestigium.Helpers.Processes — Developers Guide

**Document ID:** VEST-HLP-PROCESSES-DEV-000  
**Version:** 1.3  
**Status:** Companion to accepted SRS v1.2. Matches the engine on `main`.  
**Date:** 10 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)  
**Campaigns:** [`Campaigns_v1.2.md`](Campaigns_v1.2.md)

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Processes/`.

## Current tree

| Path | Role |
|---|---|
| `ProcessHelper.cs` | Façade: List / Get / Search / tree / threads / watch / start / kill / campaign |
| `ProcessSnapshotter.cs` | Slim + full row capture |
| `ProcessFullReader.cs` | Signer, PE image type, PEB command line, autostart |
| `ProcessTreeWalker.cs` | Live PPID walk + cycle guard |
| `ProcessThreadReader.cs` | Thread snapshot |
| `ProcessWatcher.cs` / `SystemWatcher.cs` | Interval samples, first tick null deltas |
| `SystemCounterReader.cs` | Commit, physical, kernel, paging, topology |
| `ProcessGpuCatalog.cs` | GPU Engine / Adapter / Process Memory (750 ms cache) |
| `ProcessCampaign.cs` | In-process windows + `samples.jsonl` |
| `../Vestigium.Helpers.Processes.Demo/` | WPF gallery: Processes, Watch, Threads, System, Start/Kill, Campaign, JSONL |

TFM is `net10.0-windows`.

## Call shapes

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

## Campaign

```csharp
var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
{
    Name = "day-parts",
    Match = new ProcessSearchRequest { Term = "vestigium", Mode = ProcessSearchMode.Contains },
    SampleInterval = TimeSpan.FromSeconds(1),
    IncludeSystemCounters = true,
    Windows =
    [
        new("midnight", new TimeOnly(0, 0),  TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
        new("morning",  new TimeOnly(8, 0),  TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
        new("noon",     new TimeOnly(12, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All),
    ]
});
campaign.Sampled += (_, tick) => { /* UI; file already appended */ };
await campaign.RunAsync(cancellation);
```

Default disk: `%ProgramData%\Vestigium\Processes\Campaigns\{name}\recipe.json` and `samples.jsonl`.
Tests set `ProcessTestHooks.CampaignRoot` and optionally `ProcessTestHooks.Now`.
The host must stay running. This library does not install a scheduled task.

`12:00` is noon. Midnight is `00:00`.

## Logging

APPID `Processes`. Sparse. Watcher ticks do not write HelperLog sample lines.
Registered subcategories: `Inventory`, `Process`, `Thread`, `Watch`, `Start`, `Kill`, `System`, `Campaign`, `Jsonl`.

## Native notes

- Slim list: `Process.GetProcesses` plus limited query so protected rows still appear.
- Missing fields: `Availability` Denied / Unsupported, never fake 0 for GPU.
- Signer: WinVerifyTrust. No `sigcheck.exe`.
- Start-As: create-with-logon. No credential UI.
- Campaign scheduler: in-process only. No `schtasks`.
