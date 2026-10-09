# Vestigium.Helpers.Watch.Dns — PR02 Requirements

**Document ID:** VEST-HELPERS-WATCH-DNS-PR02-REQ
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. Binding for PR02 until the implementation plan closes it.
**Date:** 9 October 2026
**Prior:** [PR01 -- Requirements.md](PR01%20--%20Requirements.md) is closed. The exe, the clock, the pipe, event 3008, and the port bind exist.

**One sentence:** One row per name, with a count from the resolver, a count from the port, and a total.

**This version is not** the Event Log. Not a second watcher. Not a claim that the port bind sees outbound queries. Not the DnsIQ tab.

---

## 0. Decision

| Call | Why |
|---|---|
| Both sensors run | PR01 turns the resolver off when the arg is `packet`. A count by source needs both. Default is both. |
| `resolver` and `packet` still narrow | An arg of one name runs that sensor only. The other count stays 0. |
| Event means 3008 | `Microsoft-Windows-DNS-Client` event 3008. The Operational channel stays off. This is not the Event Log. |
| Key is name + type | `edge.example` A and `edge.example` AAAA are two rows. One hundred A queries are one row. |
| Counts are by source | `resolverCount`, `packetCount`, `total`. Total is the sum. It is not a third sensor. |
| Replace, do not append the same name | The pipe still sends lines. A later line for the same name and type replaces the earlier one. The consumer does not stack 100 rows. |
| Port bind is still a bind | UDP/53 and TCP/53 receive queries addressed to this host. They do not see queries this host sends. The unseen line keeps saying so. |

Rejected: flipping one sensor off to run the other as the default. Rejected: enabling `Microsoft-Windows-DNS-Client/Operational`. Rejected: a raw capture in this PR. That is a different sensor. Rejected: collapsing A and AAAA into one row.

---

## 1. What this version is

The same exe. The same clock. A rollup instead of a stream of duplicates.

| Piece | PR01 | PR02 |
|---|---|---|
| Default | Resolver only. | Both. |
| `packet` arg | Port only. Resolver off. | Port only. Resolver count stays 0. |
| `resolver` arg | Resolver. Port off. | Resolver only. Port count stays 0. |
| Row | One line per event. | One line per name and type. Later line replaces. |
| Counts | None. | Resolver, port, total. |

---

## 2. Requirements

### R02-01 Default runs both

No mode arg starts event 3008 and the port bind. `resolver` starts only 3008. `packet` starts only the bind. A bad mode rejects and exits before a sensor.

### R02-02 One hundred queries are one row

The key is the normalized name and the type. A repeat increments the source that saw it. The line sent after the increment carries the new counts. The name is not emitted once per event.

### R02-03 Counts name the source

`resolverCount` is 3008 only. `packetCount` is a question read on the bind only. `total` is the sum. A response on the port does not increment. It stays the PR01 empty-name rejection.

### R02-04 The first line is still the unseen notice

Both-mode text names both holes: the resolver misses raw sockets and non-Windows DoH, and the bind misses queries this host sends plus DoH, DoT, and DoQ. A single-mode run keeps that mode's PR01 sentence.

### R02-05 A failed sensor does not kill the other

If both were requested and the session throws, the failure row is written and the bind keeps running. If both were requested and the bind throws, the failure row is written and 3008 keeps running. If the only requested sensor throws, exit 3 as in PR01.

---

## 3. Must not change

- The clock. 5, step 5, max 180.
- Exit 2 when not elevated. Exit 4 when the pipe cannot open.
- The Operational channel. Do not enable it.
- DnsIQ. This PR does not edit `Vestigium.Suite.Network`.

---

## 4. Done

A new thread builds from the implementation plan. PR02-01 through PR02-04 are the order. DnsIQ PR08 still does not own the sensor.
