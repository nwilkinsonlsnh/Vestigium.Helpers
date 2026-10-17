# Vestigium.Helpers.Kql.AppMgmt — PR01 Implementation Plan

**Document ID:** VEST-HLP-KQL-AM-PLN-PR01  
**Version:** PR01  
**Status:** Live. Host bars are in. Publish already done ahead of this step.  
**Date:** 17 October 2026  
**Binding:** `PR01 -- Requirements.md`.

---

## 0. Done

PR01.001 closed 17 October 2026. Documents Accepted.

PR01.002 closed 17 October 2026. `src/Vestigium.Helpers.Kql.AppMgmt` is in the Helpers solution. Project reference to KQL. No Demo project.

PR01.003 closed 17 October 2026. `PackageCatalog` and `FeatureCatalog`.

PR01.004 closed 17 October 2026. `AppMgmtKql.Packages()` and `AppMgmtKql.Features()`.

PR01.005 closed 17 October 2026. `PackageAdapter` and `FeatureAdapter`. Blank and missing stay unknown. Size is projected from kilobytes. `IsX86` is the inverse of `IsX64`.

PR01.006 closed 17 October 2026. `AppMgmtKqlTests`. Accepts, cross-session rejects, case and suffix, blank publisher, missing size, IsX86 inverse, closed state completion, core pack still compiles.

PR01.007 closed 17 October 2026. QueryBar on the package page and the features page. Host compiles and filters. Empty text shows all. A bad query keeps the last good rows. Saved queries are an empty collection in this revision.

PR01.008 closed 17 October 2026. `Vestigium.Helpers.Kql` 2.0.0 and `Vestigium.Helpers.Kql.AppMgmt` 1.0.0 are on nuget.org. Pulled forward so the host could restore.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR01.001 | Accept this folder. | Done |
| 2 | PR01.002 | `src/Vestigium.Helpers.Kql.AppMgmt`. Project in the Helpers solution. Project reference to KQL. No Demo project. | Done |
| 3 | PR01.003 | `PackageCatalog` and `FeatureCatalog`. Fields from the PR03 factory. | Done |
| 4 | PR01.004 | `AppMgmtKql.Packages()` and `AppMgmtKql.Features()`. | Done |
| 5 | PR01.005 | Package row and feature row adapters. Blank and missing stay unknown. | Done |
| 6 | PR01.006 | Tests. Requirements §5. Cross-session rejects included. | Done |
| 7 | PR01.007 | AppMgmtIQ. QueryBar on the package page, then the features page. Host filter. Not in this package. | Done |
| 8 | PR01.008 | Publish. After PR01.007 consumes the sessions. Not before. | Done |

---

## 2. Out of this plan

Dashboard charts, enable and disable, uninstall, export, saved-query persistence, a date-typed `InstallDate`, moving the existing KQL packs out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 17 Oct 2026 | Plan written. Implementation not started. |
| PR01 | 17 Oct 2026 | Steps renumbered PR01.001–PR01.008. |
| PR01 | 17 Oct 2026 | PR01.001–PR01.004 closed. Catalog and sessions in. |
| PR01 | 17 Oct 2026 | PR01.005 closed. Adapters in. |
| PR01 | 17 Oct 2026 | PR01.006 closed. Tests in. |
| PR01 | 17 Oct 2026 | PR01.007 closed. Host bars in. PR01.008 pulled forward. |
