# Vestigium.Helpers.Watch.Dns — PR03 Requirements

**Document ID:** VEST-HELPERS-WATCH-DNS-PR03-REQ
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Written. Binding for PR03 until the implementation plan closes it.
**Date:** 10 October 2026
**Prior:** [PR02 -- Requirements.md](../Complete/PR02/PR02%20--%20Requirements.md) is closed. The rollup key, the counts, and the emit-on-change exist. Status and Answers are mapped then discarded.

**One sentence:** The rollup keeps the last Status and Answers from the Event path so the pipe row the tab already reads actually contains them.

**This version is not** a new sensor. Not the Operational channel. Not a packet decoder that extracts source or destination IPs from the frame. Not a whois or HTTP client. Not a change to the Name+Type key. Not the DnsIQ tab.

---

## 0. Decision

| Call | Why |
|---|---|
| Preserve what Map already has | `ResolverWatch.Map` receives `QueryStatus` and `QueryResults`. `WatchRollup.Add` rebuilds the row with empty strings. The cheapest fix is to keep the last non-empty values on the key. |
| Answers is the destination | The tool is local. Source IP is this machine. The useful destination is the data the resolver returned. That lives in Answers. |
| No frame IP | `OutboundFrame` already peels the UDP payload. Extracting the remote address of the DNS server adds a field the tab does not need for this release. |
| Key stays Name+Type | PR02 locked it. A and AAAA remain two rows. Status and Answers do not join the key. |
| Last wins | A later Event for the same key replaces Status and Answers. A Port-only increment leaves the previous values. Empty stays empty. |
| Client parses if it wants | The pipe carries the string. DnsIQ may split A/AAAA out of it. This exe does not add a structured list unless the string is already simple. |

Rejected: adding source IP. Rejected: changing the rollup key. Rejected: enabling the Operational channel. Rejected: a second pipe format. Rejected: automatic resolution inside the watch.

---

## 1. What this version is

The same exe. The same clock. The same key. The same counts. The row now keeps the payload the Event path already saw.

| Piece | PR02 | PR03 |
|---|---|---|
| Status | Discarded | Last non-empty from Event. Blank if only Port. |
| Answers | Discarded | Last non-empty from Event (`QueryResults`). Blank if only Port. |
| Key | Name + Type | Unchanged. |
| Counts | Resolver / Packet / Total | Unchanged. |
| Emit | On change | On change. The new fields travel with the row. |

---

## 2. Requirements

### R03-01 The rollup keeps Status and Answers

`Add` accepts the optional status and answers (or the caller sets them on the returned row before the pipe write). The stored row for that key holds the last non-empty values. A Port increment does not clear them. An empty Event does not wipe a previous value.

### R03-02 The pipe row carries them

`WatchRow` already has the properties. The JSON line the tab deserializes includes `status` and `answers` when present. No new fields required for this cut. No version bump on the pipe.

### R03-03 Port-only rows stay blank

A question seen only on the bind or the outbound capture has empty Status and Answers. That is correct. The Event path is the one that knows the result.

### R03-04 Key and counts are untouched

Normalization, Name+Type key, resolverCount, packetCount, total, pid-omission rule, and emit-on-change stay exactly as PR02 left them. A failure row is still a failure row.

---

## 3. Must not change

- The clock. 5 / step 5 / max 180.
- Exit 2, exit 3, exit 4.
- The Operational channel.
- The bind and the outbound raw sockets.
- The Name+Type key.
- DnsIQ. This PR does not edit `Vestigium.Suite.Network`.

---

## 4. Done

A thread follows the implementation plan. After PR03 the pipe line for an Event-sourced key contains the last Status and Answers. A Port-only key does not invent them. DnsIQ PR09 can consume them without a format change.

---

## Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 10 Oct 2026 | Preserve Status and Answers on the rollup so the pipe carries the destination data the Event path already saw. |
