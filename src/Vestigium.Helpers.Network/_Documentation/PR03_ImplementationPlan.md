# Vestigium.Helpers.Network — PR03 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR03  
**Version:** 1.2  
**Status:** Closed. Share campaigns on `NetworkHelper`. Demo host is out of this package.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`ARCHIVE/PR01/ShareCampaign_v1.5.md`](ARCHIVE/PR01/ShareCampaign_v1.5.md)  
**Scope:** File-share transfer campaigns (Network 13b).

---

## 0. Publish order

FileIo is a Network project reference. Publish FileIo (and Json / Analytics / Hashing) before Network.

---

## 1. Work table

| ID | Item | Status |
|---|---|---|
| PR03.001 | FileIo doors | Closed |
| PR03.002 | Types + FileIo reference | Closed |
| PR03.003 | Default 64 MiB × 4 → P95 → TransferTime | Closed |
| PR03.004 | Advanced planner | Closed |
| PR03.005 | Payload hours + metadata hours | Closed |
| PR03.006 | JSONL + HelperLog, no credentials | Closed |
| PR03.007–009 | Demo tabs | **Skipped — not a library gate** |
| PR03.010 | Tests + close | Closed |

---

## 2. Close gates

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_
```

| Test | Asserts |
|---|---|
| `PR03_001_*` | FileIo Analyze / WriteProbe doors |
| `PR03_002_*` | Types, recipe round-trip, no password field, share-root escape |
| `PR03_003_*` | 1 GiB scales from identical probe P95; efficiency lengthens |
| `PR03_004_*` | Many-tiny+huge includes metadata; large-only skips it |
| `PR03_005_*` | Payload and metadata hours are separate |
| `PR03_006_*` | JSONL has no password keys; path escape |
| `PR03_010_*` | No WriteProbe on NetworkHelper; result splits hours |

Network still does not open `FileStream`. Probe I/O stays on FileIo.

---

## 3. Out of PR03

Demo host. Linux CI matrix. IPv6 route write. `net use`. HTTP client. Scheduler.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. |
| 1.1 | 19 Sep 2026 | 001 confirmed. Demo not a gate. |
| 1.2 | 19 Sep 2026 | 002–006 + 010 closed. 007–009 skipped. |
