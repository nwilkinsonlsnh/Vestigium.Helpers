# Vestigium.Helpers.Network — PR02 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR02  
**Version:** 1.0  
**Status:** Open. Starts after PR01 close.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Scope:** Contract lock + remaining harden. No share campaigns. No Demo chrome. No Linux netlink writer.

---

## 0. Why this PR exists

PR01 stops the unsafe defaults. PR02 makes the written contract match the DLL so PR03 can add a feature without arguing platform rules mid-slice.

The live contradiction: SRS §2.1 says route **mutate** on Linux (typed deny without `CAP_NET_ADMIN`). Code and Guide throw `PlatformNotSupportedException` and never call `ip route`. PR02 **locks the code’s behavior** as v1:

- Windows: print + mutate (IP Helper + persistent HKLM).
- Linux: print required. Mutate is a typed deny. Not `ip`. Not netlink in v1.

Linux mutate, if wanted, is PR04 — a new slice with its own close gates.

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Owner file |
|---|---|---|---|---|---|
| PR02.001 | Lock Windows-write / Linux-print in SRS + Guide + mapping table | Update | P1 | Low | `Requirements_v1.0.md`, `DevelopersGuide_v1.0.md` |
| PR02.002 | Linux mutate throws `NetworkRouteDenied` (not raw PNSE only) | Fix | P1 | Low | `NetworkRouteMutation.cs` |
| PR02.003 | DNS name / label RFC 1035 length guards | Fix | P2 | Medium | `DnsClient.cs` |
| PR02.004 | Campaign JSONL single-writer append | Fix | P2 | Medium | `CampaignJsonl.cs` |
| PR02.005 | Confirm Linux ICMP path; document DGRAM vs raw | Update | P2 | Medium | `IcmpEchoEngine.cs`, Guide |
| PR02.006 | IPv6 route **print** stays; write documented out of v1 | Update | P2 | Low | SRS §2.1, Guide |
| PR02.007 | P95: no samples → no bill (typed) | Fix | P2 | Low | `PercentileBillEngine.cs` |
| PR02.008 | Tests + close | Update | P1 | Medium | `NetworkPR02Tests.cs` |

---

## 2. Build notes

### PR02.001 / PR02.002 — Route contract

SRS amendments in this PR:

| # | New lock |
|---|---|
| 11 | Route **print** both OS. **Mutate Windows only** in v1. Linux mutate → `NetworkRouteDenied` (“Linux route write is not in v1”). Never spawn `route` / `ip`. |
| 11b | IPv6 print is required. IPv6 mutate is not v1. |

Keep `PlatformNotSupportedException` only if the suite already treats it as the typed deny everywhere. Prefer wrapping as `NetworkRouteDenied` so hosts have one catch. Tests assert the type and the message token `Linux` / `not in v1`.

Do **not** implement netlink here.

### PR02.003 — DNS names

Before `EncodeQuery`:

- Each label 1–63 octets.
- Total encoded name ≤ 255 octets including length bytes and the root zero.
- Reject embedded NUL and non-ASCII in the wire encoder (IDNA happens at the host, or we document “ASCII / already-punycode only”).

Parser depth 10 stays.

### PR02.004 — JSONL writer

`CampaignJsonl.Append`:

- Open with `FileShare.Read` and exclusive write, or use the Json helper `AppendJsonl` if FileIo/Json already shipped that door.
- Do not `Read()` the whole file after every window if a cheaper “has terminal window” scan exists; if you keep the full read, take the same lock.
- Two `RunAsync` on the same `ResultsPath` must not interleave half-lines.

Credentials still never appear in this file (echo campaign has none). PR03 inherits the same writer.

### PR02.005 — Linux ICMP

- Keep `System.Net.NetworkInformation.Ping` if it is unprivileged DGRAM on the supported distros.
- If a raw socket is required and fails, existing `ProtocolForbidden` / `PayloadRestricted` path stays.
- Guide: one paragraph — DGRAM first via BCL; no `ping(8)`; payload reject → empty retry.
- No new socket implementation in PR02 unless the BCL path is proven raw-only. If that proof appears, stop and open a note in PR04 rather than growing this PR.

### PR02.006 — IPv6 routes

Print via existing stack tables. Mutate API remains IPv4-only (`PrefixLength` 0–32). Passing an IPv6 destination to `AddRoute` already throws — keep it, add a fixture.

### PR02.007 — P95

`BillP95` / `BillPercentile`:

- Null or empty samples → `InvalidOperationException` after Reject. No `0` bill.
- Analytics `NumericSeries` with `n == 0` same rule.
- Network does not reimplement percentile math.

---

## 3. Tests that close this plan

| Fixture | Covers |
|---|---|
| `PR02_002_linux_add_route_is_network_route_denied` | Skip on Windows |
| `PR02_002_windows_add_route_without_admin_is_denied` | Skip on Linux; no live table change on success path |
| `PR02_003_label_over_63_rejected` | |
| `PR02_003_name_over_255_rejected` | |
| `PR02_004_two_appends_are_whole_lines` | temp file |
| `PR02_006_ipv6_destination_rejected_on_add` | |
| `PR02_007_empty_samples_throw` | |
| `PR02_007_empty_series_throw` | |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR02_
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network
```

---

## 4. Out of PR02

| Item | Goes to |
|---|---|
| Share-transfer campaigns + FileIo probe wiring | PR03 |
| Demo tabs | PR03 |
| Linux netlink mutate | PR04 |
| IPv6 mutate | PR04 |
| Split test TFM / Linux CI | PR04 |
| HTTP client | Never this library |

---

## 5. Commit form

```text
Network PR02: <id short goal>
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. Contract lock after PR01. |
