# Vestigium.Helpers.SystemInfo — Implementation Plan

**Status:** Active  
**Owner:** Helpers  
**Companion:** Requirements.md  
**Goal:** One package that owns machine facts (native + CIM) so hosts stop owning the queries.

## What we are building

A single `Vestigium.Helpers.SystemInfo` package (`net10.0-windows`).

- Shared `Fact<T>` and pure records stay.
- Native readers (`CpuFacts`, `MemoryFacts`) move into this assembly. Doors stay the same.
- Base CIM inventory (ComputerSystem, OperatingSystem, BIOS, Processor, PhysicalMemory summary fields) becomes structured records + a collector door.
- SysInfoIQ (and any later host) consumes the doors; it no longer owns the WMI calls for those sections.

## What we are not building

- Separate Cpu/Memory packages after this PR.
- Full SectionsCatalog coverage in this package.
- Remote CIM, scheduled runs, or PowerShell.
- PDH counters (that is PerfMon).
- Text export formatting (host owns the locked Summary block and file layout).

## Package shape (decision)

One assembly. No satellites.

| Area | Location | Notes |
|---|---|---|
| Records + Fact | root | No P/Invoke |
| Native readers | `Native/` or internal | GetLogicalProcessorInformationEx, GlobalMemoryStatusEx, GetPerformanceInfo, CallNtPowerInformation |
| CIM base | `Cim/` | ManagementObjectSearcher for the five base classes. Returns structured records, not text. |
| Doors | public static | `CpuFacts.Host`, `CpuFacts.Live()`, `MemoryFacts.Read()`, `SystemInventory.ReadBase()` |

Rejected: keep three packages. No consumer wants only the records. Rejected: one package per CIM class. Same problem, worse.

## PR sequence

| PR | Scope | Done when |
|---|---|---|
| PR01 | Collapse to one package. Move native. Add base CIM records + door. Update SysInfoIQ to consume. | Existing doors green. Base summary fields available without host WMI. Package published. |
| PR02 | Storage + Network identity facts (disk drives, logical disks, adapters). Reuse Network helper where it already covers. | SysInfoIQ storage/network pages call library doors. |
| PR03 | Process/Service census using existing Processes/Services helpers. | No new Process.GetProcesses in SysInfoIQ. |
| Later | Remaining catalog sections as needed. Elevation notes stay in host. | — |

## Constraints

- Failed read is `Fact.Unavailable()` or an error record. Never a zero that looks like a measurement.
- No display strings in records. Host formats.
- EVENTID block still unclaimed (19000–19499 is PerfMon.Memory). Confirm before catalog registration.
- Logging via Vestigium.Logging only when the host asks. Library does not open a log.

## Next action

PR01 Implementation Plan and Requirements in the PR01 folder. Then code. No code until those are locked.
