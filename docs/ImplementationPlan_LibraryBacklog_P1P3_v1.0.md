# Library backlog implementation plan (P1–P3)

**Document ID:** VEST-HLP-PLAN-BL-P1P3-000  
**Version:** 1.0  
**Status:** Paper  
**Date:** 15 September 2026  
**Source:** `docs/Backlog_LibraryFromTests_v1.0.md`

P0 (**L-JNL-01** per-line journal write, **L-JNL-02** `out` order) is **not** in this plan. Do it first if the host still holds a journal open during `ReadAllText`.

## Scoreboard

| Phase | Library | IDs | Status |
|---|---|---|---|
| **B0 Paper** | — | this file | **Ready** |
| **B1 WinReg journal data** | WinReg | L-JNL-03, L-JNL-04, L-JNL-05 | Planned |
| **B2 WinReg journal API** | WinReg | L-JNL-06, L-JNL-07, L-REG-01 | Planned |
| **B3 Processes** | Processes | L-PRC-01, L-PRC-02, L-THR-01 | Planned |
| **B4 Services** | Services | L-SVC-01 | Planned |
| **B5 Kql** | Kql (+ Processes catalog) | L-KQL-01, L-KQL-02 | Planned |
| **B6 Shared / hygiene** | Network, Logging, Tests | L-NET-01, L-LOG-01, L-TST-01 | Planned |

---

## B1 — WinReg journal data (P1)

| ID | Work |
|---|---|
| **L-JNL-03** | Before `DeleteKey`, if `journal` is set, snapshot the key (`Full`, depth cap 32 / 256 keys). Store as `beforeTree` JSON (paths + values) or omit + `beforeOmitted` if over cap. Rollback restores that tree then stops skipping `DeleteKey`. |
| **L-JNL-04** | `Restore` of `vest-regidx/1` without payloads: if every value row lacks `text`, return `Unsupported` with reason `index has no payloads`. If some have text, keep `applied=` / `skipped=` and add `partial=true`. |
| **L-JNL-05** | Track the open path on `RegistryJournal`. `Compact` / `Purge` on that path → `InUse` until Dispose. |

Tests: `RegistryBacklogB1*`.

---

## B2 — WinReg journal API (P2)

| ID | Work |
|---|---|
| **L-JNL-06** | `RegistryJournal.ReadText(path)` / `RegistryHelper.ReadJournalText(path)` — `FileShare.ReadWrite`. |
| **L-JNL-07** | `Rollback(path, confirm, batchId: id)` undoes that batch; default remains last batch. |
| **L-REG-01** | `RenameValue` writes one mut `op=RenameValue` with `from`/`to` + value payload. Inverse: delete `to`, restore `from`. Stop relying on Set+Delete pair for undo. |

Tests: `RegistryBacklogB2*`.

---

## B3 — Processes (P1/P2/P3)

| ID | Work |
|---|---|
| **L-PRC-01** | When `OpenProcess` / query fails and `GetProcessById` throws `ArgumentException` or PID is gone, field/process status is `Gone`, not `Denied`. |
| **L-PRC-02** | Caps (`MaxMatches`, recipe bounds) throw `ArgumentOutOfRangeException` only. Align tests. |
| **L-THR-01** | Rename public enum to `ProcessThreadState` (keep obsolete alias `ThreadState` for one release if already shipped). |

Tests: existing `ProcessCoverageBoost` / campaign tests retargeted.

---

## B4 — Services (P1)

| ID | Work |
|---|---|
| **L-SVC-01** | Document and implement `ServiceTreeWalker` cap as **nodes visited** (unique service names seen). One constant, used in both directions. Tests assert `<= cap` or exact visited count from that definition. |

Tests: `ServicePhase3` retarget.

---

## B5 — Kql (P2)

| ID | Work |
|---|---|
| **L-KQL-01** | `KqlCompileResult.Diagnostics` includes `==` + wildcard warning even when Logging is off. |
| **L-KQL-02** | Process pack: bind `WindowTitle`, `CommandLine`, `Name`, … as aliases of `PROC.*`. Unknown-field errors list aliases. |

Tests: existing `ProcessKqlTests` + binder tests.

---

## B6 — Shared / hygiene (P2/P3)

| ID | Work |
|---|---|
| **L-NET-01** | `DataUnit` public members = documented set. Add `Megabit` only if the product wants it; otherwise tests use existing members. |
| **L-LOG-01** | Audit `VestigiumStatus` for Warning/Error/Success used by Helpers. Any new level lands in Logging first. |
| **L-TST-01** | `InternalsVisibleTo` for Tests on WinReg / Processes / Services / Kql. Tests stop calling non-visible types (`RegistryNative`, `RegistryIndexWriter`) or those types stay `internal` and tests use façades only. |

---

## Gates

- No demo work.
- Confirm still required on writes / purge / rollback.
- HelperLog still never prints value payloads or SDDL.
- HKCU sandbox for registry writes.

## Suggested order

B1 → B2 (WinReg stays hot) → B3 → B4 → B5 → B6.
