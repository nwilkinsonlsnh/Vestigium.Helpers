# Vestigium.Helpers.Kql — PR02 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR02  
**Version:** PR02  
**Status:** Accepted. Step 6 done. Publish not started.  
**Date:** 4 October 2026  
**Binding:** `PR02 -- Requirements.md`, then `PR02 -- Design.md`.

Publish waits on the RouteIQ popup, which now calls Complete. The package bump is step 7.

---

## 0. Done

Step 1 closed 4 October 2026. Documents Accepted.

Step 2 closed 4 October 2026. Completion types and `KqlHelper.Complete`. A negative caret returns an empty list.

Step 3 closed 4 October 2026. Slot scan.

Step 4 closed 4 October 2026. Prefix, then contains. MRU breaks equal-prefix ties only.

Step 5 closed 4 October 2026. `KqlCompletionTests`.

Step 6 closed 4 October 2026. RouteIQ popup under the caret. Tab accepts the highlighted row. Up and down move the highlight. Escape closes. Enter still commits the MRU. The chevron list is unchanged. RouteIQ takes a project reference until step 7 publishes.

Not done: NuGet.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Done. Documents Accepted. |
| 2 | `KqlCompletion` | Done. |
| 3 | Slot scan | Done. |
| 4 | Rank | Done. |
| 5 | Tests | Done. |
| 6 | RouteIQ | Done. Popup, Tab, Up, Down, Escape. Enter still saves. |
| 7 | Publish | Minor bump of `Vestigium.Helpers.Kql`. Then RouteIQ returns to the package reference. |

---

## 2. Tests that define done

```text
route.prot|                         → route.protocol
route.protocol |                    → ==, CONTAINS
route.protocol == |                 → route.protocol.netmgmt, route.protocol(netmgmt)
route.destination BEGINS WITH |     → ipaddress(
route.destination BEGINS WITH ipaddress(172|  → empty
neighbors.mac|                      → neighbors.macaddress
connections.protocol == |           → tcp and udp forms, not netmgmt
```

---

## 3. Out of this plan

A new package, a WPF control in Kql, a model, octet guessing, offering `IN`, publishing before the popup called Complete.

---

## 4. Document control

| Version | Date | Change |
|---|---|---|
| PR02 | 4 Oct 2026 | Plan written. Step 1 Accepted. |
| PR02 | 4 Oct 2026 | Steps 2–5. Completion, scan, rank, tests. |
| PR02 | 4 Oct 2026 | Step 6. RouteIQ popup. |
