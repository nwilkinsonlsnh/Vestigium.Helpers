# Vestigium.Helpers.SystemInfo — Requirements

**Document ID:** VEST-HELPERS-SYSTEMINFO-SRS-001  
**Version:** 1.0  
**Status:** Draft  
**Date:** 15 October 2026  
**Package:** `Vestigium.Helpers.SystemInfo`  
**TFM:** `net10.0-windows`  
**APPID:** `SystemInfo`  
**Depends on:** none for records; System.Management for CIM doors  

## 1. Purpose

Own the inventory query logic that currently lives in `Vestigium.Suite.Network.SysInfoIQ` collectors. The host formats and exports. The library reads.

## 2. In scope (v1.0)

| Door | Returns | Source (current host location) |
|---|---|---|
| `CpuFacts.Host` / `Live()` | Topology / census | Native (existing) |
| `MemoryFacts.Read()` | Physical + pools | Native (existing) |
| `SystemInventory.ReadBase()` | Structured base records | `Collector.cs` — Win32_ComputerSystem, Win32_OperatingSystem, Win32_BIOS, Win32_Processor, Win32_PhysicalMemory |
| `SystemInventory.ReadStorage()` | Disk / logical / partition / volume records | `StorageCollector.cs` |
| `SystemInventory.ReadNetwork()` | Adapter / IP / DNS / route / listener records | `NetworkCollector.cs` (reuse Network helper where possible) |
| `SystemInventory.ReadSecurity()` | Firewall / cert / signed-driver records | `SecurityCollector.cs` |
| `SystemInventory.ReadSoftware()` | Installed software / hotfix / feature / env records | `SoftwareCollector.cs` (reuse WinReg where possible) |

Records contain the fields the host currently extracts for its Summary and section files. No text formatting, no file paths, no export logic.

## 3. Out of scope

- Text export, SummaryFormatter, locked Summary block, or InventoryManifest.
- Remote targets.
- PDH / PerfMon counters.
- Elevation prompts. Library reports what it could not read.
- Display strings or culture formatting.
- Reimplementation of Processes, Services, Network, WinReg, or FileIo.

## 4. Contracts

- All numeric fields are raw (bytes, MHz, counts). Host converts.
- `Fact<T>.Unavailable()` on failed native or missing CIM instance. Caller checks `IsOk`.
- Each inventory door continues on partial failure. Missing section is marked unavailable; others still return.
- No PowerShell, wmic, or external process.
- Thread-safe for the cached Live door. Other doors are re-entrant.
- Host owns the decision to write an error file. Library returns the error record.

## 5. Consumers

- SysInfoIQ after each PR: its corresponding collector is deleted and the page calls the library door.
- Any later diagnostic host that needs a local inventory without owning the queries.

## 6. Acceptance

1. Package builds and packs as a single artifact.
2. Existing Cpu/Memory doors return the same shape and Unavailable semantics.
3. After PR01, SysInfoIQ base collect no longer contains ManagementObjectSearcher for the five Win32 classes. Summary text matches the previous output (or the difference is documented).
4. After PR02/PR03, the corresponding host collectors are gone and the pages call library doors.
5. No public type leaks ManagementObject, raw text, or file paths.
6. Unit tests cover Unavailable paths without live CIM required for the pure records.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 15 Oct 2026 | Foundation. One package. Collectors move from SysInfoIQ. |
