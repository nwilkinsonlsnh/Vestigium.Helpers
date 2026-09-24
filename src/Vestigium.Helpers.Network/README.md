# Vestigium.Helpers.Network

Workstation inventory and protocol jobs for diagnostic hosts. Not a CLI. Not `ping.exe`. Not a plot package. This library will not grow a plot API.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Network` 1.2.0 |
| Version rule | `1.1.0` is PR09. `1.2.0` is PR10. Do not republish `1.1.0` as if it had UdpProbe. |
| TFM | `net10.0` |
| APPID | `Network` (`NetworkCatalog.AppId`) |
| EVENTID | Reserved 14500–14999 (used through 14560) |
| Depends on | `Vestigium.Helpers.Json` 1.0.1, `Vestigium.Helpers.Analytics` 1.0.1, `Vestigium.Helpers.FileIo` 1.1.1, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Network/002%20--%20Requirements%20Document) |

Does not plot. OUI completeness is a URL fetched on request. The embedded snapshot is a stub and is not the IEEE registry.

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="1.2.0" />
```

```csharp
using Vestigium.Helpers.Network;

var box = NetworkHelper.GetWorkstation();
var echo = await NetworkHelper.IcmpEcho("192.0.2.1").RunAsync();
var dns  = await NetworkHelper.LookupAsync("example.com");
var path = await NetworkHelper.Pathping("192.0.2.1").RunAsync();
var tcp  = await NetworkHelper.TcpConnect("192.0.2.1", 443).RunAsync();
var udp  = await NetworkHelper.UdpProbe("192.0.2.1", 53).RunAsync();
var ask  = await NetworkHelper.ProbeDns("example.com").RunAsync();
```

`Ping` / `Trace` are aliases for `IcmpEcho` / `IcmpTrace`.

## Surface

| Call | Returns | Notes |
|---|---|
| `GetWorkstation` / `GetAdapters` / `GetSnapshot` | inventory | Local stack. |
| `IcmpEcho` / `IcmpTrace` / `Pathping` | `NetworkJob<T>` | Bind set uses a bound ICMP path. |
| `TcpConnect` / `UdpProbe` | `NetworkJob<T>` | One host, one port. |
| `ProbeDns` | `NetworkJob<DnsProbeResult>` | Answered / refused / timed out. |
| `WatchAdapter` | `NetworkJob<AdapterWatchResult>` | Oper-status samples. No bill. |
| `SampleCounters` | `NetworkJob<CounterSampleResult>` | One adapter. No bill. |
| `PathMtu` | `NetworkJob<PathMtuResult>` | Shrinks only on too-big. |
| `LookupAsync` | DNS result | |
| `GetConnections` / `GetRoutes` / `GetNeighbors` | lists | |
| `ProbeNeighbor` | `NeighborProbeResult` | One-address resolve. |
| `AddRoute` / `ChangeRoute` / `RemoveRoute` | void | Defaults throw `NetworkRouteDenied`. |
| `CreateEchoCampaign` / `CreateShareCampaign` | campaign | Recipe persists bind when set. |
| `ClassifyAddress` / `DescribePrefix` / `PlanByHosts` | prefix math | |
| `ParseMac` / `LookupOuiAsync` / `LookupOuiPacked` | MAC / OUI | Live lookup is the URL. Packed is a stub. |
| `Bandwidth` / `BillP95` / `BillPercentile` | amounts | No plot. |
| `NetworkCatalog.Register(cfg)` | void | Host-only. |

## Rules that do not move

- Not `ping.exe` / `tracert.exe` / `pathping.exe`.
- Route writes need admin / `CAP_NET_ADMIN`. Defaults are denied.
- NetBIOS is Windows-only.
- OUI completeness is `LookupOuiAsync` against the caller URL.
- Never log credentials.
- The library never calls `VestigiumLogger.Initialize`.
- `InterfaceIndex` 0 is not rewritten to 1.
- No port sweep.

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
