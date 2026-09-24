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
| 1 | PR08-01 | Fix `NetworkHelper` XML. Linux route write is Option C netlink, not a blanket typed deny. | Done (PR07-05, on main) |
| 2 | PR08-02 | Fail IDs for route deny, campaign path escape, DNS peer mismatch, and live OUI reject. 14530 stays RouteDenied. 14535 and 14540 stay the PR07 events. New IDs are 14545 / 14550 / 14555. Version stays **1.0.1**. | Done |
| 3 | PR08-04 | `BillPercentile(NumericSeries, double)` on the façade. | Done |
| 4 | PR08-03 | Stop `Compile Remove` on `NetworkInventoryTests.cs`. Keep Hotspot removed. Tests stay off public Internet and off ProgramData / `/var/lib/vestigium`. | Open |
| 5 | PR08-05 | Point `001/README.md` at this folder. Align SRS / Design / Guide / package README with Option C + packed OUI + no Charts. | Open |
| 6 | PR08-06 | Version rule in csproj + README. 1.0.0 without events; 1.0.1 with PR08-02. | Open |
| 7 | PR08-07 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner |

## Slice notes

### PR08-01

Done on main by PR07-05. `NetworkHelper` summary is Option C: Windows IPv4 IP Helper + HKLM persist, Windows IPv6 `CreateIpForwardEntry2`, Linux IPv4 and IPv6 netlink. Typed deny only for missing admin / `CAP_NET_ADMIN`, ACL, or a default-route write. No `route` / `ip` / `netsh`.

### PR08-02

14530 `RouteDenied` was already wired. 14535 `IcmpForbidden` and 14540 `CampaignWindowMissed` stay. Do not renumber them.

New, still count by 5:

- 14545 `CampaignPathEscape` — campaign path leaves the root
- 14550 `DnsPeerMismatch` — UDP foreign source (warning, packet dropped) and TCP peer mismatch (error)
- 14555 `OuiLookupRejected` — allowlist, blocked host, redirect, HTTP status, body cap

Everything else stays on 14510–14525. `network.json` is the list hosts copy. Package version stays 1.0.1.

### PR08-03

Inventory tests were removed in the umbrella csproj. That is not “Linux CI.” Restore the file. If a test hits the live public net or writes ProgramData, delete that test, do not keep the whole file removed.

### PR08-04

Done. `NetworkHelper.BillPercentile(NumericSeries, double)` calls `PercentileBillEngine.FromSeries`. No new type. `BillP95(NumericSeries)` still passes 0.95.

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
- Simon: do not pack an IEEE OUI. The registry changes. Lookup is a URL the caller points at, fetched on that request. Do not grow `_Data/oui-snapshot.txt`.

## Next action

Owner: keep or cut PR08-02 (the 1.0.1 bump). Then implement in table order.
