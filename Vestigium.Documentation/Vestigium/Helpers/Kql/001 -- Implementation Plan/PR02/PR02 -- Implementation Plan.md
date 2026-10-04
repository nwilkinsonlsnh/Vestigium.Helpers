# Vestigium.Helpers.Kql — PR02 Implementation Plan

**Document ID:** VEST-HLP-KQL-PLN-PR02  
**Version:** PR02  
**Status:** Accepted. Step 3 done. Ranking not started.  
**Date:** 4 October 2026  
**Binding:** `PR02 -- Requirements.md`, then `PR02 -- Design.md`.

Publish waits on RouteIQ calling Complete.

---

## 0. Done

Step 1 closed 4 October 2026. Documents Accepted.

Step 2 closed 4 October 2026. `KqlCompletion`, `KqlCompletionRow`, `KqlCompletionSlot`, `KqlCompletionKind`. `KqlHelper.Complete(text, caret, session, hints)`. A negative caret returns `KqlCompletion.Empty`. A caret past the end clamps. No throw. Hints are accepted and not read yet.

Step 3 closed 4 October 2026. Slot scan in `KqlCompleter`. Field, operator, value, join, none. The lexer reads finished tokens. The caret token is the prefix filter. An unclosed constructor is none. A lex error returns an empty list. Hints are still not read.

Not done: contains ranking, MRU boost, tests, RouteIQ popup, NuGet.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Done. Documents Accepted. |
| 2 | `KqlCompletion` | Done. Result and row types. `KqlHelper.Complete`. Empty list on a bad caret. No throw. |
| 3 | Slot scan | Done. Field, operator, value, join, none. Lexer on finished tokens. Prefix filter. No predicate bind. |
| 4 | Rank | Prefix, then contains. MRU texts boost equal prefixes only. |
| 5 | Tests | Every example in Requirements §6. Connections does not offer a route field. Insert of a value row compiles under PR01. |
| 6 | RouteIQ | Popup under the caret. Tab accepts. Up and down move the highlight. Enter still commits the MRU. Not in this package. |
| 7 | Publish | After step 6 consumes Complete. Minor bump of `Vestigium.Helpers.Kql`. Not before. |

Steps 2–5 are this repo. Step 6 is `Vestigium.Suite.Network`. Step 7 waits on step 6.

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

A Connections session given `route.des` returns no `route.destination` row.

Regression: PR01 accept lines still compile. `PID == 0` still completes a Process field.

---

## 3. Out of this plan

A new package, a WPF control in Kql, a model, octet guessing, offering `IN`, publishing before RouteIQ calls Complete.

---

## 4. Document control

| Version | Date | Change |
|---|---|---|
| PR02 | 4 Oct 2026 | Plan written. Step 1 Accepted. Implementation not started. |
| PR02 | 4 Oct 2026 | Step 2. Completion types and Complete. Empty list on a bad caret. |
| PR02 | 4 Oct 2026 | Step 3. Slot scan. Prefix filter. Hints not read. |
