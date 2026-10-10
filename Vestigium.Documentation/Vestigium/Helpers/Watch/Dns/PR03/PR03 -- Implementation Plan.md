# Vestigium.Helpers.Watch.Dns — PR03 Implementation Plan

**Document ID:** VEST-HELPERS-WATCH-DNS-PR03-PLAN
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Written.
**Date:** 10 October 2026
**Binding:** [PR03 -- Requirements.md](PR03%20--%20Requirements.md) wins on this cut. PR02 wins on the key, the counts, and the emit. This file wins on order.

**Goal:** The rollup keeps the last Status and Answers from the Event path and the pipe row carries them.

**Not:** A new sensor. Frame IPs. A structured answer list. The DnsIQ tab.

---

## Decision

| Call | Why |
|---|---|
| Keep last non-empty | Port increments must not wipe a previous Event result. |
| No new JSON fields | `WatchRow` already serializes Status and Answers. The tab already has the properties. |
| Caller supplies them | `DnsClientSession` already has the values from `Map`. Pass them into the rollup or set them before the write. |

---

## Implementation table

| Slice | Id | Work | Status |
|---|---|---|---|
| 1 | PR03-01 | `WatchRollup.Add` (or a follow-up setter) stores last non-empty Status and Answers. Port does not clear them. | |
| 2 | PR03-02 | Event path supplies the values. Pipe line contains them. Port-only line does not invent them. | |
| 3 | PR03-03 | Existing tests still pass. One new case: Event then Port keeps the Answers; Port then Event fills them. | |

---

## Slices

### PR03-01

Extend the rollup so the stored `WatchRow` for a key retains the last non-empty Status and Answers. Signature change is internal. A call that only increments the Port count leaves the previous strings. An empty string does not overwrite a prior value.

### PR03-02

`DnsClientSession` (or `ResolverWatch`) passes the status and results into the rollup before the pipe write. Confirm the JSON line includes the fields. A Port-only path still writes a row with empty Status and Answers.

### PR03-03

Add or extend the rollup tests. Event add followed by Port add keeps the Answers. Port add followed by Event add fills them. Counts still sum. Key still Name+Type.

---

## Files this plan expects to touch

```
src/Vestigium.Helpers.Watch.Dns/WatchRollup.cs
src/Vestigium.Helpers.Watch.Dns/DnsClientSession.cs
src/Vestigium.Helpers.Watch.Dns/ResolverWatch.cs          (if the signature moves)
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchRollupTests.cs
```

Do not touch the packet parsers. Do not add source or destination IP fields. Do not edit DnsIQ.

---

## Next action

Land PR03. DnsIQ PR09 can then surface Status and Answers in the details window without a pipe change.
