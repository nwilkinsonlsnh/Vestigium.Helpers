# PR01 — Implementation Plan

**Companion:** PR01 -- Requirements.md  
**Package:** Vestigium.Helpers.SystemInfo  
**Goal:** Collapse the three packages into one. Move the base Collector (five Win32 classes) out of SysInfoIQ into a structured door. Host keeps SummaryFormatter and file layout.

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

## Sub-steps

| ID | Step | Action | Done when |
|---|---|---|---|
| PR01.001 | Move native readers | Move `CpuFacts.cs` and `MemoryFacts.cs` (and private structs) from the satellite projects into `SystemInfo` under `Native/` or internal. Update namespaces. | Files exist in SystemInfo. Satellites no longer contain the source. |
| PR01.002 | Collapse projects | Update project references. Remove `Vestigium.Helpers.SystemInfo.Cpu` and `.Memory` from the solution and Directory.Build.props. Public doors remain identical. | Solution builds with one SystemInfo project. Existing callers compile. |
| PR01.003 | Add base inventory door | Add `BaseInventory` records and `SystemInventory.ReadBase()`. Map the five Win32 classes (ComputerSystem, OperatingSystem, BIOS, Processor, PhysicalMemory) to the fields SysInfoIQ currently extracts for its Summary. | Door returns structured records or Unavailable. No text. |
| PR01.004 | Point SysInfoIQ at the door | Update `Collector.cs` in the host to call `SystemInventory.ReadBase()`. Delete the ManagementObjectSearcher path for the five base classes. Keep SummaryFormatter and export. | Host base collect no longer owns the five WMI queries. Summary text matches (or difference is listed). |
| PR01.005 | Tests and pack | Add tests for Unavailable paths and record shapes. Update README. Pack 0.2.0. | Tests green. Package published. |

## Risks

- Existing NuGet consumers of the satellite packages. Acceptable: they are 0.1.x, unpublished or low use. Document the break.
- CIM latency. Acceptable for inventory; not for a hot path.
- Summary text drift. Acceptable if documented; not acceptable if silent.
- EVENTID collision. Leave unregistered until confirmed.

## Done when

Package is one. Doors match. SysInfoIQ base collect no longer owns the five WMI queries. Summary text matches (or difference is listed). Tests green.
