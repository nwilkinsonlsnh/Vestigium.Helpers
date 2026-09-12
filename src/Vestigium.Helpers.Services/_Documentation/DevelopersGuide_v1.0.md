# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.7  
**Status:** Phase 7  
**Date:** 12 September 2026

## Watch

```csharp
using var watch = ServiceHelper.Watch("EventLog", TimeSpan.FromSeconds(1));
watch.Sampled += (_, rows) => { /* status + pid */ };

using var q = ServiceHelper.WatchQuery("Name LIKE 'sql%'", TimeSpan.FromSeconds(2));
```

## Campaign

```csharp
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

Samples: `%ProgramData%\Vestigium\Services\Campaigns\sql-day\samples.jsonl`

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase7
```
