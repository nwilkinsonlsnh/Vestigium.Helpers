# Vestigium.Helpers.Network — PR07 Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR07
**Status:** Open (paper)
**Date:** 24 September 2026
**Backlog:** [`PR07 -- Backlog.md`](PR07%20--%20Backlog.md)
**Binding:** Requirements win. Commit: `Network PR07: <id short goal>`.
**Package:** 1.0.0 on nuget.org → **1.0.1** in PR07-08.

This is not TCP connect, pathping, duration-per-window, Charts, Demo, Ubuntu CI, or a new OUI dump.

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR07-01 | Trace finish uses `LogFinished` (or the same switch Echo uses). Failed/TimedOut/Cancelled do not write Success. | On branch |
| 2 | PR07-02 | Windows IPv6 Change/Remove write the same Success line Add already writes. Deny path unchanged. | On branch |
| 3 | PR07-03 | IPv6 IfIndex: caller `>= 1` or first up IPv6 NIC. Stop calling `TryFirstIpv4Index()` from the v6 write path. | On branch |
| 4 | PR07-04 | `NetworkCatalog.Register` taxonomy includes `Share`, `Stats`, `Progress`. Keep existing names. | On branch |
| 5 | PR07-05 | Fix `NetworkHelper` summary comment to Option C (Linux netlink, typed deny only on cap/ACL/default). | On branch |
| 6 | PR07-07 | Add `RouteDenied` 14530, `IcmpForbidden` 14535, `CampaignWindowMissed` 14540 to `NetworkEvents`, `network.json`, and `Rows`. Wire Reject/forbidden/missed lines to those IDs. | On branch |
| 7 | PR07-06 | Campaign recipe DTO grows an `echo` object. Write on Create. Read on Open. Absent object = current defaults. | On branch |
| 8 | PR07-08 | csproj + package README Version `1.0.1`. Sibling pins stay Json 1.0.1 / Analytics 1.0.1 / FileIo 1.1.1. Logging stays `$(VestigiumLoggingVersion)`. | Open |
| 9 | PR07-10 | Tests: trace status log; v6 index reject; taxonomy contains Share/Stats/Progress; recipe round-trip Echo; old recipe without echo still opens; 14530–14540 exist; still no Charts. | Open |
| 10 | PR07-09 | SRS 1.6 amendment note, Design §2 IfIndex, Guide consume 1.0.1, Implementation Plan README points here. | Open |

PR07-10 (`dotnet test --filter FullyQualifiedName~Network`) is the owner gate on the clone.

## Slice notes

### PR07-01 — Trace log

`IcmpEchoEngine.LogFinished` already maps status → Success / Warning / Failed. Trace currently always calls `NetworkLog.Success`. Reuse the mapper. Do not invent a second policy.

### PR07-03 — IPv6 IfIndex

Wrong mechanism today: `NetworkRouteMutation.Add/Change/Remove` for `spec.IsIPv6` fills a missing index from `TryFirstIpv4Index()`. That is an IPv4 table index.

Right mechanism: `GetIPProperties().GetIPv6Properties().Index` on an up, non-loopback NIC, or the caller’s `InterfaceIndex >= 1`. If neither exists → Reject + `ArgumentException`, same text family as the IPv4 “interface required” path.

Linux already requires `InterfaceIndex`. Do not weaken that.

### PR07-06 — Recipe echo object

Shape (names locked here):

```json
"echo": {
  "timeoutMs": 1000,
  "intervalMs": 1000,
  "bufferSize": 32,
  "ttl": 128,
  "dontFragment": false,
  "maxDurationMs": null,
  "allowBurst": false
}
```

`Count` stays on the window, not on `echo`. Window count already overrides `Echo.Count` at run.

Unknown extra JSON properties: ignore (Json defaults). No schema version field in this wave.

### PR07-07 — Event IDs

| ID | Name | When |
| ---: | :--- | :--- |
| 14530 | RouteDenied | `NetworkRouteDenied` path (cap / ACL / default route) |
| 14535 | IcmpForbidden | Echo/trace `ProtocolForbidden` |
| 14540 | CampaignWindowMissed | `windowMissed` JSONL line |

Still count by 5. Still no payload bytes in the message.

### PR07-08 — Pack

Do not push from this agent. Owner packs 1.0.1 after 01–07 + tests. Inspect nuspec: no Charts, no `.csproj` deps.

## What this PR does not do

See Backlog “Cut”. Live Ubuntu route checks stay on archived PR05 §3.

## Document control

| Version | Date | Change |
| :--- | :--- | :--- |
| 1.0 | 24 Sep 2026 | Open. First wave after nuget.org 1.0.0. |
