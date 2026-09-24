# Vestigium.Helpers.Network — PR07 Backlog

**Document ID:** VEST-HLP-NETWORK-PR07-BL
**Package:** `Vestigium.Helpers.Network` 1.0.0 on nuget.org (bump to **1.0.1** in this wave)
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Network/001 -- Implementation Plan/PR07/`
**Binding:** `Requirements_v1.6.md` wins on conflict. Design, Guide, and the package README follow it.
**Date:** 24 September 2026

## Intent

v1.6 + PR01–PR06 shipped the library and the first public nupkg. PR07 is the first post-ship pass: make the shipped doors tell the truth, persist what Create already accepted, and stop logging a failed trace as Success.

PR07 is not a new protocol kit. It is not TCP/UDP connect jobs, pathping, duration-per-window, a scheduler, HTTP reachability, a plot API, Demo, a live IEEE OUI dump, macOS as a test gate, or repo Linux CI.

## What the library is today

| Field | Value |
| :--- | :--- |
| TFM | `net10.0` (Windows + Linux). Not `net10.0-windows`. |
| Façade | `NetworkHelper` |
| Depends on | Json 1.0.1, Analytics 1.0.1, FileIo 1.1.1, Logging 1.7.1 |
| EVENTID | Reserved 14500–14999, step 5, used through **14525** |
| APPID | `Network` |
| Route write | Option C. Default `0.0.0.0/0` and `::/0` denied. |
| Packed OUI | Offline stub. Incomplete by lock. |

## In this wave

### Fixes

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR07-01 | `IcmpTraceEngine` finish line uses job status, not `NetworkLog.Success` | Failed / timed-out / cancelled traces currently log Success. Echo already has `LogFinished`. Same rule. |
| PR07-02 | Windows IPv6 `Change` / `Remove` log the same Success/deny line as `Add` | Add logs. Change/Remove return quiet. Tuesday-at-2am audit hole. |
| PR07-03 | Windows IPv6 write does not borrow `TryFirstIpv4Index()` | Decision 29: no guessed IfIndex. An IPv4 NIC index on an IPv6 write is the wrong NIC. Require `InterfaceIndex >= 1`, or an up IPv6 interface index. Never invent `1`. |
| PR07-04 | `NetworkCatalog.Register` includes every `HelperLog.Subcategories` value that engines already emit | `Share`, `Stats`, `Progress` are written. Taxonomy does not register them. Host Initialize then drops or mis-tags those lines. |
| PR07-05 | `NetworkHelper` XML comment matches Option C | Comment still says Linux writes throw typed denies. Code calls netlink. Stale comments become next-wave “bugs.” |

### Updates

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR07-06 | Echo campaign recipe persists `IcmpEchoOptions` that `Create` already accepted | `OpenEchoCampaign` today rebuilds `Echo` as `new()`. Timeout, interval, TTL, buffer, Don’t-Fragment, MaxDuration, AllowBurst vanish. Recipe is not a recipe. Missing fields on old files keep today’s defaults. |
| PR07-07 | Named events 14530 / 14535 / 14540 | RouteDenied, IcmpForbidden, CampaignWindowMissed. Same block, step 5. Catalog + `NetworkEvents` + `network.json` stay one source. |
| PR07-08 | Package **1.0.1**. README Depends-on line stays Json 1.0.1 / Analytics 1.0.1 / FileIo 1.1.1 / Logging 1.7.1. | Behavior + catalog change after 1.0.0 is already on nuget.org (24 Sep 2026). Do not republish 1.0.0. |
| PR07-09 | SRS / Design / Guide / package README / this folder’s index match the code. PR06 stays archived. | Paper follows 01–08. |

### Tests / close

| ID | Item |
| :--- | :--- |
| PR07-10 | Tests in `Vestigium.Helpers.Tests` named for the behavior. Filter `FullyQualifiedName~Network`. Owner runs it on the clone. |

## Cut (named so they do not sneak back)

| Item | Why cut |
| :--- | :--- |
| TCP / UDP connect jobs | This DLL already has ICMP Echo, ICMP/UDP trace, and stack tables. New protocol = later addendum. |
| Pathping-class | Roadmap “later.” Not a 1.0.1 fix. |
| Duration-per-window on `EchoWindow` | SRS §6 already parked it. Do not smuggle it in with recipe persist. |
| Scheduler package / cron / systemd | Host lifetime. Locked out. |
| HTTP reachability | Not this package. OUI GET stays the only HTTP. |
| Charting / plot API | Never this library. Not deferred. |
| Demo / Network.Demo | Locked out. |
| Full IEEE MA-L dump | Decision 35. Packed stays a stub. |
| Live Ubuntu / admin-Windows route checks | PR05 §3 parked. Record dated notes when those boxes exist. Not a publish gate. |
| Repo `net10.0` test TFM / ubuntu workflow | Repo CI. Not a Network feature. |
| macOS as a test gate | Best-effort BCL only. |
| Bump past 1.0.1 | No buyer. |

## Defaults this wave locks

| Setting | Value |
| :--- | :--- |
| Package version | `1.0.1` |
| EVENTID used through | 14540 |
| Windows IPv6 IfIndex | Caller `>= 1`, or first **IPv6** up NIC. No IPv4 index reuse. No `1`. |
| Old echo recipes without `echo` object | Open succeeds. `Echo` stays `new IcmpEchoOptions()`. |
| Default route write | Still `NetworkRouteDenied` |
| Plot API | Never |

## Close gate

1. PR07-01 through PR07-09 are on the branch that ships 1.0.1.
2. PR07-10 is `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` on the clone.
3. Event IDs stay inside 14500–14999 and count by 5.
4. No packet bytes, WLAN keys, share passwords, or `Exception` objects in HelperLog writes.
5. No plot API. No process spawn. No default-route write.

PR06 paper lives under `000 -- Archived/001 -- Implementation Plan/PR06/`.
