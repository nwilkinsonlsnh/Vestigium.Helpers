# Vestigium.Helpers.Kql.AppMgmt — PR01 Requirements

**Document ID:** VEST-HLP-KQL-AM-SRS-PR01  
**Version:** PR01  
**Status:** Draft. Documents only. Implementation not started. Do not publish.  
**Date:** 17 October 2026  
**Package:** `Vestigium.Helpers.Kql.AppMgmt`  
**Depends on:** `Vestigium.Helpers.Kql` PR03 door (`Create(fields)`)  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Binding:** This file wins for this package on conflict with chat. The KQL dialect still wins in `Vestigium.Helpers.Kql`.

If implementation and this file disagree, this file wins.

---

## 0. Purpose

Own the field catalogs and the row adapters for AppMgmtIQ. Core KQL does not learn about packages or features.

Two sessions. The package page and the Windows Features page do not share a catalog.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | One package, two catalogs | Package and Feature live here. Not two projects. |
| 2 | No pack in core | Fields are built with the PR03 factory and passed to `Create(fields)`. |
| 3 | Adapters live here | `PackageRow` and `FeatureRow` become the values the evaluator reads. Core does not reference AppMgmtIQ. |
| 4 | Two sessions | `Packages()` and `Features()`. A package query does not bind `State`. A feature query does not bind `Publisher`. |
| 5 | Same dialect | Operators are the PR01 set. No pipe. No new operator. |
| 6 | Unknown stays unknown | A blank publisher is unknown, not `""`. A missing size is unknown, not `0`. |
| 7 | QueryBar is the host | This package does not draw a bar and does not write ProgramData. |

---

## 2. Package catalog

| Canonical | Type | Aliases | Notes |
|---|---|---|---|
| DisplayName | string | Name | |
| Publisher | string | | |
| DisplayVersion | string | Version | |
| Size | number | | Estimated size, already in the display unit the host chose. Compare the number. |
| InstallDate | string | Date | Display string, not a parsed date, in this revision. |
| IsX64 | bool | | |
| IsGlobal | bool | | |
| SystemComponent | bool | | |
| WindowsInstaller | bool | | |
| NoModify | bool | | |
| NoRemove | bool | | |
| NoRepair | bool | | |

Examples that must compile on the package session:

```text
Publisher LIKE '%Microsoft%'
Size GT 100 && IsX64 == true
IsGlobal == true && NoRemove == true
DisplayName LIKE '%chrome%'
Name LIKE '%office%'
```

`State == Enabled` does not compile on this session.

---

## 3. Feature catalog

| Canonical | Type | Aliases | Closed |
|---|---|---|---|
| Caption | string | | |
| Name | string | | |
| State | string | | Enabled, Disabled, Absent, Unknown |
| IsEnabled | bool | | |
| Parent | string | | |

Examples that must compile on the feature session:

```text
State == Enabled
Caption LIKE '%Hyper%'
Parent == 'NetFx4-AdvSrvs'
IsEnabled == true
```

`Publisher LIKE '%Microsoft%'` does not compile on this session.

---

## 4. What this package does not do

- Parse or rank. That is core KQL.
- Draw `VestigiumQueryBar`
- Filter the grid. The host applies the compiled predicate.
- Store saved queries
- Enable or disable a Windows feature
- Publish as part of accepting this document

---

## 5. Gate

1. Both sessions open from the PR03 door.
2. Every example in §2 and §3 compiles on its session and fails on the other.
3. A blank publisher does not match `Publisher == ''`.
4. `State == Enabled` offers the closed value on complete. `State == Installed` does not.
5. Core packs still compile. This package does not reference `KqlCatalog`.
