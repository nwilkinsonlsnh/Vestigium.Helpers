# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.0  
**Status:** Draft with SRS v1.0.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Network/`.

## What this library is

Workstation inventory and diagnostic jobs for PingIQ, DnsIQ, TraceIQ, HttpIQ, and ProbeHost. Structured objects first. Optional CLI-shaped transcripts are derived from those objects.

It is not `ping.exe`. The Windows / Unix command-line tools are the behavior reference.

## Design

- Façade: `NetworkHelper` (Identity, Probe, inventory, job factories).
- Jobs: `NetworkJob<TResult>` for Ping and Trace. DNS lookup is a cancellable `Task` because it is one round-trip (or a short retry loop), not a multi-minute walk.
- Inventory is a snapshot. No `NetworkChange` event in v1; hosts poll.
- ICMP and RFC 1035 queries stay in-process. Do not shell out.
- Windows-only: NetBIOS name table / cache / sessions. Guard + `PlatformNotSupportedException` elsewhere.
- `netsh` is a read-only snapshot assembler over the same adapter / route / neighbor data. No configuration verbs.

## Logging

Category `Helpers`. APPID `Network`. Library never calls `VestigiumLogger.Initialize`.

Sparse: recipe at start, failures, one summary. Not a JSONL line per successful echo.

## Roadmap

See SRS §13. HTTP reachability is v1.1. Mutations (flush DNS, ARP delete) are v1.3 and require an explicit flag.

Never here: FileIo, Processes-as-a-ping-wrapper, Charts UI, full `netsh` write.
