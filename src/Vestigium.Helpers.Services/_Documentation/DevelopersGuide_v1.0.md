# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.3  
**Status:** Phase 3  
**Date:** 11 September 2026

## Lists

```csharp
ServiceHelper.List();                                 // visible Win32
ServiceHelper.List(kind: ServiceKind.Driver);         // visible drivers
ServiceHelper.ListHidden();                           // registry-only
ServiceHelper.List(scope: ServiceListScope.All);      // union, IsHidden set
```

## Tree

```csharp
var down = ServiceHelper.GetDependsOn("EventLog");
var up   = ServiceHelper.GetDependedBy("EventLog");
var tree = ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.Both);
var flat = tree.Flatten();
```

Cycles and depth > 32 set `AmbiguousDependency` and stop that branch.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase3
```
