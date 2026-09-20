# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.6  
**Status:** Current call surface. Contract is [`Requirements_v1.6.md`](Requirements_v1.6.md). Shape is [`Design_v1.6.md`](Design_v1.6.md).  
**Date:** 19 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. PingIQ, DnsIQ, TraceIQ, and ProbeHost subscribe on **Windows or Linux**. Not a CLI. Not `ping.exe`.

One `net10.0` DLL. Do not spawn `ping` / `ip` / `ss` / `traceroute` / `netstat` / `arp` / `nbtstat` / `netsh` / `nslookup` / `route`.

Route **print** works on both OS. Route **mutate** is Windows-only in v1 (administrator). Linux mutate throws `NetworkRouteDenied`. IPv6 destinations are rejected on mutate. NetBIOS is Windows-only.

`NetworkTestHooks` is internal. Hosts cannot set it. Tests already have `InternalsVisibleTo`.

## Linux ICMP (PR02.005)

`IcmpEcho` / `Ping` use `System.Net.NetworkInformation.Ping`. On current .NET 10 and the distros we care about that is unprivileged **ICMP DGRAM** when the kernel allows it. The library does not open a raw socket itself and does not spawn `ping(8)`.

If the kernel rejects a custom Echo payload, the engine retries once with an empty buffer and sets `PayloadRestricted` on the result. If ICMP is not permitted at all (`Operation not permitted`, access denied, or `PlatformNotSupportedException`), the reply is `ProtocolForbidden` and the job status is `Failed` when nothing else succeeded.

A dedicated DGRAM socket stack is **not** in PR02. Open that in PR04 only if a supported distro proves BCL Ping is raw-only.

## Public surface

| Method | Notes |
|---|---|
| `Probe` / `Identity` | On-box. Returns `Vestigium.Helpers.Network`. |
| `GetWorkstation` / `GetAdapters` / `GetAdapter` | IPv4 prefix and mask agree. Linux NetBIOS-over-TCP = Unknown. |
| `IcmpEcho` / `Ping` | Count default 4. `0` = continuous under the duration/interval lock below. `StatsPath` appends JSONL (not campaign-confined). Linux: BCL Ping / DGRAM. |
| `IcmpTrace` / `Trace` | TTL walk. ICMP then UDP fallback. |
| `LookupAsync` / `LookupManyAsync` | Null server = OS. Set server = RFC 1035 UDP/TCP 53. Wire path accepts only that IP+port. |
| `GetConnections` / `GetStatistics` / `GetRoutes` / `GetNeighbors` | Lists. Empty is legal. Linux PID is best-effort. `GetRoutes` prints IPv4 and IPv6. |
| `CreateEchoCampaign` / `OpenEchoCampaign` | In-process clock. 15 min grace. Recipe/results must sit under the campaign root. |
| `GetSnapshot` | Adapters + routes + connections + neighbors + stats. |
| `AddRoute` / `ChangeRoute` / `RemoveRoute` / `DeleteRoute` | **Windows IPv4 write** (IP Helper + optional HKLM persistent). Linux → `NetworkRouteDenied`, never `ip route`. IPv6 dest → `ArgumentException`. Pass `InterfaceIndex` or an up IPv4 NIC must exist. Access denied → `NetworkRouteDenied`. |
| `GetNetBios` | Windows only. |
| `ClassifyAddress` / `DescribePrefix` / `PlanByHosts` / `PlanByNetworks` / `SplitPrefix` / `SplitPrefixByCount` / `PackVlsm` / `Contains` / `Overlaps` / `Summarize` / `NextBlock` | Prefix math. See subnet addendum. |
| `ParseMac` / `MacFromInteger` / `FormatMac` / `ToModifiedEui64` / `ToEui48` / `ToLinkLocal` | Offline. No HTTP. |
| `LookupOuiAsync` / `LoadOuiRegistry` / `LookupOuiFile` | Live OUI is HTTPS + allowlist. Inject `Handler` in tests. |
| `Bandwidth` / `ConvertBandwidth` / `TransferTime` / `RequiredRate` / `Transferred` / `VolumeFromRate` / `RateFromVolume` / `EstimateWebsite` / `BandwidthSeconds` | Pure math. |
| `BillP95` / `BillPercentile` | Samples or `NumericSeries`. Empty-sample rule is PR02.007. |

## ICMP continuous

```csharp
// four fast pings — allowed
NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = 4, Interval = TimeSpan.Zero });

// one hour monitor — interval must be >= 1s
NetworkHelper.IcmpEcho(target, new IcmpEchoOptions
{
    Count = 0,
    MaxDuration = TimeSpan.FromHours(1),
    Interval = TimeSpan.FromSeconds(1)
});

// lab burst, one minute or less
NetworkHelper.IcmpEcho(target, new IcmpEchoOptions
{
    Count = 0,
    MaxDuration = TimeSpan.FromSeconds(30),
    Interval = TimeSpan.Zero,
    AllowBurst = true
});
```

`Count = 0` with no `MaxDuration` becomes 60 seconds on the options object when the job is created.

## Routes

```csharp
var printed = NetworkHelper.GetRoutes(RouteFamily.All); // Windows + Linux, v4 + v6

NetworkHelper.AddRoute(new NetworkRouteChange
{
    Destination = "192.0.2.0",
    PrefixLength = 24,
    Gateway = "192.0.2.1",
    InterfaceIndex = 12 // or omit if an up IPv4 NIC exists
});
```

On Linux that `AddRoute` throws `NetworkRouteDenied`.

## OUI

Default URL host is `api.macvendors.com`. A custom `RegistryUrl` needs `AllowCustomRegistry = true` and that host in `AllowedRegistryHosts`. `http://`, loopback, RFC1918, and 3xx without a followed hop all fail closed (throw or `Source = None` for 3xx/empty/HTML).

## Campaign paths

Default roots: `%ProgramData%\Vestigium\Network\Campaigns\` and `/var/lib/vestigium/network/campaigns/`. Never silent `$HOME`.

`ResultsPath` / `RecipePath` after `GetFullPath` must stay under that root (or the test hook root). `..\..\Windows\evil.jsonl` throws.

## HelperLog

APPID `Network`, category `Helpers`. Recipe + summary. No packet bytes. Campaign does not log each echo to HelperLog; those rows go to stats JSONL. Directory prefixes are stripped from log tokens.

## Linux CI waiver

Dated **10 September 2026**. `Vestigium.Helpers.Tests` is `net10.0-windows` (Charts). The GitHub workflow is `windows-latest` only. Portable Network tests are written so they can run on Linux when the test TFM splits (PR04). Until then the Linux filter is waived; Windows `FullyQualifiedName~Network` is the gate.

## What is not next in this DLL

Share-transfer campaigns (PR03). Linux netlink write / IPv6 route write / a dedicated ICMP DGRAM socket if BCL is proven raw-only (PR04). A scheduler service. HTTP client.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.2 | 10 Sep 2026 | Phase 8 harden wording. Inventory through campaigns. |
| 1.6 | 19 Sep 2026 | Shipped façade including subnet / MAC / bandwidth. PR01.001–006 call rules. Hooks no longer public. |
| 1.6 + PR02.001 | 19 Sep 2026 | Route print both OS; mutate Windows IPv4 only. |
| 1.6 + PR02.005 | 19 Sep 2026 | Linux ICMP is BCL Ping / DGRAM. No ping(8). No new socket in PR02. |
