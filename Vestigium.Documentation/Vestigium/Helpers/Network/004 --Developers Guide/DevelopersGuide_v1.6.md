# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.6  
**Status:** Current call surface. Contract is [`Requirements_v1.6.md`](Requirements_v1.6.md). Shape is [`Design_v1.6.md`](Design_v1.6.md). Consume package **2.0.0**.  
**Date:** 31 October 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. Hosts subscribe on Windows or Linux. Not a CLI. Not `ping.exe`. Not a plot package. It will not grow a plot API.

## PR13 jobs

```csharp
var hosts = NetworkHelper.HostsFromPrefix("192.0.2.0/30");
var scan = await NetworkHelper.ScanPorts(hosts, NetworkHelper.PortsWellKnown("windows"), new PortScanOptions
{
    OpenOnly = true,
    ReadBanner = true
}).RunAsync();
var csv = PortScanCsv.Write(scan);
```

`Sent` on progress is the expected probe count after discovery. `Received` is finished.

`OpenOnly` drops closed, timed-out, and error hits. Default off.

`ReadBanner` passes through to `TcpConnect`. `Confirm` stays a separate flag. Both off by default.

`WellKnown` names are `common`, `catalog`, `windows`, `printers`, and `databases`.

`HostsFromPrefix` returns usable IPv4 hosts. Cap 1024. It does not scan. The scanner still does not expand a CIDR.

`PortScanCsv.Write` is text. One row per open port. No file write.

No SYN in this package. Half-open is `Vestigium.Helpers.Network.RawSocket`.

## PR12 jobs

`ScanPorts` is the only list or range job. `TcpConnect` and `UdpProbe` stay one host, one port.

Hosts are supplied. Discovery, confirm, and known UDP stay optional and off by default.

## What is not next in this DLL

Scheduler package. HTTP reachability beyond the short confirm probe. Demo gallery. Plot API. Packing the IEEE OUI registry. CIDR expansion inside the scanner. SYN or half-open scan. Default-route write. Actual hop walk from the inference path. Neighbor or MAC resolution from the inference path. SNMP walk or set. OS fingerprinting.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.6 + PR12-07 | 31 Oct 2026 | Consume 2.0.0. PR12 doors. `ScanPorts`. No SYN. |
| 1.6 + PR13-07 | 31 Oct 2026 | PR13 doors. Progress total, `OpenOnly`, `ReadBanner`, lab sets, `HostsFromPrefix`, CSV. |
