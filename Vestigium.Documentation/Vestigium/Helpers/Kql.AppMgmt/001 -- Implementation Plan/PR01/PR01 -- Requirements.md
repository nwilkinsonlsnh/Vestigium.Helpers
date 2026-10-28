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

Two sessions. The package page binds `app.*` only. The Windows Features page binds `feature.*` only.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | One package, two catalogs | Package and Feature live here. Not two projects. |
| 2 | No pack in core | Fields are built with the PR03 factory and passed to `Create(fields)`. |
| 3 | Adapters live here | The host row becomes the values the evaluator reads. Core does not reference AppMgmtIQ. |
| 4 | Two sessions | `Packages()` and `Features()`. |
| 5 | Same dialect | Operators are the KQL PR01 set. No pipe. No new operator. |
| 6 | Unknown stays unknown | A blank string is unknown, not `""`. A missing number is unknown, not `0`. Top-level unknown is not a hit. |
| 7 | QueryBar is the host | This package does not draw a bar and does not write ProgramData. |
| 8 | Canonical is dotted | The insert name is `app.displayname`. Lookup is case-insensitive. |
| 9 | Bare suffix when unique | `displayname` binds `app.displayname`. The bar inserts the canonical. |
| 10 | Size is three fields | One registry value, three number projections. `app.size` is not a field. |
| 11 | IsX86 is the inverse | `app.IsX86 == true` matches a row where `app.IsX64` is false. |
| 12 | Third segment is a field here | `app.size.kb` is a field. It is not a closed value. Size is the exception to the KQL two-segment rule. |

Operators a field may use are the ones its type already accepts. String: `==` `!=` `LIKE` `!LIKE` `CONTAINS` `BEGINS WITH` `ENDS WITH` `IN`. Integer and number: `==` `!=` `<` `>` `<=` `>=` `GT` `LT` `GE` `LE` `GTE` `LTE` `BETWEEN`. Bool: `==` `!=` with `true` or `false`.

---

## 2. Package catalog

Session: `Packages()`. Namespace: `app`.

### 2.1 app.displayname

| | |
|---|---|
| Canonical | `app.displayname` |
| Bare | `displayname` |
| Type | string |
| Source | Registry `DisplayName`. Blank is unknown. |

```text
app.displayname LIKE '%chrome%'
app.displayname == 'Google Chrome'
app.displayname CONTAINS 'chrome'
app.displayname BEGINS WITH 'Microsoft'
displayname LIKE '%office%'
app.DisplayName LIKE '%chrome%'
```

### 2.2 app.publisher

| | |
|---|---|
| Canonical | `app.publisher` |
| Bare | `publisher` |
| Type | string |
| Source | Registry `Publisher`. Blank is unknown. |

```text
app.publisher LIKE '%Microsoft%'
app.publisher == 'Microsoft Corporation'
app.publisher CONTAINS 'Adobe'
publisher BEGINS WITH 'Intel'
```

A blank publisher does not match `app.publisher == ''`.

### 2.3 app.version

| | |
|---|---|
| Canonical | `app.version` |
| Bare | `version` |
| Type | string |
| Source | Registry `DisplayVersion`. Not `MajorVersion`. |

```text
app.version == '1.2.3'
app.version LIKE '1.%'
app.version BEGINS WITH '10'
version CONTAINS 'preview'
```

### 2.4 app.MajorVersion

| | |
|---|---|
| Canonical | `app.MajorVersion` |
| Bare | `MajorVersion` |
| Type | integer |
| Source | Registry `VersionMajor`, else `MajorVersion`. Missing is unknown, not `0`. |

```text
app.MajorVersion == 10
app.MajorVersion GE 10
app.MajorVersion BETWEEN 8 AND 11
MajorVersion GT 4
```

### 2.5 app.MinorVersion

| | |
|---|---|
| Canonical | `app.MinorVersion` |
| Bare | `MinorVersion` |
| Type | integer |
| Source | Registry `VersionMinor`, else `MinorVersion`. Missing is unknown, not `0`. |

```text
app.MinorVersion == 0
app.MinorVersion LT 5
app.MinorVersion BETWEEN 0 AND 2
```

### 2.6 app.InstallDate

| | |
|---|---|
| Canonical | `app.InstallDate` |
| Bare | `InstallDate` |
| Type | string |
| Source | The display string the host already shows. Not a `DateTime` in this revision. |

```text
app.InstallDate LIKE '%2026%'
app.InstallDate == '16 OCT 2026'
app.InstallDate CONTAINS 'OCT'
InstallDate BEGINS WITH '16'
```

A date compare is out. `app.InstallDate GT '2026-01-01'` is not a supported call.

### 2.7 app.size.kb, app.size.mb, app.size.gb

Three fields. One source. Registry `EstimatedSize` is kilobytes.

| Canonical | Value |
|---|---|
| `app.size.kb` | EstimatedSize |
| `app.size.mb` | EstimatedSize / 1024 |
| `app.size.gb` | EstimatedSize / 1024 / 1024 |

Type is number. A missing EstimatedSize is unknown on all three. `app.size` is not a field. `size` is not a bare suffix.

```text
app.size.kb GT 51200
app.size.mb GT 100
app.size.mb BETWEEN 50 AND 500
app.size.gb GT 1
app.size.gb GE 0.5
```

Rejected:

```text
app.size GT 100
app.size.tb GT 1
size.mb GT 100
```

### 2.8 app.IsX64

| | |
|---|---|
| Canonical | `app.IsX64` |
| Bare | `IsX64` |
| Type | bool |
| Source | True when the row came from the native uninstall key. False when it came from `WOW6432Node`. |

