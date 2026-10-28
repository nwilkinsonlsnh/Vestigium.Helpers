# PerfMon family — PR03a implementation plan

**Document ID:** VEST-HLP-PERFMON-PLAN-PR03A
**Version:** 1.0
**Status:** Locked
**Date:** 28 September 2026
**Depends on:** PR02a tools
**Siblings:** [`PR03b`](PR03b%20--%20Implementation%20Plan.md), [`PR03c`](PR03c%20--%20Implementation%20Plan.md)

Regenerating a shard that did not change content must not dirty the tree.

---

## 1. Goal

1. `Filter-PdhCatalog.ps1` writes JSON in a stable shape (same key order, two-space indent, no trailing spaces).
2. `New-PdhCatalogClasses.ps1` writes C# that matches the committed style (no extra blank line, last `Counters` entry has a trailing comma, UTF-8 no BOM).
3. A second generate on the same shard is a no-op in `git diff`.
4. Empty shard (`categoryCount: 0`) is success with a warning, not a throw.

Done when `PR03a_` fixtures regenerate PageFile and `git diff` is empty.

## 2. What this version is not

New category types. Changing short jobs. Committing `tools/PerfMon/dumps/pdh-all.json`.

## 3. Locked rules

| # | Rule |
|---|---|
| 1 | Filter output keys: `source`, `machine`, `utc`, `allowList`, `categoryCount`, `categories`. |
| 2 | Category keys: `category`, `identifier`, `type`, `counters`. No `instances`, no `help`. |
| 3 | Counter keys: `name`, `identifier`. |
| 4 | `allowList` is `*Objects.All` for that probe, even when the dump missed the object. |
| 5 | Missing dump categories are omitted from `categories`, not emitted as empty counter arrays. |
| 6 | Generator skips a category with zero counters. |
| 7 | Generator does not rewrite a `.cs` file whose text would be identical. |

## 4. Steps

| Step | Pri | Work | Exit |
|---|---|---|---|
| **PR03a.001** | P0 | Stable JSON writer in `Filter-PdhCatalog.ps1`. | Re-filter PageFile = no diff. |
| **PR03a.002** | P0 | Stable C# writer in `New-PdhCatalogClasses.ps1`. | Re-generate PageFile = no diff. |
| **PR03a.003** | P0 | Empty-shard path: Gpu warning, exit 0. | `PR03a_003_*`. |
| **PR03a.004** | P1 | Tools README notes “second run must be clean.” | Green. |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR03a_
```

## 5. Still parked

Filling Gpu / Processor Information (PR03b). Remaining Network / Disk objects (PR03c).

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 28 Sep 2026 | Locked. Idempotent generate. |
