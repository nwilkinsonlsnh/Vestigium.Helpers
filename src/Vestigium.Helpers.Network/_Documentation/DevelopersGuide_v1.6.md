# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.6  
**Status:** Current call surface. Contract is [`Requirements_v1.6.md`](Requirements_v1.6.md). Shape is [`Design_v1.6.md`](Design_v1.6.md).  
**Date:** 19 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. Hosts subscribe on Windows or Linux. Not a CLI. Not `ping.exe`. Not a charting package.

One `net10.0` DLL. References: Json, Analytics, FileIo. **Does not reference Charts.**

Route **print** works on both OS. Route **mutate** is Windows IPv4 only in v1. Linux and IPv6 write throw `NetworkRouteDenied`. NetBIOS is Windows-only.

`NetworkTestHooks` is internal.

## Charts stay on the host (PR04.001)

Network returns numbers: `BandwidthAmount`, `TransferResult`, `PercentileBill`, `ShareCampaignResult` (payload hours, metadata hours, declared-pipe hours, disclaimer).

A host that wants a plot calls `Vestigium.Helpers.Charts` itself. Do not add a Charts project reference here. Do not add `Chart*` / `Plot*` doors on `NetworkHelper`.

## Share campaigns

```csharp
var analysis = FileIoHelper.AnalyzeDirectory(sourceDir);
var plan = NetworkHelper.PlanShareProbe(analysis);
var campaign = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
{
    Target = new FileShareTarget { Directory = shareDir },
    Mode = ShareCampaignMode.Advanced,
    SourceAnalysis = analysis
});
var result = await campaign.RunAsync();
// result.PayloadDuration + result.MetadataDuration == result.MeasuredDuration
// result.DeclaredPipeDuration is optional and separate
```

Default mode: 64 MiB × 4 write probes via FileIo, P95 → `TransferTime(plannedSize)`. Network does not open `FileStream`. No password field on `FileShareTarget`.

## Public surface (additions)

| Method | Notes |
|---|---|
| `PlanShareProbe` | Five FileIo buckets. Metadata step when the mix is many-small. |
| `CreateShareCampaign` / `OpenShareCampaign` | Recipe/results under campaign root. Share directory confined when `ShareRoot` is set (tests). |

Existing ICMP / DNS / route / MAC / bandwidth doors are unchanged from PR02.

## Linux CI

Dated **10 September 2026**. The umbrella test project is `net10.0-windows` because that assembly also covers WinReg and, on Windows, Charts. That is **repo CI**, not a Network feature. A later workflow PR may add a portable `net10.0` test project. Until then Windows `FullyQualifiedName~Network` is the Network gate.

## What is not next in this DLL

Linux netlink / IPv6 route write unless PR04 Option B/C is chosen in writing on [`PR04_ImplementationPlan.md`](PR04_ImplementationPlan.md). Packed OUI file is optional (PR04.002). Scheduler package. HTTP client. Demo gallery. Charts.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 10 Sep 2026 | Phase 8 harden wording. |
| 1.6 | 19 Sep 2026 | Shipped façade. PR01 locks. |
| 1.6 + PR02 | 19 Sep 2026 | Route contract. Linux ICMP = BCL Ping. |
| 1.6 + PR03 | 19 Sep 2026 | Share campaigns. Demo skipped. |
| 1.6 + PR04.001 | 19 Sep 2026 | No Charts reference. Hosts plot results. |
