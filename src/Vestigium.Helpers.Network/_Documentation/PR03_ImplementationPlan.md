# Vestigium.Helpers.Network — PR03 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR03  
**Version:** 1.1  
**Status:** Open. PR03.001 confirmed FileIo doors. Network does **not** reference FileIo until PR03.002.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network` (+ FileIo doors already specified)  
**Contract:** [`ARCHIVE/PR01/ShareCampaign_v1.5.md`](ARCHIVE/PR01/ShareCampaign_v1.5.md) wins for this slice.  
**Scope:** File-share transfer campaigns (Network 13b). Demo tabs are not a library publish gate.

---

## 0. Why this PR exists

Share campaigns are locked and not on `NetworkHelper`. FileIo owns `AnalyzeDirectory` / `WriteProbe` / `ReadProbe`. Network owns the target, planner, JSONL, and time scale-up.

PR03 is the feature PR. Security rules from PR01 (path confinement, no credentials in JSONL, no cousin CLIs) apply unchanged.

---

## 0.1 Publish order (remember this)

When Network grows a `ProjectReference` to FileIo (PR03.002), a NuGet consumer of Network also needs FileIo on the feed.

| Package | Why Network cares |
|---|---|
| `Vestigium.Logging` | Already |
| `Vestigium.Helpers.Json` | Already a Network reference (campaign JSONL) |
| `Vestigium.Helpers.Analytics` | Already a Network reference (P95) |
| `Vestigium.Helpers.Hashing` | FileIo dependency, not Network directly |
| `Vestigium.Helpers.FileIo` | **Publish before Network** once 002 lands |
| `Vestigium.Helpers.Network` | Last in this chain |

PR03.001 does **not** add the FileIo reference. Confirmation only.

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Owner | Status |
|---|---|---|---|---|---|---|
| PR03.001 | Confirm FileIo `AnalyzeDirectory` / `WriteProbe` / `ReadProbe` match v1.5 §2 | Update | P1 | Medium | FileIo | **Confirmed. No FileIo code change.** |
| PR03.002 | Types: `FileShareTarget`, `ShareProbePlan`, campaign options / result | New | P1 | Medium | Network | Open. This slice adds the FileIo project reference. |
| PR03.003 | Default mode: 64 MiB × 4 write probe → P95 → `TransferTime` | New | P1 | Medium | Network | Open |
| PR03.004 | Advanced planner from `FileIoDirectoryAnalysis` | New | P1 | High | Network | Open |
| PR03.005 | Advanced estimate: payload hours + metadata hours separate | New | P1 | Medium | Network | Open |
| PR03.006 | Campaign JSONL + HelperLog (no credentials, no file bytes) | New | P1 | Medium | Network | Open |
| PR03.007–009 | Demo tabs | New | P2 | Medium | Host / later | Out of Network publish |
| PR03.010 | Tests + close | Update | P1 | High | Tests | Open |

---

## 2. PR03.001 result

FileIo already ships the v1.5 §2 doors:

```
FileIoHelper.AnalyzeDirectory(path, FileIoAnalyzeOptions?)
  → FileIoDirectoryAnalysis
FileIoHelper.WriteProbe(directory, FileIoSize | (value, unit), FileIoProbeOptions?)
  → FileIoProbeResult
FileIoHelper.ReadProbe(path)
  → FileIoProbeResult
```

Close gates already covered by `FileIoAnalyzeTests` and named again as `PR03_001_*`:

- 10 × 1 KiB + 1 × 10 MiB → Tiny 10, TotalBytes exact, source files still on disk.
- Missing path → `DirectoryNotFoundException`.
- WriteProbe 1 MiB → deleted by default, `BytesPerSecond > 0`.

Empty directory is zeros, not a throw. Default probe bytes are random. `KeepProbe` opts out of delete.

Network still must not open `FileStream`.

---

## 3. Tests

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03_001
```

---

## 4. Out of PR03 library work

Demo tabs. Linux CI. IPv6 route write. `net use`. HTTP client.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. Share campaigns + Demo tabs. |
| 1.1 | 19 Sep 2026 | 001 confirmed FileIo doors. Publish-order note. Demo not a Network gate. |
