# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.2  
**Status:** Draft with SRS v1.2. Phase map is [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md).  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract (document version **1.2**). Build mode follows the implementation plan, one phase at a time.

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

A .NET 10 LTS resource library. PingIQ, DnsIQ, TraceIQ, HttpIQ, and ProbeHost subscribe on **Windows or Linux**. Not a CLI. Not `ping.exe`.

## Platforms

One `net10.0` DLL. WPF Demo stays `net10.0-windows`. ProbeHost-on-Linux consumes the library directly.

Do not spawn `ping` / `ip` / `ss` / `traceroute`. Linux ICMP: DGRAM first; payload reject → empty retry + `PayloadRestricted`. Route mutations need admin / `CAP_NET_ADMIN`. NetBIOS is Windows-only.

Campaign default roots: `%ProgramData%\Vestigium\Network\Campaigns\` and `/var/lib/vestigium/network/campaigns/`. Tests inject a temp root.

## Phase order

0 Paper → 1 Inventory → 2 ICMP Echo → 3 Trace + DNS → 4 Tables → 5 Campaigns + JSONL → 6 Snapshot / route / NetBIOS → 7 Demo → 8 Harden.
