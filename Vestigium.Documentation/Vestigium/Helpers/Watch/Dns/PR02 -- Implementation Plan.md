# Vestigium.Helpers.Watch.Dns — PR02 Implementation Plan

**Document ID:** VEST-HELPERS-WATCH-DNS-PR02-PLAN
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. Not started.
**Date:** 9 October 2026
**Binding:** [PR02 -- Requirements.md](PR02%20--%20Requirements.md) wins on this cut. PR01 wins on the clock, the pipe, and the elevation exits. This file wins on order.

**Goal:** A required source, and one normalized query name as one row with a count from each source that was asked to run.

**Not:** A builder. The Event Log. A URL key. A raw outbound capture. The pipe name the tab must know. The DnsIQ tab.

---

## Decision

| Call | Why |
|---|---|
| `TryCreate(source, seconds)` | Missing source rejects. `Both` is not inside the type. |
| Emit on change | The tab is open during the watch. A final snapshot hides the count until stop. |
| Pid omitted when mixed | The sources are Event and Port. The caller is not a third source. |

---

## Implementation table

| Slice | Id | Work | Status |
|---|---|---|---|
| 1 | PR02-01 | `TryCreate` requires `Event`, `Port`, or `Both`. Missing source rejects. `Main` passes `Both` only when the arg is absent. | |
| 2 | PR02-02 | Normalize the name. Key is name + type. Scheme or path rejects. | |
| 3 | PR02-03 | Rollup. Increment the source. Emit the replaced line. Omit pid when mixed. | |
| 4 | PR02-04 | Run the requested sensors. A failure row for the one that throws. Exit 3 only if none remain. | |
| 5 | PR02-05 | Unseen line matches the source. `Both` names both holes. | |

---

## Slices

### PR02-01

`WatchRequest.TryCreate(source, seconds, out request, out reject)`. Source is required. Duration rules are unchanged. `Main` maps a missing arg to `Both` and a bad arg to a reject. No sensor opens on a reject. No builder.

### PR02-02

`QueryName.TryNormalize(value, out name, out reject)`. Trim. Drop one trailing dot. Reject a scheme, a path, or an empty label. The test shows `Edge.Example.` and `edge.example` are one key, and `https://edge.example/a` rejects.

### PR02-03

`WatchRollup.Add(name, type, source, pid)` returns the row to send. `resolverCount` and `packetCount` start at 0. `total` is the sum. A non-question port payload does not call `Add`. The second pid for a key clears pid. The test sends 100 Event adds and asserts one key with count 100. No socket. No ETW.

### PR02-04

`Main` starts each sensor the request asked for. A thrown session writes the PR01 failure row and, if the other sensor was requested, does not exit. If it was the only sensor, exit 3. The rollup is the only writer of query rows.

### PR02-05

`Unseen.Line(source)` for `Both` states both holes. `Event` and `Port` keep the PR01 sentences.

---

## Files this plan expects to touch

```
src/Vestigium.Helpers.Watch.Dns/WatchRequest.cs       [NEW]
src/Vestigium.Helpers.Watch.Dns/QueryName.cs          [NEW]
src/Vestigium.Helpers.Watch.Dns/WatchRollup.cs        [NEW]
src/Vestigium.Helpers.Watch.Dns/Program.cs
src/Vestigium.Helpers.Watch.Dns/Unseen.cs
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchRollupTests.cs   [NEW]
```

Do not enable the Operational channel. Do not edit DnsIQ. Do not add a builder.

---

## Next action

PR02-01. Required source. Missing source rejects.
