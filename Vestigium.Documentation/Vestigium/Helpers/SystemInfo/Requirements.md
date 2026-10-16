# Vestigium.Helpers.SystemInfo — Requirements

**Document ID:** VEST-HELPERS-SYSTEMINFO-SRS-001  
**Version:** 1.0  
**Status:** Draft  
**Date:** 15 October 2026  
**Package:** `Vestigium.Helpers.SystemInfo`  
**TFM:** `net10.0-windows`  
**APPID:** `SystemInfo`  
**Depends on:** none for records; System.Management for CIM door  

## 1. Purpose

Own the machine-fact reads that SysInfoIQ and other hosts currently scatter. One package. Structured results. Unavailable is not a zero.

## 2. In scope (v1.0)

| Door | Returns | Source |
|---|---|---|
| `CpuFacts.Host` | `Fact<CpuTopology>` | GetLogicalProcessorInformationEx |
| `CpuFacts.Live()` | `Fact<CpuLive>` (1s cache) | GetPerformanceInfo + CallNtPowerInformation |
| `MemoryFacts.Read()` | `MemoryRead` (per-field Fact) | GlobalMemoryStatusEx + GetPerformanceInfo |
| `SystemInventory.ReadBase()` | `BaseInventory` | Win32_ComputerSystem, Win32_OperatingSystem, Win32_BIOS, Win32_Processor, Win32_PhysicalMemory |

`BaseInventory` contains the fields the locked Summary block needs (hostname, domain, role, manufacturer, model, serial, OS caption/version/build/install/boot, processor name, sockets, logical, total physical memory). No text formatting.

## 3. Out of scope

- Full SectionsCatalog (storage, network tables, firewall, software, events, drivers).
- Text export or SummaryFormatter.
- Remote targets.
- PDH / PerfMon counters.
- Elevation prompts. Library reports what it could not read.
- Display strings or culture formatting.

## 4. Contracts

- All numeric fields are raw (bytes, MHz, counts). Host converts.
- `Fact<T>.Unavailable()` on failed native or missing CIM instance. Caller checks `IsOk`.
- CIM door continues on partial failure; missing section is marked unavailable, others still return.
- No PowerShell, wmic, or external process.
- Thread-safe for the cached Live door. Other doors are re-entrant.

## 5. Consumers

- SysInfoIQ PR that replaces its Collector.Query for the five base classes.
- Any later diagnostic host that needs a local snapshot without owning WMI.

## 6. Acceptance

1. Package builds and packs as a single artifact.
2. Existing Cpu/Memory doors return the same shape and Unavailable semantics.
3. `SystemInventory.ReadBase()` returns the Summary fields on a typical Windows machine (or Unavailable on failure).
4. No P/Invoke outside the Native area.
5. Unit tests cover Unavailable paths without live CIM required for the pure records.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 15 Oct 2026 | Foundation. One package. Native + base CIM. |
