# Vestigium.Helpers.Kql.AppMgmt — PR01 Implementation Plan

**Document ID:** VEST-HLP-KQL-AM-PLN-PR01  
**Version:** PR01  
**Status:** Draft. Implementation not started. Do not publish.  
**Date:** 17 October 2026  
**Binding:** `PR01 -- Requirements.md`.

This package does not exist yet. The project is created in step 2.

---

## 0. Done

Nothing. Documents only.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Documents Accepted. |
| 2 | `src/Vestigium.Helpers.Kql.AppMgmt` | Project in the Helpers solution. Package reference to KQL at the PR03 version. No Demo project. |
| 3 | Catalogs | `PackageCatalog` and `FeatureCatalog`. Fields from the PR03 factory. |
| 4 | Sessions | `AppMgmtKql.Packages()` and `AppMgmtKql.Features()`. |
| 5 | Adapters | Package row and feature row to evaluator values. Blank and missing stay unknown. |
| 6 | Tests | Requirements §5. Cross-session rejects included. |
| 7 | AppMgmtIQ | QueryBar on the package page, then the features page. Host filter. Not in this package. |
| 8 | Publish | After step 7 consumes the sessions. Not before. |

Steps 2–6 are this repo. Step 7 is `Vestigium.Suite.Windows`. Step 8 waits on step 7.

Step 2 does not start until KQL PR03 step 3 is in.

---

## 2. Out of this plan

Dashboard charts, enable and disable, uninstall, export, saved-query persistence, a date-typed `InstallDate`, moving the existing KQL packs out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR01 | 17 Oct 2026 | Plan written. Implementation not started. |
