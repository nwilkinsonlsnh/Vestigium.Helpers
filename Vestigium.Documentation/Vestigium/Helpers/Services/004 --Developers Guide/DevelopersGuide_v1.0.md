# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.8  
**Status:** Phase 8  
**Date:** 12 September 2026

Open `Vestigium.Helpers.slnx`. Library: `src/Vestigium.Helpers.Services/`. TFM `net10.0-windows`.

## Inventory

```csharp
ServiceHelper.Probe();
ServiceHelper.List();                                  // visible Win32
ServiceHelper.List(kind: ServiceKind.Driver);
ServiceHelper.ListHidden();
ServiceHelper.List(scope: ServiceListScope.All);
ServiceHelper.Get("EventLog");                         // Full by default
ServiceHelper.Search("sql", ServiceSearchMode.Contains);
ServiceHelper.Search("Name LIKE 'sql%'");              // KqlPack.Service
```

## Tree

```csharp
ServiceHelper.GetDependsOn("EventLog");
ServiceHelper.GetDependedBy("EventLog");
ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.Both).Flatten();
```

Cycles / depth 16 / 256 nodes set `AmbiguousDependency` and stop that branch.

## Control

Returns `ServiceControlResult`. Does not throw on Access Denied.

```csharp
ServiceHelper.Start("Spooler");
ServiceHelper.Stop("Spooler", confirmDependents: true);
ServiceHelper.Restart("Spooler", confirmDependents: true);
ServiceHelper.Pause("wuauserv");
ServiceHelper.Continue("wuauserv");
ServiceHelper.SetStartType("Spooler", ServiceStartType.AutomaticDelayed, confirm: true);
```

`ServiceHelper.ProtectedNames` cannot be Stopped, Restarted, or have StartType / Logon / Recovery changed. EventLog is on that list.

## Logon

```csharp
ServiceHelper.SetLogon("MySvc", new ServiceLogonRequest
{
    Kind = ServiceLogonKind.LocalSystem,
    InteractWithDesktop = true,    // LocalSystem + own-process only
    Confirm = true
});

ServiceHelper.SetLogon("MySvc", new ServiceLogonRequest
{
    Kind = ServiceLogonKind.Account,
    Account = @"DOMAIN\svc-my",
    Password = secret,             // never on ServiceInfo, never JSONL, log is password=***
    GrantLogonRight = ServiceGrantLogonRight.Service,
    Confirm = true
});

ServiceHelper.QueryLogonRights(@"DOMAIN\svc-my");
```

## Recovery

```csharp
var rec = ServiceHelper.GetRecovery("MySvc");
ServiceHelper.SetRecovery("MySvc", new ServiceRecoveryRequest
{
    FirstFailure = ServiceFailureActionKind.Restart,
    SecondFailure = ServiceFailureActionKind.Restart,
    SubsequentFailures = ServiceFailureActionKind.None,
    ActionDelay = TimeSpan.FromMinutes(1),
    ResetPeriod = TimeSpan.FromDays(1),
    Confirm = true
});
```

## Watch and campaigns

```csharp
using var watch = ServiceHelper.Watch("EventLog", TimeSpan.FromSeconds(1));
using var q = ServiceHelper.WatchQuery("Name LIKE 'sql%'", TimeSpan.FromSeconds(2));

var campaign = ServiceHelper.CreateCampaign(new ServiceCampaignRecipe
{
    Name = "sql-day",
    Query = "Name LIKE 'sql%' || DisplayName LIKE '%SQL%'",
    SampleInterval = TimeSpan.FromSeconds(5),
    Windows =
    [
        new("midnight", new TimeOnly(0, 0), TimeSpan.FromMinutes(10), ServiceCampaignDays.All),
        new("morning", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ServiceCampaignDays.Weekdays),
        new("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(10), ServiceCampaignDays.All)
    ]
});
await campaign.RunAsync(stoppingToken);
```

Live samples: `%ProgramData%\Vestigium\Services\Campaigns\<name>\samples.jsonl`  
Tests must set `ServiceTestHooks.CampaignRoot`. `CreateCampaign` overwrites `recipe.json`.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase
```
