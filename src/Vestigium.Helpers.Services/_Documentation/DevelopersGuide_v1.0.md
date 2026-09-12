# Vestigium.Helpers.Services — Developers Guide

**Document ID:** VEST-HLP-SERVICES-DEV-000  
**Version:** 1.5  
**Status:** Phase 5  
**Date:** 12 September 2026

## Logon

```csharp
ServiceHelper.SetLogon("MySvc", new ServiceLogonRequest
{
    Kind = ServiceLogonKind.LocalSystem,
    InteractWithDesktop = true,   // LocalSystem + own-process only
    Confirm = true
});

ServiceHelper.SetLogon("MySvc", new ServiceLogonRequest
{
    Kind = ServiceLogonKind.Account,
    Account = @"DOMAIN\svc-my",
    Password = password,          // never logged, never on ServiceInfo
    GrantLogonRight = ServiceGrantLogonRight.Service, // opt-in
    Confirm = true
});

var rights = ServiceHelper.QueryLogonRights(@"DOMAIN\svc-my");
ServiceHelper.GrantLogonRights(@"DOMAIN\svc-my", ServiceGrantLogonRight.ServiceAndBatch);
```

Protected services (EventLog, RpcSs, …) cannot change logon.

## Build

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~ServicePhase5
```
