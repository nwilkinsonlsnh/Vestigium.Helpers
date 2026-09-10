# Vestigium.Helpers.Processes — Developers Guide

**Document ID:** VEST-HLP-PROCESSES-DEV-000  
**Version:** 1.1  
**Status:** Draft companion to SRS v1.1  
**Date:** 10 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Processes/`. This file is the design companion: how to call the façade once the SRS is accepted. The SRS wins if the two disagree.

## Current tree

| Path | Role |
|---|---|
| `ProcessHelper.cs` | Skeleton façade (`Identity`, `Probe`) |
| `_Documentation/Requirements_v1.0.md` | Lossless contract (v1.1 draft) |
| `../Vestigium.Helpers.Processes.Demo/` | Shared gallery chrome until the Processes tabs land |

Do not grow `ProcessHelper` until SRS v1.1 is Accepted.

## Intended call shapes (after acceptance)

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

## Native notes (implementation, not SRS)

- Slim list should prefer `NtQuerySystemInformation` / toolhelp plus `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` so protected rows still appear.
- Command line: PEB read when the token allows it; otherwise `Availability = Denied`.
- Signer: WinVerifyTrust / catalog. Do not call `sigcheck.exe`.
- GPU: PDH GPU Engine counters or the documented adapter APIs. Missing counter set → `Unsupported`.
- Paging lists: `NtQuerySystemInformation` system cache / memory list classes when present.
- Start-As: create-with-logon. No credential UI in this library.
- Watchers: one timer per watcher, skip overrun ticks, raise `Exited` when `Get` returns null.

## Logging

APPID `Processes`. Sparse. See SRS §12. Tests inject `LogDirectory`.

## TFM

SRS v1.1 proposes `net10.0-windows`. Do not flip the csproj until the SRS is Accepted (open item O1). Until then the skeleton stays `net10.0` so Linux CI still restores the solution.

## Roadmap pointer

SRS §16. Next document after acceptance is an implementation plan, same pattern as FileIo.
