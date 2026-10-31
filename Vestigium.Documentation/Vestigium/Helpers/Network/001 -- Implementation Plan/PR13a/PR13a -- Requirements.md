# Vestigium.Helpers.Network — PR13a Requirements

**Document ID:** VEST-HLP-NETWORK-PR13A-REQ  
**Status:** Open  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` next after **2.0.0**. This paper does not set the version number.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the door.  
**Companion:** [`PR13a -- Implementation Plan.md`](PR13a%20--%20Implementation%20Plan.md)

One-sentence goal: give a host a flat open-port row for a grid, and keep CSV as a writer over that list.

CSV is a format. The row is the model. A host should not parse text the library just built from objects.

---

## Decisions locked for PR13a

| # | Decision | Locked as |
|---|---|---|
| 84 | Flat open row | `PortScanOpenRow` is host, port, transport, service, MSS, banner, elapsed. |
| 85 | Rows is the door | `PortScanCsv.Rows` returns `IReadOnlyList<PortScanOpenRow>`. Pure. No I/O. |
| 86 | CSV stays | `PortScanCsv.Write` stays. It writes the same rows. Banner stays quoted. No file write. |
| 87 | Nested rows stay | `PortScanHostRow` is unchanged. It answers "what is open on this box." |
| 88 | Open ports only | Rows come from open TCP and open UDP. Closed and timed-out are not in the list. |

---

## What the caller can ask after PR13a

| Question | Answer | Source |
|---|---|---|
| Bind a grid of open ports | Flat rows | `PortScanCsv.Rows` |
| Paste a table | CSV text | `PortScanCsv.Write` |
| What is open on this one host | Nested lists | `PortScanResult.Hosts` |

---

## Out of this release

| Item | Why not |
|---|---|
| File write | The host writes the file. |
| Closed or timed-out rows | The grid is open ports. `Hits` still has the rest. |
| Replacing `PortScanHostRow` | Different question. |
| SYN, CIDR inside `ScanPorts`, TLS | Unchanged locks. |

---

## Defaults this release locks

| Setting | Value |
|---|---|
| Row source | Open TCP then open UDP, host order as in `Hosts`. |
| CSV | Same columns as today. Banner quoted. |
| Commit | `Network PR13a: <id short goal>` |

---

## Acceptance

1. `PortScanOpenRow` exists.
2. `Rows` returns one row per open port and does no I/O.
3. `Write` still returns CSV and matches the rows.
4. Nested host rows are unchanged.
5. SRS, Design, Guide, and package README amended in the same change that lands the door.
