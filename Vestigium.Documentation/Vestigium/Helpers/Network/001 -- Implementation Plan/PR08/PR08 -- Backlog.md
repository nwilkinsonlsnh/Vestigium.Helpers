# Vestigium.Helpers.Network — PR08 Backlog

**Document ID:** VEST-HLP-NETWORK-PR08-BL  
**Package:** `Vestigium.Helpers.Network` 1.0.0 on tree; 1.0.1 if EVENTIDs land  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Network/001 -- Implementation Plan/PR08/`  
**Binding:** `Requirements_v1.6.md` wins. This backlog amends paper only where a row says so.

## Intent

PR01–PR05 shipped the protocol surface. PR06 planned the nuget flip; the packable csproj already speaks Json 1.0.1, Analytics 1.0.1, FileIo 1.1.1. The live `001` folder had no plan.

PR08 is the next wave that can ship without a new protocol: stale comments, queryable fail IDs, the inventory tests that are `Compile Remove`d, and paper that matches Option C.

PR08 is not pathping, not HTTP reachability, not Charts, not Demo, not a scheduler package, not a full IEEE OUI dump, not default-route write, not repo Linux CI, not live Ubuntu mutate, not duration-per-window.

## What the library is today

| Slice | State |
| :--- | :--- |
| Façade | `NetworkHelper` — inventory, ICMP echo/trace, DNS, stack tables, Option C routes, echo + share campaigns, prefix math, MAC/OUI, bandwidth, P95 |
| TFM | `net10.0`. Tests project is `net10.0-windows`. |
| EVENTID | Reserved 14500–14999, used 14500–14525 only. Six generic rows. |
| Pack refs | Json 1.0.1, Analytics 1.0.1, FileIo 1.1.1, Logging 1.7.1 |
| Charts | Not referenced. Keep it that way. |
| Packed OUI | Embedded stub. Incomplete on purpose. |

## Review — keep / fix / cut

### Fixes (in)

| ID | Item | Why |
| :--- | :--- | :--- |
| PR08-01 | Stale `NetworkHelper` XML: “Linux writes throw typed denies.” Option C writes Linux via netlink. | Comment lies. Hosts will copy it. |
| PR08-02 | Named fail IDs: RouteDenied 14530, CampaignPathReject 14535, DnsPeerReject 14540, OuiLookupFailed 14545. Wire `HelperLog` + `network.json` + `NetworkEvents`. | Json already did this pattern. Six generic IDs cannot be queried. Step 5. Stay in 14500–14999. |
| PR08-03 | `NetworkInventoryTests.cs` is `Compile Remove`. Restore a portable subset that does not need public Internet or ProgramData. Keep `NetworkHotspotTests.cs` removed. | Inventory is a shipped door with no compiled fixture. |

### Updates (in)

| ID | Item | Why |
| :--- | :--- | :--- |
| PR08-04 | `BillPercentile(NumericSeries, double)` next to `BillP95(NumericSeries)`. | Hole, not a product. |
| PR08-05 | Paper: this folder is the live plan. `001/README.md` points here. Requirements / Design / Guide / package README say Option C on Linux, packed OUI incomplete, no Charts. | v1.6 text still talks like PR05 is “this amendment.” |
| PR08-06 | Version: stay 1.0.0 if only comments + paper. Bump **1.0.1** if PR08-02 lands. | Events are a contract change. |
| PR08-07 | `dotnet test --filter FullyQualifiedName~Network` on the clone. Owner gate. | Same as Json PR07. |

### New features (out of this wave)

| Item | Reason |
| :--- | :--- |
| Duration-per-window on echo campaigns | SRS §6 already marks it later. Needs an addendum, not a sneak. |
| pathping-class | Roadmap later. |
| Scheduler package | Different project. |
| Live IEEE MA-L packed dump | Decision 35. Hosts pass a file. |
| HTTP reachability | HttpIQ. Decision 31. |
| Default `0.0.0.0/0` / `::/0` write | Decision 34. |
| Charts reference | Decision 33. |
| Demo project | Decision 10. |
| Repo Linux CI / un-waive test TFM | Repo work, not this DLL. |
| Live Ubuntu route mutate | Parked PR05. Needs a box and CAP_NET_ADMIN. |
| Restore `NetworkHotspotTests` | Live stack. Not a library gate. |

## Defaults this wave locks

| Setting | Value |
| :--- | :--- |
| Package | `1.0.0` unless PR08-02 ships, then `1.0.1` |
| EVENTID | 14500–14999, step 5, used through 14545 if PR08-02 ships |
| Route write | Option C. Defaults denied. |
| OUI packed | Offline stub |
| Tests filter | `FullyQualifiedName~Network` |
| Commit | `Network PR08: <id short goal>` |

## Close gate

1. PR08-01 through PR08-06 on `main` as scoped (PR08-02 + 1.0.1 together, or PR08-02 cut and version stays 1.0.0).
2. PR08-07 green on the clone.
3. Event IDs stay inside 14500–14999 and count by 5.
4. No packet bytes, credentials, PEM, or `Exception` object in HelperLog writes.
5. No Charts PackageReference. No process spawn.
