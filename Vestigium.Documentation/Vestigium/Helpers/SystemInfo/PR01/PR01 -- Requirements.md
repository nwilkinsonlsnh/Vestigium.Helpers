# Vestigium.Helpers.SystemInfo — Implementation Plan

**Status:** Active  
**Owner:** Helpers  
**Companion:** Requirements.md  
**Goal:** Move all inventory query logic out of SysInfoIQ. After this sequence the host is a skeleton: it calls library doors, formats the locked Summary, and writes the export folder. It owns no WMI, no Registry inventory queries, and no NetworkInterface inventory queries.

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
- SysInfoIQ calls the doors, builds its Summary text, and writes the export folder.

## End state (locked)

After PR01 + PR02 + PR03:

| Host file                                                    | Status                                                       |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `Collector.cs`                                               | Deleted or reduced to a door call + SummaryFormatter. No ManagementObjectSearcher. |
| `StorageCollector.cs`                                        | Deleted.                                                     |
| `NetworkCollector.cs`                                        | Deleted.                                                     |
| `SecurityCollector.cs`                                       | Deleted.                                                     |
| `SoftwareCollector.cs`                                       | Deleted.                                                     |
| `SummaryFormatter.cs`                                        | Stays. Host owns text.                                       |
| `Exporter.cs`                                                | Stays. Host owns files.                                      |
| ViewModels that call WMI / Registry / NetworkInterface for inventory | Gone.                                                        |

The application is a skeleton. All query logic lives in the library (or in helpers the library calls).

## What we are not building

- Separate Cpu/Memory packages after PR01.
- Text export, SummaryFormatter, or the locked Summary block inside the library.
- Remote CIM, scheduled runs, or PowerShell.
- PDH counters (that is PerfMon).
- Reimplementation of surfaces already owned by other helpers (Processes, Services, Network, WinReg, FileIo). Call those.

## Package shape (decision)

One assembly. No satellites.

| Area              | Location              | Notes                                                        |
| ----------------- | --------------------- | ------------------------------------------------------------ |
| Records + Fact    | root                  | No P/Invoke                                                  |
| Native readers    | `Native/` or internal | Topology, live CPU, memory status                            |
| Inventory doors   | `Inventory/`          | One static class per former collector. Structured results.   |
| Shared CIM helper | internal              | ManagementObjectSearcher wrapper that returns rows or Unavailable. |

Rejected: keep three packages. Rejected: leave any collector in the host. Rejected: one package per collector.

## PR sequence

| PR    | Scope                                                        | Done when                                                    |
| ----- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| PR01  | Collapse to one package. Move native. Move the base `Collector` (five Win32 classes) as structured records. SysInfoIQ base path calls the door. | Host no longer runs ManagementObjectSearcher for ComputerSystem/OS/BIOS/Processor/Memory. Summary text matches. |
| PR02  | Move `StorageCollector` and `NetworkCollector`. Reuse Network helper where it already covers adapters/TCP. | Host Storage and Network pages call library doors. Those two collector files are deleted. |
| PR03  | Move `SecurityCollector` and `SoftwareCollector`. Reuse WinReg where it already covers Uninstall keys. | Host Security and Software pages call library doors. Those two collector files are deleted. |
| Later | Remaining catalog sections (events, drivers, tasks, shares) as needed. Elevation notes stay in host. | —                                                            |

### PR01 sub-steps

| ID       | Step                        | Action                                                       | Done when                                                    |
| -------- | --------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| PR01.001 | Move native readers         | Move `CpuFacts.cs` and `MemoryFacts.cs` (and private structs) from the satellite projects into `SystemInfo` under `Native/` or internal. Update namespaces. | Files exist in SystemInfo. Satellites no longer contain the source. |
| PR01.002 | Collapse projects           | Update project references. Remove `Vestigium.Helpers.SystemInfo.Cpu` and `.Memory` from the solution and Directory.Build.props. Public doors remain identical. | Solution builds with one SystemInfo project. Existing callers compile. |
| PR01.003 | Add base inventory door     | Add `BaseInventory` records and `SystemInventory.ReadBase()`. Map the five Win32 classes (ComputerSystem, OperatingSystem, BIOS, Processor, PhysicalMemory) to the fields SysInfoIQ currently extracts for its Summary. | Door returns structured records or Unavailable. No text.     |
| PR01.004 | Point SysInfoIQ at the door | Update `Collector.cs` in the host to call `SystemInventory.ReadBase()`. Delete the ManagementObjectSearcher path for the five base classes. Keep SummaryFormatter and export. | Host base collect no longer owns the five WMI queries. Summary text matches (or difference is listed). |
| PR01.005 | Tests and pack              | Add tests for Unavailable paths and record shapes. Update README. Pack 0.2.0. | Tests green. Package published.                              |

## Constraints

- Failed read is `Fact.Unavailable()` or an error record. Never a zero that looks like a measurement.
- No display strings in records. Host formats.
- EVENTID block still unclaimed (19000–19499 is PerfMon.Memory). Confirm before catalog registration.
- Logging via Vestigium.Logging only when the host asks. Library does not open a log.
- One section failure does not fail the whole inventory. Host decides what to write.

## Next action

PR01.001. Then PR01.002. No code until the papers are the ones we keep.
