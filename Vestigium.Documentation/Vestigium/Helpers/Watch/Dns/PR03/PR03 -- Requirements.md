# Vestigium.Helpers.Watch.Dns — PR03 Requirements

**Document ID:** VEST-HELPERS-WATCH-DNS-PR03-REQ
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Written. Binding for PR03 until the implementation plan closes it.
**Date:** 10 October 2026
**Prior:** [Complete/PR02](../Complete/PR02/PR02%20--%20Requirements.md) is closed. The rollup key, the counts, and the emit-on-change exist. Status and Answers are mapped then discarded.

**One sentence:** The rollup puts the current Status and Answers on every emitted line so the client can keep the full sequence of responses for the watch window.

**This version is not** a new sensor. Not the Operational channel. Not a packet decoder. Not an accumulator of history inside the helper. Not a whois client. Not a change to the Name+Type key. Not the DnsIQ tab.

---

## 0. Decision

| Call | Why |
|---|---|
| Emit on every change | The pipe already sends a line when a count changes. Put Status and Answers on that line. The client receives the sequence. The helper does not need to store the past. |
| Last on the key is fine | The stored row holds the latest so the next emit is correct. History lives in the stream the client already reads. |
| Do not accumulate inside the helper | A list of every past answer on the row bloats the pipe and duplicates work the client must do anyway for the details pane. |
| Answers is the destination | The tool is local. Source IP is this machine. The useful destination is the data the resolver returned. |
| Key stays Name+Type | PR02 locked it. Status and Answers do not join the key. |
| Port-only stays blank | A Port increment does not invent Status or Answers. It leaves the previous values so the next Event can update them. |

Rejected: storing a history list in the helper. Rejected: last-only with no emit of the payload. Rejected: adding source IP. Rejected: changing the rollup key. Rejected: enabling the Operational channel.

---

## 1. What this version is

The same exe. The same clock. The same key. The same counts. Every emitted line now carries the Status and Answers that were current when the count changed.

| Piece | PR02 | PR03 |
|---|---|---|
| Status | Discarded | On the emitted line. Latest kept on the key. |
| Answers | Discarded | On the emitted line. Latest kept on the key. |
| History | None | In the stream. Client keeps it. |
| Key | Name + Type | Unchanged. |
| Counts | Resolver / Packet / Total | Unchanged. |

---

## 2. Requirements

### R03-01 Every emitted line carries Status and Answers

When the Event path supplies them, the JSON line includes `status` and `answers`. The rollup stores the latest non-empty so a later Port increment does not clear them and a later Event can replace them. Empty does not wipe a prior value.

### R03-02 The helper does not accumulate history

No list of past answers on the row. No second channel. The emit-on-change stream is the record. DnsIQ PR09 keeps the lines.

### R03-03 Port-only lines stay blank

A question seen only on the bind or the outbound capture has empty Status and Answers unless a prior Event for that key already set them. The Event path is the one that knows the result.

### R03-04 Key and counts are untouched

Normalization, Name+Type key, counts, pid-omission, and emit-on-change stay as PR02 left them.

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

After PR03 every Event-sourced line on the pipe contains Status and Answers. The client can keep the sequence. A Port-only line does not invent them. DnsIQ PR09 consumes the stream without a format change.

---

## Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 10 Oct 2026 | Emit Status and Answers on every line. History stays in the stream. Helper does not accumulate. |
