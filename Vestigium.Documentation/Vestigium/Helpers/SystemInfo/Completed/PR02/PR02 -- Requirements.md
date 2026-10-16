# PR02 — Requirements

**Companion:** PR02 -- Implementation Plan.md  
**Status:** Draft  
**Date:** 16 October 2026  

## What PR02 ships

Storage and Network inventory doors. SysInfoIQ `StorageCollector` and `NetworkCollector` are deleted. Host pages call the library.

| Door | Returns | Source today |
|---|---|---|
| `SystemInventory.ReadStorage()` | Disk drives, logical disks, partitions, volumes | `StorageCollector.cs` |
| `SystemInventory.ReadNetwork()` | Adapters / IP / DNS, routes, ARP, listeners, TCP | `NetworkCollector.cs` |

Records carry the fields the host currently maps to its row types. No display strings (no GB, no percent, no drive-type names). Host formats.

## Storage

| Section | Preferred query | Fallback | Fields |
|---|---|---|---|
| Disk drives | `Win32_DiskDrive` | — | DeviceID, Caption, InterfaceType, Size, SerialNumber, Status, Partitions, MediaType |
| Logical disks | `Win32_LogicalDisk` | — | DeviceID, VolumeName, FileSystem, DriveType, Size, FreeSpace, VolumeSerialNumber |
| Partitions | `MSFT_Partition` (`root\Microsoft\Windows\Storage`) | `Win32_DiskPartition` | DiskNumber, PartitionNumber, DriveLetter, Size, Offset, GptType / Type, IsBoot, IsSystem, IsHidden, OperationalStatus / Status |
| Volumes | `MSFT_Volume` (`root\Microsoft\Windows\Storage`) | `Win32_Volume` | DriveLetter, FileSystemLabel / Label, FileSystem, DriveType, Size / Capacity, SizeRemaining / FreeSpace, HealthStatus, OperationalStatus, Path / DeviceID |

A failed section is `Fact.Unavailable` (or an empty list with an error record). Partial success is allowed.

## Network

| Section | Source | Fields |
|---|---|---|
| Adapters / IP / DNS | `NetworkInterface.GetAllNetworkInterfaces` | Name, Description, OperationalStatus, PhysicalAddress, UnicastAddresses, GatewayAddresses, DnsAddresses, DnsSuffix, Speed, NetworkInterfaceType, IPv4 DHCP |
| Routes | `Win32_IP4RouteTable` | Destination, Mask, NextHop, InterfaceIndex, Metric1 |
| ARP | `MSFT_NetNeighbor` (`root\StandardCimv2`) | IPAddress, LinkLayerAddress, State, InterfaceIndex |
| Listeners | `IPGlobalProperties` TCP + UDP listeners | Protocol, LocalAddress, Port |
| TCP | `IPGlobalProperties.GetActiveTcpConnections` | LocalAddress, LocalPort, RemoteAddress, RemotePort, State |

Reuse `Vestigium.Helpers.Network` where it already returns the same shape. Do not reimplement.

## Acceptance

1. `ReadStorage` returns the four sections or Unavailable per section. No ManagementObject leaves the library.
2. `ReadNetwork` returns the five sections or Unavailable per section. No raw text.
3. SysInfoIQ Storage and Network pages call the doors. `StorageCollector.cs` and `NetworkCollector.cs` are deleted.
4. Host row mapping and SizeFormat stay. Display values match (or the difference is listed).
5. Truncation caps (ARP, TCP) stay in the host. Library returns the full set or a documented cap with a flag.
6. Unit tests cover Unavailable paths without live CIM required for the pure records.

## Out

Security, Software. Text export. Remote. Elevation prompts.
