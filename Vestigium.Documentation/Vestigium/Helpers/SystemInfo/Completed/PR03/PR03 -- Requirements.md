# PR03 — Requirements

**Companion:** PR03 -- Implementation Plan.md  
**Status:** Draft  
**Date:** 16 October 2026  

## What PR03 ships

Security and Software inventory doors. SysInfoIQ `SecurityCollector` and `SoftwareCollector` are deleted. Host pages call the library.

| Door | Returns | Source today |
|---|---|---|
| `SystemInventory.ReadSecurity()` | Firewall profiles, rules, certificates, signed / system drivers | `SecurityCollector.cs` |
| `SystemInventory.ReadSoftware()` | Installed software, hotfixes, features, ODBC, environment, SQL services | `SoftwareCollector.cs` |

Records carry the fields the host currently maps to its row types. No display strings. Host formats.

## Security

| Section | Source | Fields |
|---|---|---|
| Firewall profiles | `MSFT_NetFirewallProfile` (`root\StandardCimv2`) | Name, Enabled, DefaultInboundAction, DefaultOutboundAction, NotifyOnListen, LogAllowed, LogBlocked |
| Firewall rules | `MSFT_NetFirewallRule` (`root\StandardCimv2`) | DisplayName, Enabled, Direction, Action, Profiles, Protocol |
| Certificates | `X509Store` (LocalMachine + CurrentUser; My, Root, CA, TrustedPublisher) | Location, Store, Subject, Issuer, Thumbprint, NotAfter, HasPrivateKey |
| Drivers | `Win32_PnPSignedDriver` + `Win32_SystemDriver` | Source, DeviceName / Name, DriverVersion, Manufacturer / PathName, IsSigned, Signer, InfName / State |

Numeric CIM values (Enabled, Direction, Action, Profiles) stay numeric in the record. Host maps to labels.

## Software

| Section | Source | Fields |
|---|---|---|
| Installed | Uninstall registry (LM + CU, x64 + WOW6432) | Location, Arch, Name, Version, Publisher, InstallDate, Hidden |
| Hotfixes | `Win32_QuickFixEngineering` | HotFixID, Description, InstalledOn, InstalledBy |
| Features | `Win32_OptionalFeature` | Name, InstallState |
| ODBC | ODBCINST.INI + ODBC.INI (same hives) | Location, Arch, Kind, Name, Detail |
| Environment | Process, User, Machine | Scope, Name, Value |
| SQL services | `ServiceController` filtered to MSSQL* / SQL* | Name, DisplayName, Status, StartType |

Reuse `Vestigium.Helpers.WinReg` for Uninstall and ODBC keys. Reuse `Vestigium.Helpers.Services` for SQL services. Do not reimplement.

## Acceptance

1. `ReadSecurity` returns the four sections or Unavailable per section. No ManagementObject or X509Certificate2 leaves the library.
2. `ReadSoftware` returns the six sections or Unavailable per section. Hidden is a bool, not a display string.
3. SysInfoIQ Security and Software pages call the doors. `SecurityCollector.cs` and `SoftwareCollector.cs` are deleted.
4. Host row mapping and label helpers stay. Display values match (or the difference is listed).
5. Truncation caps (rules, drivers, features) stay in the host.
6. Unit tests cover Unavailable paths without live CIM or registry required for the pure records.

## Out

Text export. Remote. Elevation prompts. Event log, tasks, shares.
