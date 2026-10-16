# PR01 — Requirements

**Companion:** PR01 -- Implementation Plan.md  
**Status:** Draft  
**Date:** 15 October 2026  

## What PR01 ships

One package. Existing Cpu and Memory doors preserved. Base collector moves out of SysInfoIQ.

| Item | Rule |
|---|---|
| Package | Single `Vestigium.Helpers.SystemInfo`. Satellites removed. |
| CpuFacts.Host / Live | Same signatures and Unavailable semantics. |
| MemoryFacts.Read | Same MemoryRead shape. Per-field Fact. |
| SystemInventory.ReadBase | Returns BaseInventory with the fields SysInfoIQ currently extracts from the five Win32 classes for its Summary. Partial success allowed. |
| SysInfoIQ | `Collector.cs` no longer contains ManagementObjectSearcher for ComputerSystem, OperatingSystem, BIOS, Processor, or PhysicalMemory. It calls the door and feeds SummaryFormatter. Still writes the export folder. |

## Acceptance

1. Solution builds with one SystemInfo project.
2. Existing unit tests (or new ones) pass for topology, live, and memory Unavailable paths.
3. ReadBase returns expected fields or Unavailable on a test machine / mocked path.
4. SysInfoIQ CollectBase produces the same Summary text as before (or the difference is listed in the PR).
5. No new public types that leak ManagementObject or raw text.
6. The five WMI queries are gone from the host ViewModels folder.

## Out

Storage, Network, Security, Software collectors. Text export. Remote.
