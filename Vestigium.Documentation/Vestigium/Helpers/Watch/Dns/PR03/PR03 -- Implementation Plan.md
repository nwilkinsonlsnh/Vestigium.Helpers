# Vestigium.Helpers.Watch.Dns — PR03 Implementation Plan

**Document ID:** VEST-HELPERS-WATCH-DNS-PR03-PLAN
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Written. PR03-01 and PR03-02 done.
**Date:** 10 October 2026
**Binding:** [PR03 -- Requirements.md](PR03%20--%20Requirements.md) wins on this cut. PR02 wins on the key, the counts, and the emit. This file wins on order.

**Goal:** Every emitted line carries the current Status and Answers so the client can keep the sequence of responses.

**Not:** An accumulator inside the helper. Frame IPs. A structured answer list. The DnsIQ tab.

---

## Decision

| Call | Why |
|---|---|
| Emit the payload | The stream is the record. The client keeps every line. |
| Latest on the key | So a Port increment does not clear a prior Event result. |
| No history list | Duplicates work the client must do for the details pane. |

---

## Implementation table

| Slice | Id | Work | Status |
|---|---|---|---|
| 1 | PR03-01 | Rollup stores latest non-empty Status and Answers. Port does not clear them. | Done. |
| 2 | PR03-02 | Event path supplies the values. Every emitted line contains them when known. | Done. |
| 3 | PR03-03 | Tests: Event then Port keeps Answers; Port then Event fills them. Counts still sum. | Done. |

---

## Slices

### PR03-01

Done. Rollup keeps latest non-empty Status and Answers. Port increment leaves them. Empty does not wipe.

### PR03-02

Done. DnsClientSession passes Status and Answers into Add. WatchPipe camelCase JSON includes status and answers. PacketWatch calls Add with no payload; Port-only does not invent them.

### PR03-03

Done. Tests cover Event-then-Port, Port-then-Event, and empty-does-not-wipe. Key and counts unchanged.

---

## Files this plan expects to touch

```
src/Vestigium.Helpers.Watch.Dns/WatchRollup.cs
src/Vestigium.Helpers.Watch.Dns/DnsClientSession.cs
src/Vestigium.Helpers.Watch.Dns/ResolverWatch.cs          (if the signature moves)
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchRollupTests.cs
```

Do not touch the packet parsers. Do not add a history list. Do not add source or destination IP fields. Do not edit DnsIQ.

---

## Next action

None in this repo. DnsIQ PR09 keeps the lines and shows the sequence in details.
