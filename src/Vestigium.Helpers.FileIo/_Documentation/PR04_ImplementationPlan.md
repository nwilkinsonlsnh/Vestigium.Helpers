# Vestigium.Helpers.FileIo — PR04 implementation plan

**Document ID:** VEST-HLP-FILEIO-PLAN-PR04  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Priority:** P1  
**Depends on:** PR03 closed  
**Package:** `Vestigium.Helpers.FileIo`  
**Parent:** [`ImplementationPlan_v1.2.md`](ImplementationPlan_v1.2.md)

PR04 makes the index API and the exclude masks honest.

---

## Goal

1. `CopyOnlyUniqueContent` has a dest index that `CleanIndex` / `CleanIndexesOlderThan` can actually delete.
2. Analyze and Job use the same wildcard rule.

## Baseline

- SRS §5.5: index under `%ProgramData%\Vestigium\FileIo\Indexes\`, keep by default, explicit clean APIs.
- Code: job builds an in-memory `ConcurrentDictionary` by hashing dest files each run. `FileIoHelper.IndexPath` hashes the dest root and points at `{IndexRoot}/{hash}.jsonl`. **No writer uses that path.**
- `IndexRootOverride` already exists for tests.
- Job `Masked` uses `*` / `?` regex. Analyze `Masked` is `name.Contains(mask.Trim('*'))`.

## Steps

| Step | Priority | Work | Status |
|---|---|---|---|
| **PR04.001** | P0 | When `CopyOnlyUniqueContent` is on, load existing jsonl from `IndexPath(Destination)` if present, then merge dest walk. After a successful copy, append the dest row (relative path, size, mtime, digest). | Open |
| **PR04.002** | P0 | `CleanIndex(destinationRoot)` deletes that jsonl. `CleanIndexesOlderThan` unchanged except it now has files to age. | Open |
| **PR04.003** | P0 | Production root remains `%ProgramData%\Vestigium\FileIo\Indexes\`. Tests set `FileIoHelper.IndexRootOverride` to a temp folder. | Open |
| **PR04.004** | P1 | Extract `FileIoMask.Matches(name, masks)` from Job. Analyze calls it. Document: `*` and `?`, case-insensitive, whole name. | Open |
| **PR04.005** | P1 | Tests: second job with `CopyOnlyUniqueContent` SkipDuplicate without rehashing a dest that only exists in the jsonl (optional: file removed from dest but row kept — decide and document; v1 may require dest file still present). Analyze `*.tmp` does not match `notatmp.txt`. | Open |
| **PR04.006** | P2 | Do not encrypt the index. Do not log digest of file **contents** beyond the hex already allowed. | Open |

## Do not

- Change UniqueName.
- Add LAD.
- Put Charts in FileIo.
- Write indexes during Audit Mode (decisions only; index file unchanged).

## Close gate

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~FileIo
```

A unique-content Copy writes a jsonl under the override root. `CleanIndex` removes it. Analyze and Job agree on `*.tmp`.

Commit: `FileIo PR04: persist dest index and unify masks`.

## Out of PR04

Demo, pack, Hashing NuGet, certainty algorithm rewrite.
