# Vestigium.Helpers.Watch.Dns — PR02 Implementation Plan

**Document ID:** VEST-HELPERS-WATCH-DNS-PR02-PLAN
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. Not started.
**Date:** 9 October 2026
**Binding:** [PR02 -- Requirements.md](PR02%20--%20Requirements.md) wins on this cut. PR01 wins on the clock, the pipe, and the elevation exits. This file wins on order.

**Goal:** Roll repeated names into one row with a resolver count, a port count, and a total. Default runs both sensors.

**Not:** The Event Log. A raw outbound capture. The DnsIQ tab.

---

## Decision

| Call | Why |
|---|---|
| Rollup in the exe | The pipe stays lines. The consumer replaces on name + type. |
| Both is the default | A source count is a lie if the other sensor was switched off. |
| One failure does not stop the other | Only the requested set is required. |

---

## Implementation table

| Slice | Id | Work | Status |
|---|---|---|---|
| 1 | PR02-01 | Mode. Default both. `resolver` or `packet` narrows. Bad mode exits before a sensor. | |
| 2 | PR02-02 | Rollup. Key is name + type. Increment the source. Send the replaced line. | |
| 3 | PR02-03 | Run the requested sensors together. A failure row for the one that throws. Exit 3 only if none remain. | |
| 4 | PR02-04 | Unseen line for both. Single-mode text unchanged. | |

---

## Slices

### PR02-01

`WatchMode.Parse(args)`. No mode arg is `both`. `resolver` and `packet` are the narrowings. Anything else rejects. `Main` does not start a sensor on a reject.

### PR02-02

`WatchRollup.Add(name, type, source)` returns the row to send. Key is ordinal ignore-case name and type. `resolverCount` and `packetCount` start at 0. `total` is the sum. A non-question port payload does not call `Add`. The test sends 100 resolver adds and asserts one key with count 100. No socket. No ETW.

### PR02-03

`Main` starts each requested sensor. A thrown session writes the PR01 failure row and, if the other sensor is running, does not exit. If it was the only sensor, exit 3. The rollup is the only writer of query rows.

### PR02-04

`Unseen.Line` for `both` states both holes. Resolver-only and packet-only keep the PR01 sentences.

---

## Files this plan expects to touch

```
src/Vestigium.Helpers.Watch.Dns/WatchMode.cs          [NEW]
src/Vestigium.Helpers.Watch.Dns/WatchRollup.cs        [NEW]
src/Vestigium.Helpers.Watch.Dns/Program.cs
src/Vestigium.Helpers.Watch.Dns/Unseen.cs
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchRollupTests.cs   [NEW]
```

Do not enable the Operational channel. Do not edit DnsIQ.

---

## Next action

PR02-01. Mode parse. Default is both.
