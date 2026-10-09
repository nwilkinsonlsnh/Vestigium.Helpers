# Vestigium.Helpers.Watch.Dns — PR02 Requirements

**Document ID:** VEST-HELPERS-WATCH-DNS-PR02-REQ
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. Binding for PR02 until the implementation plan closes it.
**Date:** 9 October 2026
**Prior:** [Complete/PR01](Complete/PR01/PR01%20--%20Requirements.md) is closed. The exe, the clock, the pipe, event 3008, and the port bind exist.

**One sentence:** The watch does not start without a source, and one query name is one row with a count from each source that was asked to run.

**This version is not** a builder. Not the Event Log. Not a URL rollup. Not a raw outbound capture. Not the pipe name the tab must know. Not the DnsIQ tab.

---

## 0. Decision

| Call | Why |
|---|---|
| Source is required | `Event`, `Port`, or `Both`. `TryCreate` rejects a missing source before a sensor opens. `Both` is a choice the caller makes. It is not a hidden default inside the type. |
| No builder | Three values and a duration. A fluent chain adds nothing. |
| Event means 3008 | `Microsoft-Windows-DNS-Client` event 3008. The Operational channel stays off. This is not the Event Log. |
| Key is the query name and the type | Not a URL. A scheme and a path are not the question. A and AAAA stay two rows. |
| Name is normalized | Trim, drop a trailing dot, compare ignore-case. An empty label rejects and does not become a row. |
| Pid is not the key | Event and Port are the sources. Two processes asking the same name are one row. Pid is omitted when the callers differ. |
| Emit on change | A later line for the same name and type replaces the earlier one. The first line is the unseen notice, and it is not a query row. |
| Status is not a count | The row carries counts. It does not flip status to whatever arrived last. A port payload that is not a question does not increment. |
| Port bind is still a bind | UDP/53 and TCP/53 receive queries addressed to this host. They do not see queries this host sends. |

Rejected: a builder. Rejected: a hidden `Both` inside the type. Rejected: collapsing a URL path into the key. Rejected: stuffing the last pid into the row and calling it the source. Rejected: a final snapshot only. Rejected: enabling the Operational channel. Rejected: a raw capture in this PR.

---

## 1. What this version is

The same exe. The same clock. A required source. A rollup instead of a stream of duplicates.

| Piece | PR01 | PR02 |
|---|---|---|
| Source | `packet` turns the resolver off. No arg is resolver only. | `TryCreate` requires `Event`, `Port`, or `Both`. |
| Row | One line per event. | One line per normalized name and type. Later line replaces. |
| Counts | None. | `resolverCount`, `packetCount`, `total`. A source that was not requested stays 0. |
| Caller | Pid on the event. | Omitted when mixed. Not part of the key. |

---

## 2. Requirements

### R02-01 The watch does not construct without a source

`TryCreate(source, seconds, out watch, out reject)`. Source is `Event`, `Port`, or `Both`. Missing source rejects. A bad duration still rejects. No sensor opens on a reject. `Main` may pass `Both` when the arg is absent. That choice is in the arg parse, not in the type.

### R02-02 The key is a normalized query name and a type

The name is trimmed and the trailing dot is dropped. Compare ignore-case. A scheme or a path is not accepted as a name. A and AAAA are two keys. One hundred `edge.example` A queries are one row.

### R02-03 Counts name the source that was asked to run

`resolverCount` is 3008 only. `packetCount` is a question read on the bind only. `total` is the sum. A source that was not requested stays 0. A response on the port does not increment.

### R02-04 The caller is not the key

Two processes asking the same name increment the same row. Pid is set only when every event for that key came from that pid. Mixed callers omit it. Process name follows the same rule.

### R02-05 Emit on change

The pipe sends a line when a count changes. The consumer replaces on name and type. The unseen notice is the first line and is not a query row. Stop does not wait to flush a second copy.

### R02-06 A failed sensor does not kill the other

If `Both` was requested and one sensor throws, the failure row is written and the other keeps running. If the only requested sensor throws, exit 3 as in PR01.

### R02-07 The unseen line matches the source

`Both` names both holes: the resolver misses raw sockets and non-Windows DoH, and the bind misses queries this host sends plus DoH, DoT, and DoQ. `Event` and `Port` keep the PR01 sentence for that source.

---

## 3. Must not change

- The clock. 5, step 5, max 180.
- Exit 2 when not elevated. Exit 4 when the pipe cannot open.
- The Operational channel. Do not enable it.
- The bind. It is not an outbound capture.
- DnsIQ. This PR does not edit `Vestigium.Suite.Network`. The pipe name the tab must know is PR08.

---

## 4. Done

A new thread builds from the implementation plan. PR02-01 through PR02-05 are the order.
