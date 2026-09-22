# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.2  
**Status:** Phase 8 harden. Contract is [`Requirements_v1.0.md`](Requirements_v1.0.md) v1.2. Plan is [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md).  
**Date:** 10 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. PingIQ, DnsIQ, TraceIQ, and ProbeHost subscribe on **Windows or Linux**. Not a CLI. Not `ping.exe`.

One `net10.0` DLL. WPF Demo is `net10.0-windows`.

Do not spawn `ping` / `ip` / `ss` / `traceroute` / `netstat` / `arp` / `nbtstat` / `netsh` / `nslookup`. Linux ICMP: DGRAM first; payload reject → empty retry + `PayloadRestricted`. Route mutations need admin / `CAP_NET_ADMIN`. NetBIOS is Windows-only.

## Public surface (running types)

| Method | Notes |
|---|---|
| `Probe` / `Identity` | On-box. Startup-only in the demo. |
| `GetWorkstation` / `GetAdapters` / `GetAdapter` | IPv4 prefix and mask agree. Linux NetBIOS-over-TCP = Unknown. |
| `IcmpEcho` / `Ping` | Count default 4. `0` = continuous. `StatsPath` appends JSONL. |
| `IcmpTrace` / `Trace` | TTL walk. ICMP then UDP fallback. |
| `LookupAsync` / `LookupManyAsync` | Null server = OS. Set server = RFC 1035 UDP/TCP 53. |
| `GetConnections` / `GetStatistics` / `GetRoutes` / `GetNeighbors` | Lists. Empty is legal. Linux PID is best-effort and not a /proc walk. |
| `CreateEchoCampaign` / `OpenEchoCampaign` | In-process clock. 15 min grace. Inject `NetworkTestHooks`. |
| `GetSnapshot` | Adapters + routes + connections + neighbors + stats. |
| `AddRoute` / `ChangeRoute` / `RemoveRoute` / `DeleteRoute` | Windows IP Helper. Linux `PlatformNotSupportedException`. Access denied → `NetworkRouteDenied`. |
| `GetNetBios` | Windows only. |

Campaign default roots: `%ProgramData%\Vestigium\Network\Campaigns\` and `/var/lib/vestigium/network/campaigns/`. Tests inject `NetworkTestHooks.CampaignRoot`. Never silent `$HOME`.

HelperLog: APPID `Network`, category `Helpers`. Recipe + summary. No packet bytes. Campaign does not log each echo to HelperLog; those rows go to stats JSONL.

## Linux CI waiver

Dated **10 September 2026**. `Vestigium.Helpers.Tests` is `net10.0-windows` (Charts). The GitHub workflow is `windows-latest` only. Portable Network tests are written so they can run on Linux when the test TFM splits. Until then the Linux filter is waived; Windows `FullyQualifiedName~Network` is the gate.

## Phase order

0 Paper → 1 Inventory → 2 ICMP Echo → 3 Trace + DNS → 4 Tables → 5 Campaigns + JSONL → 6 Snapshot / route / NetBIOS → 7 Demo → 8 Harden.