```text
app.IsX64 == true
app.IsX64 == false
IsX64 == true
```

### 2.9 app.IsX86

| | |
|---|---|
| Canonical | `app.IsX86` |
| Bare | `IsX86` |
| Type | bool |
| Source | Inverse of `app.IsX64`. Not a second registry read. |

```text
app.IsX86 == true
app.IsX86 == false
```

A row that matches `app.IsX64 == true` matches `app.IsX86 == false`.

### 2.10 app.IsGlobal

| | |
|---|---|
| Canonical | `app.IsGlobal` |
| Bare | `IsGlobal` |
| Type | bool |
| Source | True for `HKLM`. False for `HKCU`. |

```text
app.IsGlobal == true
app.IsGlobal == false
IsGlobal == true
```

### 2.11 app.IsSystemComponent

| | |
|---|---|
| Canonical | `app.IsSystemComponent` |
| Bare | `IsSystemComponent` |
| Type | bool |
| Source | Registry `SystemComponent`. Missing is false. |

```text
app.IsSystemComponent == true
app.IsSystemComponent == false
```

### 2.12 app.WindowsInstaller

| | |
|---|---|
| Canonical | `app.WindowsInstaller` |
| Bare | `WindowsInstaller` |
| Type | bool |
| Source | Registry `WindowsInstaller`. Missing is false. |

```text
app.WindowsInstaller == true
app.WindowsInstaller == false
```

### 2.13 app.NoModify

| | |
|---|---|
| Canonical | `app.NoModify` |
| Bare | `NoModify` |
| Type | bool |
| Source | Registry `NoModify`. Missing is false. |

```text
app.NoModify == true
app.NoModify == false
```

### 2.14 app.NoRemove

| | |
|---|---|
| Canonical | `app.NoRemove` |
| Bare | `NoRemove` |
| Type | bool |
| Source | Registry `NoRemove`. Missing is false. |

```text
app.NoRemove == true
app.NoRemove == false
```

### 2.15 app.NoRepair

| | |
|---|---|
| Canonical | `app.NoRepair` |
| Bare | `NoRepair` |
| Type | bool |
| Source | Registry `NoRepair`. Missing is false. |

```text
app.NoRepair == true
app.NoRepair == false
```

### 2.16 app.HasIcon

| | |
|---|---|
| Canonical | `app.HasIcon` |
| Bare | `HasIcon` |
| Type | bool |
| Source | Computed. True when the host resolved a display icon. Not a registry value. |

```text
app.HasIcon == true
app.HasIcon == false
HasIcon == true
```

### 2.17 Combined

```text
app.publisher LIKE '%Microsoft%' && app.IsX64 == true && app.size.mb GT 50
app.IsGlobal == true && app.NoRemove == true
(app.IsX86 == true || app.size.gb GT 1) && app.HasIcon == true
```

### 2.18 Rejected on this session

```text
app.size GT 100
feature.state == Enabled
app.IsX86 == app.IsX64
app.InstallDate GT '2026-01-01'
```

`app.size` is not a field. `feature.state` is the other session. A field compared to a field is not this dialect. A date compare is not this revision.

---

## 3. Feature catalog

Session: `Features()`. Namespace: `feature`.

### 3.1 feature.caption

| | |
|---|---|
| Canonical | `feature.caption` |
| Bare | `caption` |
| Type | string |
| Source | `Win32_OptionalFeature.Caption`. |

```text
feature.caption LIKE '%Hyper%'
feature.caption CONTAINS '.NET'
caption BEGINS WITH 'Internet'
```

### 3.2 feature.name

| | |
|---|---|
| Canonical | `feature.name` |
| Bare | `name` |
| Type | string |
| Source | `Win32_OptionalFeature.Name`. |

```text
feature.name == 'NetFx3'
feature.name LIKE 'IIS-%'
feature.name BEGINS WITH 'Microsoft-Hyper-V'
```

### 3.3 feature.state

| | |
|---|---|
| Canonical | `feature.state` |
| Bare | `state` |
| Type | string |
| Closed | `Enabled`, `Disabled`, `Absent`, `Unknown` |
| Source | `InstallState`. 1 Enabled, 2 Disabled, 3 Absent, else Unknown. |

```text
feature.state == Enabled
feature.state == feature.state.Disabled
feature.state == feature.state(Absent)
state == Unknown
```

`feature.state == Installed` does not compile. `Installed` is not in the closed set.

### 3.4 feature.IsEnabled

| | |
|---|---|
| Canonical | `feature.IsEnabled` |
| Bare | `IsEnabled` |
| Type | bool |
| Source | True when `InstallState` is 1. |

```text
feature.IsEnabled == true
feature.IsEnabled == false
```

### 3.5 feature.parent

| | |
|---|---|
| Canonical | `feature.parent` |
| Bare | `parent` |
| Type | string |
| Source | The parent feature name from the tree. A root has no parent and is unknown. |

```text
feature.parent == 'NetFx4-AdvSrvs'
feature.parent LIKE 'IIS-%'
parent == 'WCF-Services45'
```

### 3.6 Rejected on this session

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
2. Every accept line in §2 and §3 compiles on its session.
3. Every reject line in §2.18 and §3.6 fails on that session.
4. `app.displayname`, `app.DisplayName`, and `displayname` bind the same field.
5. A blank publisher does not match `app.publisher == ''`.
6. A package with no EstimatedSize does not match `app.size.mb GT 0`.
7. `app.IsX86 == true` matches a row where `app.IsX64` is false.
8. `feature.state == Enabled` offers the closed value on complete. `feature.state == Installed` does not.
9. Core packs still compile. This package does not reference `KqlCatalog`.
