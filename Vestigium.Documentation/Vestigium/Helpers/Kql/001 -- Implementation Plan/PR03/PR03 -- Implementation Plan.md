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

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR03.001 | Accept this folder. | Draft |
| 2 | PR03.002 | Public `KqlField` factory. Internal constructor unchanged. | |
| 3 | PR03.003 | `KqlHelper.Create(fields)`. Session with empty packs. Lookup is canonical, unique suffix, aliases. | |
| 4 | PR03.004 | Tests. Requirements §5. Existing pack queries still compile. | |
| 5 | PR03.005 | Publish. After `Vestigium.Helpers.Kql.AppMgmt` consumes the door. Not before. | |

PR03.002 through PR03.004 are this repo. PR03.005 waits on the AppMgmt package.

---

## 2. Out of this plan

AppMgmt catalogs, row adapters, the query bar, saved queries, a new pack enum value, moving Process or Route out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 17 Oct 2026 | Plan written. Implementation not started. |
| PR03 | 17 Oct 2026 | Steps renumbered PR03.001–PR03.005. |
