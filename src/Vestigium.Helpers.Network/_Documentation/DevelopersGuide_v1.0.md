# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.1  
**Status:** Draft with SRS v1.1.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract (document version **1.1**). Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS **resource library**. PingIQ, DnsIQ, TraceIQ, HttpIQ, and ProbeHost subscribe to it. It is not a CLI and not `ping.exe`.

Reachability is **ICMP Echo Request/Reply**. Trace is **ICMP Time Exceeded**. Stats and campaign recipes go through `Vestigium.Helpers.Json` (`.json` recipe, `.jsonl` append-only warehouse). HelperLog stays the sparse audit trail.

## Design

- Façade: `NetworkHelper`.
- `Ping` / `Trace` are aliases for `IcmpEcho` / `IcmpTrace`.
- Campaign runner is in-process. ProbeHost (or another hosted service) must stay alive across windows. We do not call `schtasks`.
- Grace 15 minutes; missed windows write `windowMissed` and do not backfill hours later.
- Hot-path JSONL append: `JsonHelper.ToJson(..., WriteIndented = false)` then append a line. Do not `OpenJsonl` + `Save` the whole file per echo.
- `GetRoutes` is print. `AddRoute` / `ChangeRoute` / `DeleteRoute` are explicit and log Warning.

## Siblings

| Project | Role |
|---|---|
| `Vestigium.Helpers.Json` | Recipe `.json`, stats `.jsonl` serialize/open |
| `Vestigium.Helpers.Analytics` | Optional RTT NumericSeries at finalize |
| `Vestigium.Logging` | Audit JSONL under `Logs\\Network\\` only |

## Roadmap

Json `AppendJsonl` (preferred write path) is the first follow-up on the Json side. HTTP reachability stays off this façade until v1.3.
