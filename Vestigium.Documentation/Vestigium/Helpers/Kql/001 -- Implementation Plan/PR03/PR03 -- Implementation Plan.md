# Vestigium.Helpers.Kql — PR03 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR03  
**Version:** PR03  
**Status:** Live. Door is in. Tests and publish not started.  
**Date:** 17 October 2026  
**Binding:** `PR03 -- Requirements.md`.

---

## 0. Done

PR03.002 closed 17 October 2026. `KqlField.Create`. Internal constructor unchanged.

PR03.003 closed 17 October 2026. `KqlHelper.Create(fields)`. Empty packs. Lookup is canonical, unique suffix, aliases.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR03.001 | Accept this folder. | Done |
| 2 | PR03.002 | Public `KqlField` factory. Internal constructor unchanged. | Done |
| 3 | PR03.003 | `KqlHelper.Create(fields)`. Session with empty packs. Lookup is canonical, unique suffix, aliases. | Done |
| 4 | PR03.004 | Tests. Requirements §5. Existing pack queries still compile. | |
| 5 | PR03.005 | Publish. After `Vestigium.Helpers.Kql.AppMgmt` consumes the door. Not before. | |

PR03.004 is this repo. PR03.005 waits on the AppMgmt package consuming the door. The project reference is in. Publish still waits.

---

## 2. Out of this plan

AppMgmt catalogs, row adapters, the query bar, saved queries, a new pack enum value, moving Process or Route out of core.

---

## 3. Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 17 Oct 2026 | Plan written. Implementation not started. |
| PR03 | 17 Oct 2026 | Steps renumbered PR03.001–PR03.005. |
| PR03 | 17 Oct 2026 | PR03.002 and PR03.003 closed. Door is in. |
