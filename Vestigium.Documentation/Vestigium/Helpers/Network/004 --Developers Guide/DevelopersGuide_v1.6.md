# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.6  
**Status:** Current call surface. Contract is [`Requirements_v1.6.md`](Requirements_v1.6.md). Shape is [`Design_v1.6.md`](Design_v1.6.md). Consume package **1.2.0**.  
**Date:** 24 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. Hosts subscribe on Windows or Linux. Not a CLI. Not `ping.exe`. Not a plot package. It will not grow a plot API.

One `net10.0` DLL. References: Json 1.0.1, Analytics 1.0.1, FileIo 1.1.1. Logging from repo `$(VestigiumLoggingVersion)`. No plot package.

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="1.2.0" />
```

Route **print** works on both OS, both families. Route **write** is Option C.

Default `0.0.0.0/0` or `::/0` throws `NetworkRouteDenied`. No admin / no `CAP_NET_ADMIN` throws the same.

NetBIOS is Windows-only. `NetworkTestHooks` is internal. This library does not plot.

## PR10 jobs

```csharp
var udp  = await NetworkHelper.UdpProbe("192.0.2.1", 53).RunAsync();
var ask  = await NetworkHelper.ProbeDns("example.com").RunAsync();
var watch = await NetworkHelper.WatchAdapter("Ethernet", new AdapterWatchOptions { Duration = TimeSpan.FromSeconds(5) }).RunAsync();
```

Bound ICMP: when `InterfaceIndex` or `SourceAddress` is set, echo uses the bound path. Omit both and BCL `Ping` may stay.

PMTU: timeout is unknown. Only a sized reject shrinks the walk.

`ProbeNeighbor` is one-address resolve, not a table filter.

Pathping phase 2 samples with the protocol the walk settled on.

Campaign recipes persist bind when set. Old recipes open unset.

`UdpProbe` is one host and one port. `ProbeDns` is answered / refused / timed out. `WatchAdapter` is oper-status, not byte counters.

## PR09 jobs

```csharp
var walk = await NetworkHelper.Pathping("192.0.2.1", new PathpingOptions { Family = RouteFamily.Pv4 }).RunAsync();
var tcp  = await NetworkHelper.TcpConnect("192.0.2.1", 443).RunAsync();
var mtu  = await NetworkHelper.PathMtu("192.0.2.1").RunAsync();
var nic  = await NetworkHelper.SampleCounters("Ethernet", new CounterSampleOptions { Duration = TimeSpan.FromSeconds(5) }).RunAsync();
var arp  = NetworkHelper.ProbeNeighbor("192.0.2.1");
```

`InterfaceIndex` 0 is not rewritten to 1. Trace probes are ICMP, then UDP if ICMP is forbidden, then TCP if UDP is silent.

## OUI

The registry is not packed. Completeness is `LookupOuiAsync`.

## Share campaigns

FileIo probes. No password field.

## ICMP continuous

`Count = 0` needs `MaxDuration` (≤ 24 h). Interval floor 200 ms under one minute, 1 s above.

## Linux CI

Windows `FullyQualifiedName~Network` is the Network gate.

## What is not next in this DLL

Scheduler package. HTTP reachability. Demo gallery. Plot API. Packing the IEEE OUI registry. Port sweep. Default-route write.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR09-11 | 24 Sep 2026 | Consume 1.1.0. PR09 doors. |
| 1.6 + PR10-09 | 24 Sep 2026 | Consume 1.2.0. PR10 doors. |
