# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.4  
**Status:** Phase 4  
**Date:** 11 September 2026

## Control

```csharp
ServiceHelper.Start("Spooler");
ServiceHelper.Stop("Spooler", confirmDependents: true);
ServiceHelper.Restart("Spooler", confirmDependents: true);
ServiceHelper.Pause("wuauserv");     // Unsupported if the service cannot pause
ServiceHelper.Continue("wuauserv");
ServiceHelper.SetStartType("Spooler", ServiceStartType.AutomaticDelayed, confirm: true);
```

Every verb returns `ServiceControlResult`. Access Denied and missing names are statuses, not exceptions.

`ServiceHelper.ProtectedNames` cannot be Stopped, Restarted, or have StartType changed. EventLog is on that list.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase4
```
