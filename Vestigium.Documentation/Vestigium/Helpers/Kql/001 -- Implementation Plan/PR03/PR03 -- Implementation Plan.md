# Vestigium.Helpers.Kql — PR03 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR03  
**Version:** PR03  
**Status:** Draft. Implementation not started. Do not publish.  
**Date:** 17 October 2026  
**Binding:** `PR03 -- Requirements.md`.

---

## 0. Done

Nothing. Documents only.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Documents Accepted. |
| 2 | `KqlField` factory | Public factory. Internal constructor unchanged. |
| 3 | `KqlHelper.Create(fields)` | Session with empty packs. Lookup is canonical, unique suffix, aliases. |
| 4 | Tests | Requirements §5. Existing pack queries still compile. |
| 5 | Publish | After `Vestigium.Helpers.Kql.AppMgmt` consumes the door. Not before. |

Steps 2–4 are this repo. Step 5 waits on the AppMgmt package.

---

## 2. Out of this plan

AppMgmt catalogs, row adapters, the query bar, saved queries, a new pack enum value, moving Process or Route out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 17 Oct 2026 | Plan written. Implementation not started. |
