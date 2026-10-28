# Vestigium.Helpers.SystemInfo

Machine facts and inventory reads. Native readers and CIM doors live here. No text, no files.

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.SystemInfo` |
| TFM | `net10.0-windows` |
| Depends on | `System.Management`, `System.ServiceProcess.ServiceController` |

## Doors

| Door | Returns |
|---|---|
| `CpuFacts.Host` / `Live` | Topology and live CPU |
| `MemoryFacts.Read` | Memory snapshot |
| `SystemInventory.ReadBase` | ComputerSystem, OperatingSystem, BIOS, Processor |
| `SystemInventory.ReadStorage` | Disk drives, logical disks, partitions, volumes |
| `SystemInventory.ReadNetwork` | Adapters, routes, ARP, listeners, TCP |
| `SystemInventory.ReadSecurity` | Firewall profiles, rules, certificates, drivers |
| `SystemInventory.ReadSoftware` | Installed, hotfixes, features, ODBC, environment, SQL services |

A failed section is `Fact<T>.Unavailable()`. It is not a zero record. Partial success is allowed.

Raw numbers. No display strings. Host formats.

EVENTID 19000–19499 is already `PerfMon.Memory`. This package does not register a catalog until that block is confirmed.
