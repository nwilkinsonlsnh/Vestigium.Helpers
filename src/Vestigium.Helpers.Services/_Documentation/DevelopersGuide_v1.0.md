# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.1  
**Status:** Phase 1  
**Date:** 11 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Services/`.

## Phase 1 API

```csharp
ServiceHelper.Identity;          // "Vestigium.Helpers.Services"
ServiceHelper.Probe();           // read-only count + EventLog lookup

var rows = ServiceHelper.List(); // visible Win32, slim
var one  = ServiceHelper.Get("EventLog");
ServiceHelper.TryGet("Spooler", out var spooler);

var hits = ServiceHelper.Search("sql", ServiceSearchMode.Contains);
hits = ServiceHelper.Search("Event", ServiceSearchMode.StartsWith, ServiceSearchFields.Name);
hits = ServiceHelper.Search("Log", ServiceSearchMode.EndsWith, ServiceSearchFields.Name);
```

`List()` does not include hidden services. That is Phase 3.

Control, logon, recovery, KQL, and campaigns are later phases. Do not call them from Phase 1 tests except as not-yet-gated scaffolding.

## Build

```
dotnet build src/Vestigium.Helpers.Services/Vestigium.Helpers.Services.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase1
```
