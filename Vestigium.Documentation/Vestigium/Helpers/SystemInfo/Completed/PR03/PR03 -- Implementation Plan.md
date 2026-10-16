# PR03 — Implementation Plan

**Companion:** PR03 -- Requirements.md  
**Package:** Vestigium.Helpers.SystemInfo  
**Goal:** Move `SecurityCollector` and `SoftwareCollector` out of SysInfoIQ into structured doors. Host keeps row mapping, label helpers, and export. After this PR the host is a skeleton.

## Scope

- Add `SystemInventory.ReadSecurity()` and `SystemInventory.ReadSoftware()`.
- Map the queries SysInfoIQ currently runs.
- Reuse `Vestigium.Helpers.WinReg` for Uninstall and ODBC. Reuse `Vestigium.Helpers.Services` for SQL services.
- Point SysInfoIQ Security and Software pages at the doors. Delete the two collector files.
- Pack and publish next version.

## Not in PR03

- Text generation or label mapping inside the library.
- Truncation UI. Host decides what to show.
- Event log, scheduled tasks, shares.
- EVENTID catalog registration.

## Sub-steps

| ID | Step | Action | Done when |
|---|---|---|---|
| PR03.001 | Security records | Add FirewallProfile, FirewallRule, Certificate, Driver records. Numeric CIM values stay numeric. | Records exist. |
| PR03.002 | ReadSecurity door | Implement `SystemInventory.ReadSecurity()`. MSFT_NetFirewallProfile, MSFT_NetFirewallRule, X509Store, Win32_PnPSignedDriver, Win32_SystemDriver. Partial success. | Door returns structured records or Unavailable per section. |
| PR03.003 | Software records | Add InstalledSoftware, Hotfix, Feature, Odbc, Environment, SqlService records. Hidden is bool. | Records exist. |
| PR03.004 | ReadSoftware door | Implement `SystemInventory.ReadSoftware()`. Uninstall registry via WinReg, Win32_QuickFixEngineering, Win32_OptionalFeature, ODBC registry, environment, SQL services via Services helper. | Door returns structured records or Unavailable per section. |
| PR03.005 | Point SysInfoIQ at the doors | Update host Security and Software pages to call the doors and map to existing rows. Delete `SecurityCollector.cs` and `SoftwareCollector.cs`. Keep label helpers. | Host no longer owns those queries. Display matches (or difference is listed). |
| PR03.006 | Tests and pack | Tests for Unavailable and record shapes. README. Pack. | Tests green. Package published. Host is a skeleton. |

## Risks

- Certificate stores can be locked. A closed store is a skipped store, not a failed section, unless all stores fail.
- Firewall rules can be large. Host keeps the cap.
- WinReg / Services overlap. Call them; do not duplicate.
- Elevation. Library reports Unavailable. Host shows the note.

## Done when

Both doors exist. Both collector files are deleted. Host pages call the library. No inventory queries remain in SysInfoIQ. Display matches (or difference is listed). Tests green.
