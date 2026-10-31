# Vestigium.Helpers.Network

Workstation inventory and protocol jobs for diagnostic hosts. Not a CLI. Not `ping.exe`. Not a plot package. This library will not grow a plot API.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Network` 2.0.0 |
| Version rule | `2.0.0` is PR12: `ScanPorts`, rollup, optional discovery, confirm, known UDP. Decision 41 is amended only for `ScanPorts`. No SYN. Do not republish an older version as if it had these doors. |
| TFM | `net10.0` |
| APPID | `Network` (`NetworkCatalog.AppId`) |
| EVENTID | Reserved 14500–14999 (used through 14560) |
| Depends on | `Vestigium.Helpers.Json` 1.0.1, `Vestigium.Helpers.Analytics` 1.0.1, `Vestigium.Helpers.FileIo` 1.1.2, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/002%20--%20Requirements%20Document) |

Does not plot. OUI completeness is a URL fetched on request. The embedded snapshot is a stub and is not the IEEE registry.

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="2.0.0" />
```

```csharp
using Vestigium.Helpers.Network;

var box = NetworkHelper.GetWorkstation();
var echo = await NetworkHelper.IcmpEcho("192.0.2.1").RunAsync();
var ttl  = NetworkHelper.SummarizeTtl(echo.Replies);
var dns  = await NetworkHelper.LookupAsync("example.com");
var path = await NetworkHelper.Pathping("192.0.2.1").RunAsync();
var tcp  = await NetworkHelper.TcpConnect("192.0.2.1", 443).RunAsync();
var udp  = await NetworkHelper.UdpProbe("192.0.2.1", 53).RunAsync();
var ask  = await NetworkHelper.ProbeDns("example.com").RunAsync();
var scan = await NetworkHelper.ScanPorts("192.0.2.1", NetworkHelper.PortsWellKnown("common")).RunAsync();
```

`Ping` / `Trace` are aliases for `IcmpEcho` / `IcmpTrace`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `GetWorkstation` / `GetAdapters` / `GetSnapshot` | inventory | Local stack. `NetworkAdapter.Mtu` is the interface MTU. |
| `IcmpEcho` / `IcmpTrace` / `Pathping` | `NetworkJob<T>` | Bind set uses a bound ICMP path. |
| `SummarizeTtl` | `TtlSummaryResult` | Estimated hops from existing replies. Not a trace. |
| `TcpConnect` / `UdpProbe` | `NetworkJob<T>` | One host, one port. `Mss` and optional `Banner` on connect. |
| `ScanPorts` | `NetworkJob<PortScanResult>` | Hosts you supply. List, range, or well-known set. Cap 1024. |
| `PortsFromList` / `PortsFromRange` / `PortsWellKnown` | `PortSet` | Pure. No I/O. |
| `ProbeDns` | `NetworkJob<DnsProbeResult>` | Answered / refused / timed out. |
| `ProbeSnmpSysDescr` | `NetworkJob<SnmpSysDescrResult>` | One Get of `sysDescr`. Community never logged. |
| `InferDevice` | `InferenceResult` | Pure. Evidence in, class / vendor / confidence out. |
| `WatchAdapter` | `NetworkJob<AdapterWatchResult>` | Oper-status samples. No bill. |
| `SampleCounters` | `NetworkJob<CounterSampleResult>` | One adapter. No bill. |
| `PathMtu` | `NetworkJob<PathMtuResult>` | Remote frame size. Shrinks only on too-big. |
| `LookupAsync` | DNS result | |
| `GetConnections` / `GetRoutes` / `GetNeighbors` | lists | `GetNeighbors(RouteFamily)` reads one family. Parameterless is `All`. |
| `GetRoutesAsync` / `GetNeighborsAsync` / `GetConnectionsAsync` | task of the same list | Token cancels the wait. It does not abort `iphlpapi`. Not a `NetworkJob`. |
| `GetLmHostsAsync` / `GetNetBiosNamesAsync` / `GetNetBiosStatsAsync` | task of the sync result | Same hop. No second log line. |
| `TryService` | bool, name | Blank protocol is TCP. Miss is false. No log. |
| `ProbeNeighbor` | `NeighborProbeResult` | One-address resolve. Not called by inference. |
| `AddRoute` / `ChangeRoute` / `RemoveRoute` | void | Defaults throw `NetworkRouteDenied`. |
| `CreateEchoCampaign` / `CreateShareCampaign` | campaign | Recipe persists bind when set. |
| `ClassifyAddress` / `DescribePrefix` / `PlanByHosts` | prefix math | Scanner does not call these. |
| `ParseMac` / `LookupOuiAsync` / `LookupOuiPacked` | MAC / OUI | Live lookup is the URL. Packed is a stub. |
| `Bandwidth` / `BillP95` / `BillPercentile` | amounts | No plot. |
| `NetworkCatalog.Register(cfg)` | void | Host-only. |

## Rules that do not move

- Not `ping.exe` / `tracert.exe` / `pathping.exe`.
- Route writes need admin / `CAP_NET_ADMIN`. Defaults are denied.
- NetBIOS is Windows-only.
- OUI completeness is `LookupOuiAsync` against the caller URL.
- Never log credentials. SNMP community is never logged.
- The library never calls `VestigiumLogger.Initialize`.
- `InterfaceIndex` 0 is not rewritten to 1.
- `TcpConnect` and `UdpProbe` stay one host, one port. `ScanPorts` is the only list or range.
- No CIDR expansion inside the scanner. No SYN.
- Inference does not obtain a MAC and does not walk hops.
- `GetNeighbors` index 0 is not rewritten to 1. A name miss stays null.
- Async print doors do not abort a P/Invoke already inside `iphlpapi`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";
    cfg.LogDirectory = logDir;
    NetworkCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes.

## Related

Sizes: `Vestigium.Helpers.Analytics`. Share probes: `Vestigium.Helpers.FileIo`. Campaign recipes: `Vestigium.Helpers.Json`.

Long-form documents live in [Vestigium.Documentation / Helpers / Network](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network).
