# Vestigium.Helpers.WinReg — Journal Purge Requirements

**Document ID:** VEST-HLP-WINREG-SRS-PURGE-000  
**Version:** 1.0  
**Status:** Paper  
**Date:** 15 September 2026

## Problem

`PurgeJournal` only deletes the whole file. Hosts need retention: keep the last few applies, drop old batches, drop batches already rolled back, or drop one batch by id — without corrupting Rollback.

## Decisions

| Item | Decision |
|---|---|
| Unit | **Batch**, never a lone `mut`. |
| File | Rewrite to `path.tmp`, then replace. Never edit JSONL in place. |
| Header | Always kept. `protect` stays what it was. |
| `mut-undo` | Kept only when the target batch survives. |
| Confirm | Every purge/compact requires `confirm`. |
| Combine keep-last + older-than | **AND**: a batch survives only if it is in the last N **and** younger than X when both are set. |
| Default `PurgeJournal(path)` | Still means **all** (delete file). |
| Auto-purge on Apply | Out. Host calls Purge. |
| Dry run | In. No write. Counts only. |

## Modes

| Mode | Keep |
|---|---|
| All | Nothing (delete file) |
| Keep last N batches | Newest N **committed** batches (open/aborted/failed optional drop) |
| Older than X | Batches with `startedAt` ≥ now − X |
| Undone only | Batches that are **not** fully undone |
| By batch id | Everything except that id |
| Archive | Optional copy of the **removed** batches to `archivePath` before rewrite |

Committed means `batch-end` status `committed`. Open batches at end of file are kept (still in play).

## Result

`RegistryPurgeResult`: status, path, batchesKept, batchesRemoved, mutsKept, bytesBefore, bytesAfter, dryRun.

Logs: path, mode, counts. Never payloads.
