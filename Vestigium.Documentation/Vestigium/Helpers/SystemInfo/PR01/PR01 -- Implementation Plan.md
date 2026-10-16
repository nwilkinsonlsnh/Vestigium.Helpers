# PR01 — Implementation Plan

**Companion:** PR01 -- Requirements.md  
**Package:** Vestigium.Helpers.SystemInfo  
**Goal:** Collapse the three packages into one. Move the base `Collector` (five Win32 classes) out of SysInfoIQ into a structured door. Host keeps SummaryFormatter and file layout.

## Scope

- Delete (or empty) Vestigium.Helpers.SystemInfo.Cpu and .Memory projects. Move their types into SystemInfo.
- Keep public Cpu/Memory doors identical so existing callers compile.
- Add `SystemInventory.ReadBase()` that returns structured records for ComputerSystem, OperatingSystem, BIOS, Processor, and PhysicalMemory.
- Update SysInfoIQ `Collector.CollectBase` and `BuildSummary` to call the door instead of running ManagementObjectSearcher and parsing text. Host still owns SummaryFormatter and the export folder.
- Pack and publish 0.2.0 (or next).

## Not in PR01

- StorageCollector, NetworkCollector, SecurityCollector, SoftwareCollector.
- Text generation inside the library.
- EVENTID catalog registration.
- Changes to PerfMon or other helpers.

## Steps

1. Move CpuFacts.cs / MemoryFacts.cs and their private structs into SystemInfo (Native folder or internal).
2. Update namespaces and project references. Remove the two satellite projects from the solution.
3. Add BaseInventory records + `SystemInventory.ReadBase()` using ManagementObjectSearcher. Map the fields SysInfoIQ currently extracts for the Summary block.
4. Point SysInfoIQ Collector at the door. Delete the WMI Query path for the five base classes. Keep SummaryFormatter.
5. Tests for Unavailable and for the record shapes.
6. README update. Pack.

## Risks

- Existing NuGet consumers of the satellite packages. Acceptable: they are 0.1.x, unpublished or low use. Document the break.
- CIM latency. Acceptable for inventory; not for a hot path.
- Summary text drift. Acceptable if documented; not acceptable if silent.
- EVENTID collision. Leave unregistered until confirmed.

## Done when

Package is one. Doors match. SysInfoIQ base collect no longer owns the five WMI queries. Summary text matches (or difference is listed). Tests green.
