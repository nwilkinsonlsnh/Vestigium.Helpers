# PR01 — Implementation Plan

**Companion:** PR01 -- Requirements.md  
**Package:** Vestigium.Helpers.SystemInfo  
**Goal:** Collapse the three packages into one. Move native readers in. Add the base CIM door SysInfoIQ already needs.

## Scope

- Delete (or empty) Vestigium.Helpers.SystemInfo.Cpu and .Memory projects. Move their types into SystemInfo.
- Keep public doors identical so existing callers compile.
- Add `SystemInventory.ReadBase()` that returns structured records for the five Win32 classes.
- Update SysInfoIQ Collector to call the new door for base sections instead of raw WMI text dumps. Host still owns SummaryFormatter and file layout.
- Pack and publish 0.2.0 (or next).

## Not in PR01

- Storage, Network, Process, Service sections.
- Text generation inside the library.
- EVENTID catalog registration.
- Changes to PerfMon or other helpers.

## Steps

1. Move CpuFacts.cs / MemoryFacts.cs and their private structs into SystemInfo (Native folder or internal).
2. Update namespaces and project references. Remove the two satellite projects from the solution.
3. Add BaseInventory records + SystemInventory static door using ManagementObjectSearcher.
4. Point SysInfoIQ at the door. Keep its text formatting.
5. Tests for Unavailable and for the record shapes.
6. README update. Pack.

## Risks

- Existing NuGet consumers of the satellite packages. Acceptable: they are 0.1.x, unpublished or low use. Document the break.
- CIM latency. Acceptable for inventory; not for a hot path.
- EVENTID collision. Leave unregistered until confirmed.

## Done when

Package is one. Doors match. SysInfoIQ base collect no longer owns the five WMI queries. Tests green.
