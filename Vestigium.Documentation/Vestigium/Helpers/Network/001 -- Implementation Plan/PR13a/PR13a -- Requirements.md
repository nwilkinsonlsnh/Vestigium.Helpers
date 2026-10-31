# Vestigium.Helpers.Network — PR13a Requirements

**Document ID:** VEST-HLP-NETWORK-PR13A-REQ  
**Status:** Closed  
**Date:** 31 October 2026  
**Package:** `Vestigium.Helpers.Network` **2.1.0**.  
**Binding:** `Requirements_v1.6.md` wins until this PR amends it in the same change that lands the door.  
**Companion:** [`PR13a -- Implementation Plan.md`](PR13a%20--%20Implementation%20Plan.md)

One-sentence goal: give a host a flat open-port row for a grid, and keep CSV as a writer over that list.

CSV is a format. The row is the model. A host should not parse text the library just built from objects.

Closed. Shipped as package **2.1.0**. Owner gate passed.

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

## Acceptance

Owner gate passed. Package **2.1.0** listed. Decisions 84–88 are in the SRS.
