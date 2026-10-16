# PR02 — Implementation Plan

**Companion:** PR02 -- Requirements.md  
**Package:** Vestigium.Helpers.SystemInfo  
**Goal:** Move `StorageCollector` and `NetworkCollector` out of SysInfoIQ into structured doors. Host keeps row mapping, SizeFormat, and export.

## Scope

- Add `SystemInventory.ReadStorage()` and `SystemInventory.ReadNetwork()`.
- Map the queries SysInfoIQ currently runs. Prefer Storage namespace for partitions and volumes; fall back to Win32.
- Reuse `Vestigium.Helpers.Network` where it already covers adapters or TCP.
- Point SysInfoIQ Storage and Network pages at the doors. Delete the two collector files.
- Pack and publish next version.

## Not in PR02

- SecurityCollector, SoftwareCollector.
- Text generation or SizeFormat inside the library.
- Truncation UI. Host decides what to show.
- EVENTID catalog registration.

## Sub-steps

| ID | Step | Action | Done when |
|---|---|---|---|
| PR02.001 | Storage records | Add DiskDrive, LogicalDisk, Partition, Volume records. Raw numbers. No display strings. | Records exist. No P/Invoke. |
| PR02.002 | ReadStorage door | Implement `SystemInventory.ReadStorage()`. Win32_DiskDrive, Win32_LogicalDisk. MSFT_Partition / Win32_DiskPartition. MSFT_Volume / Win32_Volume. Partial success. | Door returns structured records or Unavailable per section. |
| PR02.003 | Network records | Add Adapter, Route, Arp, Listener, TcpConnection records. | Records exist. |
| PR02.004 | ReadNetwork door | Implement `SystemInventory.ReadNetwork()`. NetworkInterface, Win32_IP4RouteTable, MSFT_NetNeighbor, IPGlobalProperties. Reuse Network helper where it matches. | Door returns structured records or Unavailable per section. |
| PR02.005 | Point SysInfoIQ at the doors | Update host Storage and Network pages to call the doors and map to existing rows. Delete `StorageCollector.cs` and `NetworkCollector.cs`. Keep SizeFormat. | Host no longer owns those queries. Display matches (or difference is listed). |
| PR02.006 | Tests and pack | Tests for Unavailable and record shapes. README. Pack. | Tests green. Package published. |

## Risks

- Storage namespace (`root\Microsoft\Windows\Storage`) is not present on all SKUs. Fallback is required.
- ARP and TCP can be large. Host keeps the cap. Library may return full or a documented limit.
- Network helper overlap. Call it; do not duplicate.
- CIM latency. Acceptable for inventory.

## Done when

Both doors exist. Both collector files are deleted. Host pages call the library. Display matches (or difference is listed). Tests green.
