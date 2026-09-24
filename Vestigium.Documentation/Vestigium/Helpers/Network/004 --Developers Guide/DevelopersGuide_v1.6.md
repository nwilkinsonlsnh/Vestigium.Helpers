# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.6  
**Status:** Current call surface. Contract is [`Requirements_v1.6.md`](Requirements_v1.6.md). Shape is [`Design_v1.6.md`](Design_v1.6.md). Consume package **1.0.1**.  
**Date:** 24 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. Hosts subscribe on Windows or Linux. Not a CLI. Not `ping.exe`. Not a plot package. It will not grow a plot API.

One `net10.0` DLL. References: Json 1.0.1, Analytics 1.0.1, FileIo 1.1.1. Logging from repo `$(VestigiumLoggingVersion)`. No plot package.

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="1.0.1" />
```

Route **print** works on both OS, both families. Route **write** is Option C:

| | Windows | Linux |
|---|---|---|
| IPv4 | IP Helper + optional HKLM persist | Netlink |
| IPv6 | `CreateIpForwardEntry2` | Netlink |
| Default `0.0.0.0/0` or `::/0` | `NetworkRouteDenied` | `NetworkRouteDenied` |
| No admin / no `CAP_NET_ADMIN` | `NetworkRouteDenied` | `NetworkRouteDenied` |

NetBIOS is Windows-only. `NetworkTestHooks` is internal.

This library does not plot. `BandwidthAmount`, `TransferResult`, `PercentileBill`, and campaign results stay network facts.

## Routes

```csharp
var printed = NetworkHelper.GetRoutes(RouteFamily.All);

NetworkHelper.AddRoute(new NetworkRouteChange
{
    Destination = "192.0.2.0",
    PrefixLength = 24,
    Gateway = "192.0.2.1",
    InterfaceIndex = 12, // required on Linux; Windows IPv6: caller >= 1 or first up IPv6 NIC
    Persistent = true    // Windows IPv4 HKLM only
});
```

`2001:db8::/32` is a legal write (same doors). `0.0.0.0/0` and `::/0` throw `NetworkRouteDenied`.

## OUI

```csharp
var live = await NetworkHelper.LookupOuiAsync("00:00:0C:11:22:33");
var packed = NetworkHelper.LookupOuiPacked("00:00:0C:11:22:33"); // offline stub, Source=File
```

Default live host is `api.macvendors.com`. Custom URL needs `AllowCustomRegistry` + allowlist. Packed snapshot is not a live IEEE pull.

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
```

Default mode: 64 MiB × 4 FileIo write probes, P95 → `TransferTime`. Network does not open `FileStream`. No password field.

## ICMP continuous

`Count = 0` needs `MaxDuration` (≤ 24 h). Interval floor 200 ms under one minute, 1 s above. `AllowBurst` is only for the short band.

## Linux CI

The umbrella test project is `net10.0-windows` because that assembly also covers WinReg. That is **repo CI**, not a Network feature. Windows `FullyQualifiedName~Network` is the Network gate. Live Ubuntu route checks wait for a later box (PR05 §4).

## What is not next in this DLL

Scheduler package. HTTP reachability. Demo gallery. Plot API. Full IEEE OUI dump.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 10 Sep 2026 | Phase 8 harden wording. |
| 1.6 | 19 Sep 2026 | Shipped façade. PR01 locks. |
| 1.6 + PR02 | 19 Sep 2026 | Then: Windows write / Linux print. |
| 1.6 + PR03 | 19 Sep 2026 | Share campaigns. Demo skipped. |
| 1.6 + PR04.001 | 19 Sep 2026 | Plotting is not a Network surface. |
| 1.6 + PR05.003 | 19 Sep 2026 | Option C + packed OUI + persist key. |
| 1.6 + PR07.009 | 24 Sep 2026 | Consume 1.0.1. IPv6 IfIndex. Echo recipe. Plot API never offered. |
