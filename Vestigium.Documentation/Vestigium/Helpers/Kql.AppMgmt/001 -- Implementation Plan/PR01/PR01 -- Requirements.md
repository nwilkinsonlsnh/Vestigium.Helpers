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
| 4 | Two sessions | `Packages()` and `Features()`. A package query does not bind `feature.state`. A feature query does not bind `app.publisher`. |
| 5 | Same dialect | Operators are the PR01 set. No pipe. No new operator. |
| 6 | Unknown stays unknown | A blank publisher is unknown, not `""`. A missing size is unknown, not `0`. |
| 7 | QueryBar is the host | This package does not draw a bar and does not write ProgramData. |
| 8 | Canonical is dotted | The insert name is `app.displayname`. Lookup is case-insensitive. |
| 9 | Bare suffix when unique | `displayname` binds `app.displayname`. The bar inserts the canonical. |
| 10 | Size is three fields | One registry value, three number projections. Not three stored sizes. |
| 11 | IsX86 is the inverse | `app.IsX86 == true` is `app.IsX64 == false`. Both are offered. |

---

## 2. Package catalog

Registry `EstimatedSize` is kilobytes. `app.size.kb` is that number. `app.size.mb` is kb / 1024. `app.size.gb` is mb / 1024. A missing size is unknown on all three.

`app.MajorVersion` and `app.MinorVersion` are the integer registry values. A missing value is unknown, not `0`.

`app.HasIcon` is computed. True when the host resolved a display icon. It is not a registry value.

`app.InstallDate` compares the display string in this revision. It is not a `DateTime`.

| Canonical | Type | Bare suffix | Notes |
|---|---|---|---|
| app.displayname | string | displayname | |
| app.publisher | string | publisher | |
| app.version | string | version | DisplayVersion. |
| app.MajorVersion | integer | MajorVersion | |
| app.MinorVersion | integer | MinorVersion | |
| app.InstallDate | string | InstallDate | Display string. |
| app.size.kb | number | | EstimatedSize as KB. |
| app.size.mb | number | | KB / 1024. |
| app.size.gb | number | | MB / 1024. |
| app.IsX64 | bool | IsX64 | |
| app.IsX86 | bool | IsX86 | Inverse of IsX64. |
| app.IsGlobal | bool | IsGlobal | HKLM. |
| app.IsSystemComponent | bool | IsSystemComponent | |
| app.WindowsInstaller | bool | WindowsInstaller | |
| app.NoModify | bool | NoModify | |
| app.NoRemove | bool | NoRemove | |
| app.NoRepair | bool | NoRepair | |
| app.HasIcon | bool | HasIcon | Computed. |

`size` is not a field. `app.size` does not bind. The unit is required.

### 2.1 Syntax that must compile

```text
app.displayname LIKE '%chrome%'
displayname LIKE '%office%'
app.publisher LIKE '%Microsoft%'
app.version == '1.2.3'
app.MajorVersion GE 10
app.MinorVersion == 0
app.InstallDate LIKE '%2026%'
app.size.mb GT 100
app.size.kb GT 51200
app.size.gb GT 1
app.IsX64 == true
app.IsX86 == true
app.IsGlobal == true
app.IsSystemComponent == false
app.WindowsInstaller == true
app.NoModify == true
app.NoRemove == true
app.NoRepair == false
app.HasIcon == true
app.publisher LIKE '%Microsoft%' && app.IsX64 == true && app.size.mb GT 50
```

Case does not matter. These are the same field:

```text
app.displayname LIKE '%chrome%'
app.DisplayName LIKE '%chrome%'
APP.DISPLAYNAME LIKE '%chrome%'
```

### 2.2 Syntax that must not compile

```text
app.size GT 100
feature.state == Enabled
app.IsX86 == app.IsX64
```

`app.size` is not a field. `feature.state` is the other session. A field compared to a field is not this dialect.

---

## 3. Feature catalog

| Canonical | Type | Bare suffix | Closed |
|---|---|---|---|
| feature.caption | string | caption | |
| feature.name | string | name | |
| feature.state | string | state | Enabled, Disabled, Absent, Unknown |
| feature.IsEnabled | bool | IsEnabled | |
| feature.parent | string | parent | |

### 3.1 Syntax that must compile

```text
feature.state == Enabled
feature.caption LIKE '%Hyper%'
feature.parent == 'NetFx4-AdvSrvs'
feature.IsEnabled == true
state == Disabled
```

### 3.2 Syntax that must not compile

```text
app.publisher LIKE '%Microsoft%'
feature.state == Installed
```

---

## 4. What this package does not do

- Parse or rank. That is core KQL.
- Draw `VestigiumQueryBar`
- Filter the grid. The host applies the compiled predicate.
- Store saved queries
- Enable or disable a Windows feature
- Parse `app.InstallDate` as a date
- Publish as part of accepting this document

---

## 5. Gate

1. Both sessions open from the PR03 door.
2. Every line in §2.1 and §3.1 compiles on its session.
3. Every line in §2.2 and §3.2 fails on that session.
4. `app.displayname`, `app.DisplayName`, and `displayname` bind the same field.
5. A blank publisher does not match `app.publisher == ''`.
6. A package with no EstimatedSize does not match `app.size.mb GT 0`.
7. `app.IsX86 == true` matches a row where `app.IsX64` is false.
8. `feature.state == Enabled` offers the closed value on complete. `feature.state == Installed` does not.
9. Core packs still compile. This package does not reference `KqlCatalog`.
