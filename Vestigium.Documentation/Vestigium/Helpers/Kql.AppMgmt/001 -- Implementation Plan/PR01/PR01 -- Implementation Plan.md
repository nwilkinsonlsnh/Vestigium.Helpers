# Vestigium.Helpers.Kql.AppMgmt — PR01 Implementation Plan

**Document ID:** VEST-HLP-KQL-AM-PLN-PR01  
**Version:** PR01  
**Status:** Draft. Implementation not started. Do not publish.  
**Date:** 17 October 2026  
**Binding:** `PR01 -- Requirements.md`.

This package does not exist yet. The project is created in PR01.002.

---

## 0. Done

Nothing. Documents only.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR01.001 | Accept this folder. | Draft |
| 2 | PR01.002 | `src/Vestigium.Helpers.Kql.AppMgmt`. Project in the Helpers solution. Package reference to KQL at the PR03 version. No Demo project. | |
| 3 | PR01.003 | `PackageCatalog` and `FeatureCatalog`. Fields from the PR03 factory. | |
| 4 | PR01.004 | `AppMgmtKql.Packages()` and `AppMgmtKql.Features()`. | |
| 5 | PR01.005 | Package row and feature row adapters. Blank and missing stay unknown. | |
| 6 | PR01.006 | Tests. Requirements §5. Cross-session rejects included. | |
| 7 | PR01.007 | AppMgmtIQ. QueryBar on the package page, then the features page. Host filter. Not in this package. | |
| 8 | PR01.008 | Publish. After PR01.007 consumes the sessions. Not before. | |

PR01.002 through PR01.006 are this repo. PR01.007 is `Vestigium.Suite.Windows`. PR01.008 waits on PR01.007.

PR01.002 does not start until KQL PR03.003 is in.

---

## 2. Out of this plan

Dashboard charts, enable and disable, uninstall, export, saved-query persistence, a date-typed `InstallDate`, moving the existing KQL packs out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 17 Oct 2026 | Plan written. Implementation not started. |
| PR01 | 17 Oct 2026 | Steps renumbered PR01.001–PR01.008. |
