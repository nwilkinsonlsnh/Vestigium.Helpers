# Vestigium.Helpers.SystemInfo — Implementation Plan

**Status:** Active  
**Owner:** Helpers  
**Companion:** Requirements.md  
**Goal:** Move the query logic that currently lives in SysInfoIQ collectors into this library. The host becomes a thin orchestrator that formats and exports.

## What we are building

A single `Vestigium.Helpers.SystemInfo` package (`net10.0-windows`) that owns the machine inventory reads.

- Shared `Fact<T>` and pure records stay.
- Native readers (`CpuFacts`, `MemoryFacts`) move into this assembly. Doors stay the same.
- The five collectors currently in `Vestigium.Suite.Network.SysInfoIQ/ViewModels` move here as structured doors:
  - `Collector` (base: ComputerSystem, OS, BIOS, Processor, PhysicalMemory)
  - `StorageCollector` (DiskDrive, LogicalDisk, Partition, Volume)
  - `NetworkCollector` (adapters, IP, DNS, routes, ARP, listeners, TCP)
  - `SecurityCollector` (firewall profiles/rules, certs, signed drivers)
  - `SoftwareCollector` (installed software, hotfixes, features, ODBC, environment, SQL services)
- Each door returns structured records (or `Fact<T>` / error records). No text dumps, no file I/O, no SummaryFormatter.
- SysInfoIQ calls the doors, builds its Summary text, and writes the export folder. It no longer owns WMI, Registry, or NetworkInterface queries for inventory.

## What we are not building

- Separate Cpu/Memory packages after PR01.
- Text export, SummaryFormatter, or the locked Summary block inside the library.
- Remote CIM, scheduled runs, or PowerShell.
- PDH counters (that is PerfMon).
- Reimplementation of surfaces already owned by other helpers (Processes, Services, Network, WinReg, FileIo). Call those.

## Package shape (decision)

One assembly. No satellites.

| Area | Location | Notes |
|---|---|---|
| Records + Fact | root | No P/Invoke |
| Native readers | `Native/` or internal | Topology, live CPU, memory status |
| Inventory doors | `Inventory/` | One static class per former collector. Structured results. |
| Shared CIM helper | internal | ManagementObjectSearcher wrapper that returns rows or Unavailable. |

Rejected: keep three packages. Rejected: leave the collectors in the host. Rejected: one package per collector.

## PR sequence

| PR | Scope | Done when |
|---|---|---|
| PR01 | Collapse to one package. Move native. Move the base `Collector` (five Win32 classes) as structured records. SysInfoIQ base path calls the door. | Host no longer runs ManagementObjectSearcher for ComputerSystem/OS/BIOS/Processor/Memory. Summary text matches. |
| PR02 | Move `StorageCollector` and `NetworkCollector`. Reuse Network helper where it already covers adapters/TCP. | Host Storage and Network pages call library doors. |
| PR03 | Move `SecurityCollector` and `SoftwareCollector`. Reuse WinReg where it already covers Uninstall keys. | Host Security and Software pages call library doors. |
| Later | Remaining catalog sections (events, drivers, tasks, shares) as needed. Elevation notes stay in host. | — |

## Constraints

- Failed read is `Fact.Unavailable()` or an error record. Never a zero that looks like a measurement.
- No display strings in records. Host formats.
- EVENTID block still unclaimed (19000–19499 is PerfMon.Memory). Confirm before catalog registration.
- Logging via Vestigium.Logging only when the host asks. Library does not open a log.
- One section failure does not fail the whole inventory. Host decides what to write.

## Next action

PR01 papers locked. Then code the package collapse and the base collector move. No code until the papers are the ones we keep.
