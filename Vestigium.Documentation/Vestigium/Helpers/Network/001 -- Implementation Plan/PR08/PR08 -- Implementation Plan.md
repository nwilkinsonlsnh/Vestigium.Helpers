# Vestigium.Helpers.Network — PR08 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR08  
**Status:** Open  
**Date:** 24 September 2026  
**Backlog:** [`PR08 -- Backlog.md`](PR08%20--%20Backlog.md)  
**Binding:** Requirements win. Commit: `Network PR08: <step>`.

One-sentence goal: make the shipped 1.0 surface tell the truth and be queryable.  
This version is not a new protocol, not pathping, not HTTP, not Charts, not a scheduler.

PR08-07 is the owner gate on the clone.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR08-01 | Fix `NetworkHelper` XML. Linux route write is Option C netlink, not a blanket typed deny. | Open |
| 2 | PR08-02 | Add EVENTID 14530 / 14535 / 14540 / 14545. Update `NetworkEvents`, `EventCatalog/network.json`, `HelperLog.EventId`. Version **1.0.1**. Cut this row only if owner refuses a bump. | Open |
| 3 | PR08-04 | `BillPercentile(NumericSeries, double)` on the façade. | Open |
| 4 | PR08-03 | Stop `Compile Remove` on `NetworkInventoryTests.cs`. Keep Hotspot removed. Tests stay off public Internet and off ProgramData / `/var/lib/vestigium`. | Open |
| 5 | PR08-05 | Point `001/README.md` at this folder. Align SRS / Design / Guide / package README with Option C + packed OUI + no Charts. | Open |
| 6 | PR08-06 | Version rule in csproj + README. 1.0.0 without events; 1.0.1 with PR08-02. | Open |
| 7 | PR08-07 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR08-01

The class summary still says Linux writes throw typed denies. That was the pre-Option C contract. `NetworkRouteMutation` + `NetworkRouteNetlink` already write. Default routes still throw `NetworkRouteDenied`. Missing `CAP_NET_ADMIN` still throws `NetworkRouteDenied`. Comment must match that.

### PR08-02

Do not invent a catalog per subcategory. Four fail IDs. Map:

- Route deny / default-route refuse → 14530
- Campaign path escape → 14535
- DNS UDP peer mismatch → 14540
- Live OUI HTTP reject (allowlist / cap / status) → 14545

Everything else stays on 14510–14525. Catalog JSON is the source hosts copy.

### PR08-03

Inventory tests were removed in the umbrella csproj. That is not “Linux CI.” Restore the file. If a test hits the live public net or writes ProgramData, delete that test, do not keep the whole file removed.

### PR08-04

`BillP95(NumericSeries)` exists. `BillPercentile(IEnumerable<decimal>, double)` exists. The series + percentile overload does not. One method. No new type.

## What this PR does not do

- Duration-per-window.
- pathping.
- Scheduler.
- Full IEEE packed OUI.
- HTTP client for reachability.
- Default-route write.
- Charts.
- Demo.
- Un-waive repo Linux CI.
- Live Ubuntu mutate.
- Re-enable `NetworkHotspotTests`.

## Rejected alternatives

| Idea | Why not |
| :--- | :--- |
| New protocol surface in the same paper | No buyer, no addendum. |
| Bump to 1.1.0 | No API shape change except one overload + IDs. |
| Keep six generic EVENTIDs forever | Queryable deny is the Tuesday-at-2am need. |
| Treat PR06 as still open | Pack refs already flipped on `main`. |

## Watch

- Alvin: do not grow `HelperLog` into a second logger.
- Theodore: inventory tests must not talk to the Internet.
- Simon: packed OUI stays incomplete; do not “fix” it with a live dump.

## Next action

Owner: keep or cut PR08-02 (the 1.0.1 bump). Then implement in table order.
