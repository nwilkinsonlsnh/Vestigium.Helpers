# PR01 — Requirements

**Companion:** PR01 -- Implementation Plan.md  
**Status:** Draft  
**Date:** 15 October 2026  

## What PR01 ships

One package. Existing Cpu and Memory doors preserved. New base inventory door.

| Item | Rule |
|---|---|
| Package | Single `Vestigium.Helpers.SystemInfo`. Satellites removed. |
| CpuFacts.Host / Live | Same signatures and Unavailable semantics. |
| MemoryFacts.Read | Same MemoryRead shape. Per-field Fact. |
| SystemInventory.ReadBase | Returns BaseInventory with Summary-block fields. Partial success allowed. |
| SysInfoIQ | Calls the door. Does not run ManagementObjectSearcher for the five base classes. Still formats Summary and writes files. |

## Acceptance

1. Solution builds with one SystemInfo project.
2. Existing unit tests (or new ones) pass for topology, live, and memory Unavailable paths.
3. ReadBase returns expected fields or Unavailable on a test machine / mocked path.
4. SysInfoIQ CollectBase produces the same Summary text as before (or documents the difference).
5. No new public types that leak ManagementObject or raw text.

## Out

Everything else in the SectionsCatalog. Text export. Remote.
