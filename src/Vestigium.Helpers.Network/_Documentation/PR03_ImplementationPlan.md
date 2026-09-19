# Vestigium.Helpers.Network — PR03 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR03  
**Version:** 1.0  
**Status:** Open. Starts after PR02 close.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network` (+ FileIo doors already specified)  
**Contract:** [`ShareCampaign_v1.5.md`](ShareCampaign_v1.5.md) wins for this slice.  
**Scope:** File-share transfer campaigns (Network 13b) and the Demo tabs that Phase 9–11 never painted.

---

## 0. Why this PR exists

Share campaigns are locked and not on `NetworkHelper`. FileIo is specified to own `AnalyzeDirectory` / `WriteProbe` / `ReadProbe`. Network owns the target, planner, JSONL, and time scale-up. The WPF gallery is still a Probe skeleton; subnet / MAC / bandwidth have no tab.

PR03 is the feature PR. Security rules from PR01 (path confinement, no credentials in JSONL, no cousin CLIs) apply unchanged.

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Owner |
|---|---|---|---|---|---|
| PR03.001 | Confirm FileIo `AnalyzeDirectory` / `WriteProbe` / `ReadProbe` match v1.5 §2 | Update / Fix | P1 | Medium | FileIo |
| PR03.002 | Types: `FileShareTarget`, `ShareProbePlan`, campaign options / result | New | P1 | Medium | Network |
| PR03.003 | Default mode: 64 MiB × 4 write probe → P95 → `TransferTime` | New | P1 | Medium | Network |
| PR03.004 | Advanced planner from `FileIoDirectoryAnalysis` | New | P1 | High | Network |
| PR03.005 | Advanced estimate: payload hours + metadata hours separate | New | P1 | Medium | Network |
| PR03.006 | Campaign JSONL + HelperLog (no credentials, no file bytes) | New | P1 | Medium | Network |
| PR03.007 | Demo: Subnet tab | New | P1 | Medium | Network.Demo |
| PR03.008 | Demo: MAC / Bandwidth tabs | New | P2 | Medium | Network.Demo |
| PR03.009 | Demo: Share campaign tab (Windows paths only) | New | P2 | Medium | Network.Demo |
| PR03.010 | Tests + close | Update | P1 | High | Tests |

---

## 2. Build notes

Parent locks from [`ShareCampaign_v1.5.md`](ShareCampaign_v1.5.md) do not move:

- Network does not copy. FileIo copies / probes.
- No `net use`, no `mount`, no cousin CLIs.
- Credentials never in JSONL (and not on `FileShareTarget` as a password field — host maps the share before the campaign runs).
- Efficiency default 1.0.
- Declared-pipe estimate is a third number, never mixed into measured duration.

### PR03.001 — FileIo doors

If `AnalyzeDirectory` / `WriteProbe` / `ReadProbe` already match §2 close gates, Network only references those types. If a door is missing or the signature drifted, fix FileIo **in this PR** (small, named commits `FileIo PR03: …`) before the Network planner.

Network must not open `FileStream`.

### PR03.002 — Surface

```
NetworkHelper.PlanShareProbe(FileIoDirectoryAnalysis source, ShareProbeOptions?)
    -> ShareProbePlan
NetworkHelper.CreateShareCampaign(ShareCampaignOptions)
NetworkHelper.OpenShareCampaign(string recipePath)
```

`ShareCampaignOptions`:

- `ShareDirectory` (must sit under a host-allowed root; reuse PR01 confinement idea — no `..` escape)
- `PlannedSize` (`BandwidthAmount`)
- `Mode` = Default | Advanced
- `Efficiency` default 1.0
- `MaxProbeBytes` default 256 MiB
- `DeclaredPipeRate` optional
- Recipe / results paths under campaign root

### PR03.003 — Default mode

- Probe: 64 MiB × 4, one stream, write. Read optional.
- Rate = P95 of probe bytes/s via Analytics / existing `BillP95` samples.
- Duration = `TransferTime(plannedSize, p95Rate)` / efficiency.
- Disclaimer string on the result: linear scale from 64 MiB sequential; many-small-file trees need Advanced.

### PR03.004 / PR03.005 — Advanced

Implement the five rules in v1.5 §3–§4 exactly. No sixth bucket. Metadata probe labeled `Workload = ManySmall`. Result exposes `PayloadDuration` and `MetadataDuration` separately when both ran.

### PR03.006 — Logging

- HelperLog subcategory `Share`. Recipe + summary only.
- Stats JSONL kinds: `campaignStart`, `probe`, `windowSummary`, `campaignEnd`.
- No share credentials, no probe file contents, no full path dumps (filename only — PR01.009).

### PR03.007–009 — Demo

Windows gallery only (`net10.0-windows`). Replace the Probe-only skeleton with tabs:

| Tab | Bound to |
|---|---|
| Probe | existing `NetworkHelper.Probe` |
| Subnet | classify / describe / plan / VLSM grid |
| MAC | parse / EUI-64 / link-local. **No live OUI** in the gallery default. |
| Bandwidth | convert / transfer time / website estimate (bots default 0) |
| Share | Default mode against a temp folder. Advanced optional. |

Live OUI stays a host opt-in. Demo does not call `LookupOuiAsync` unless the operator ticks a box that is off by default.

---

## 3. Tests that close this plan

FileIo (if touched):

| Fixture | Covers |
|---|---|
| v1.5 §7 AnalyzeDirectory 10×1 KiB + 1×10 MiB | no copy |
| v1.5 §7 WriteProbe 1 MiB temp deleted | |

Network:

| Fixture | Covers |
|---|---|
| `PR03_003_default_scales_1_gib_from_identical_probes` | duration = size / P95 |
| `PR03_004_many_tiny_plus_huge_includes_metadata_and_huge` | planner |
| `PR03_004_only_large_skips_metadata` | planner |
| `PR03_005_estimate_splits_payload_and_metadata` | |
| `PR03_006_jsonl_has_no_password_keys` | |
| `PR03_006_share_path_escape_rejected` | PR01 rule reused |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Share
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~AnalyzeDirectory
```

Demo is not an xUnit gate. Manual: F5 Network.Demo, four calculator tabs open, Share Default against `%TEMP%\VestigiumShareProbe`.

---

## 4. Out of PR03

| Item | Goes to |
|---|---|
| Linux CI / test TFM split | PR04 |
| Linux route netlink writer | PR04 |
| IPv6 route mutate | PR04 |
| Packed offline OUI snapshot as default | later / PR04 optional |
| HTTP reachability | HttpIQ |
| `net use` / credential store | Never |

---

## 5. Commit form

```text
Network PR03: <id short goal>
FileIo PR03: <id short goal>
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. Share campaigns + Demo tabs. |
