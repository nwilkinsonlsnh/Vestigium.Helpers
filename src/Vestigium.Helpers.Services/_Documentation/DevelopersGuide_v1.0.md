# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.2  
**Status:** Phase 2  
**Date:** 11 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Services/`.

## Phase 1 API

```csharp
ServiceHelper.Identity;
ServiceHelper.Probe();
ServiceHelper.List();
ServiceHelper.Get("EventLog");
ServiceHelper.Search("sql", ServiceSearchMode.Contains);
```

## Phase 2 API

```csharp
var slim = ServiceHelper.Get("EventLog", ServiceDetailLevel.Slim, joinProcess: false);
// slim.ImagePath, slim.Account, slim.StartType, slim.DelayedAutoStart, slim.DesktopInteract

var full = ServiceHelper.Get("EventLog", ServiceDetailLevel.Full, joinProcess: false);
// + Description, FailureActions, FailureResetPeriod, FailureCommand

ServiceHelper.Search("LocalSystem", ServiceSearchMode.Contains, ServiceSearchFields.Account);
ServiceHelper.Search("windows", ServiceSearchMode.Contains, ServiceSearchFields.ImagePath);
```

`LocalSystem` / `NT AUTHORITY\SYSTEM` normalize to `LocalSystem`. There is no password on the snapshot.

Hidden lists and trees are Phase 3. Control verbs are Phase 4.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase2
```
