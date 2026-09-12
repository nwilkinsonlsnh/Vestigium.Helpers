# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.6  
**Status:** Phase 6  
**Date:** 12 September 2026

## Recovery

```csharp
var current = ServiceHelper.GetRecovery("MySvc");
// current.FirstFailure / SecondFailure / SubsequentFailures / ResetPeriod

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

Protected services cannot change recovery. RunCommand requires `Command`.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase6
```
