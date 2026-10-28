# Vestigium.Helpers.SystemInfo — Design

**Companion:** Requirements.md  
**Package:** `Vestigium.Helpers.SystemInfo`  
**Status:** Draft — evolves as PR01 / PR02 / PR03 land  
**Date:** 15 October 2026  

## Purpose

One package that owns machine inventory reads. Hosts call doors and format. The library does not format text or write files.

## Shape

One assembly. No satellites.

```
Vestigium.Helpers.SystemInfo
├── Fact.cs                  // Fact<T>, FactStatus
├── Records/                  // pure records (CpuTopology, CpuLive, MemoryRead, BaseInventory, …)
├── Native/                   // CpuFacts, MemoryFacts (P/Invoke)
├── Inventory/
│   ├── SystemInventory.cs    // public doors: ReadBase, ReadStorage, ReadNetwork, ReadSecurity, ReadSoftware
│   └── Cim.cs                // internal ManagementObjectSearcher wrapper
└── Vestigium.Helpers.SystemInfo.csproj
```

Public surface is static doors and records. No ManagementObject, no file paths, no display strings leave the assembly.

## Doors (current + planned)

| Door | Returns | Owner of the query today | Lands in |
|---|---|---|---|
| `CpuFacts.Host` / `Live()` | `Fact<CpuTopology>` / `Fact<CpuLive>` | This package (native) | Existing |
| `MemoryFacts.Read()` | `MemoryRead` | This package (native) | Existing |
| `SystemInventory.ReadBase()` | `BaseInventory` | SysInfoIQ `Collector.cs` | PR01 |
| `SystemInventory.ReadStorage()` | Storage records | SysInfoIQ `StorageCollector.cs` | PR02 |
| `SystemInventory.ReadNetwork()` | Network records | SysInfoIQ `NetworkCollector.cs` | PR02 |
| `SystemInventory.ReadSecurity()` | Security records | SysInfoIQ `SecurityCollector.cs` | PR03 |
| `SystemInventory.ReadSoftware()` | Software records | SysInfoIQ `SoftwareCollector.cs` | PR03 |

## Records

- `Fact<T>` is the only way a missing read is expressed. `Unavailable` is not a zero.
- Numeric fields are raw (bytes, MHz, counts). No display strings.
- `BaseInventory` carries the fields the locked Summary block needs. Other inventory records carry the fields the host currently writes to section files.
- Partial success is allowed. A missing section is marked unavailable; the rest of the inventory still returns.

## What the host keeps

- `SummaryFormatter` and the locked Summary text.
- `Exporter` and the file layout (`{Hostname}_{Section}.txt`, manifest).
- Elevation notes and section selection UI.
- Logging and settings.

## What the host loses

After PR01 + PR02 + PR03, the five collector files are gone. The host ViewModels call library doors. No `ManagementObjectSearcher`, no Registry inventory queries, and no `NetworkInterface` inventory queries remain in SysInfoIQ.

## Evolution

This document updates when a PR lands and the door list or record shapes change. Requirements.md stays the binding contract. This file is the map.
