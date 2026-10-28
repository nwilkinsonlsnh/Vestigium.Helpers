# Library backlog from the test suite

**Version:** 1.0  
**Date:** 15 September 2026  
**Scope:** Items that belong in a **library**, not in tests. Tests that only assert current behavior stay tests.

Priority: **P0** host hits it now · **P1** correctness / data loss · **P2** API completeness · **P3** polish.

## P0

| ID | Library | Gap | Why it belongs in the library |
|---|---|---|---|
| **L-JNL-01** | WinReg | Journal holds a write handle for the whole `using`. `File.ReadAllText`, Explorer, and copy fail mid-batch. Tests now Dispose first. | Hosts will do the same thing. **Open-append-flush-close per line** in `RegistryJournal.Write`. |
| **L-JNL-02** | WinReg | `Create(protect: false, out result)` was illegal (`out` after optional). | Signature is a public contract. Keep `out` before optionals on every façade. |

## P1

| ID | Library | Gap | Why |
|---|---|---|---|
| **L-JNL-03** | WinReg | `DeleteKey` journal row has no tree snapshot. Rollback skips it. | Tests cannot restore a deleted key. Snapshot Slim/Full before delete (cap size). |
| **L-JNL-04** | WinReg | Restore-from-index without `includePayload` skips values. Easy to think Restore worked. | Result should say `Unsupported` / `skipped=` per key, or refuse Restore unless payload present. |
| **L-JNL-05** | WinReg | Compact while a `RegistryJournal` is open on the same path. | Library should deny compact if the file is the open writer’s path, or close first. |
| **L-SVC-01** | Services | Tree walker count vs cap (129 vs 137). Tests tightened asserts. | Cap must be “nodes visited,” documented, one number. |
| **L-PRC-01** | Processes | Snapshot of a PID that exits mid-walk: `Denied` vs `Gone`. Tests flaked. | One status: `Gone` when `OpenProcess` fails because the PID vanished. |

## P2

| ID | Library | Gap | Why |
|---|---|---|---|
| **L-JNL-06** | WinReg | No `RegistryJournal.ReadText(path)` with `FileShare.ReadWrite`. | Hosts will call `File.ReadAllText`. Give them a helper that cannot fight the writer. |
| **L-JNL-07** | WinReg | Rollback by batch id (not only last batch). | Purge already keys by id; undo should too. |
| **L-REG-01** | WinReg | `RenameValue` is two muts, not one. Rollback order is implicit. | Record a single `RenameValue` op (from/to) so inverse is obvious. |
| **L-KQL-01** | Kql | `==` + wildcard warning is easy to miss if Logging is not wired. | Binder should also return a diagnostic on the parse result. |
| **L-KQL-02** | Kql + Processes | Field aliases (`WindowTitle` vs `PROC.WindowTitle`). Tests failed on unknown field. | Catalog should accept the short name when the pack is Process. |
| **L-PRC-02** | Processes | Campaign `MaxMatches` throws `ArgumentException` not `OutOfRange`. | One exception type for caps. |
| **L-NET-01** | (shared / Network) | Tests referenced `DataUnit.Megabit` that did not exist. | Enum must match what we document, or tests must not invent members — library owns the enum. |

## P3

| ID | Library | Gap | Why |
|---|---|---|---|
| **L-TST-01** | several | Large `*CoverageBoost*` / `*Branch90*` suites poke internals. | Prefer `InternalsVisibleTo` + public façades over tests depending on `internal` types (`RegistryNative`, `RegistryIndexWriter`). |
| **L-LOG-01** | Logging | Warning level was added late; Kql needed it. | Status enum is shared infrastructure — add levels in Logging first. |
| **L-THR-01** | Processes | `ThreadState` vs `System.Threading.ThreadState`. | Keep the enum in a namespace that does not collide, or name it `ProcessThreadState`. |

## Not library work

Coverage-only tests, demo ViewModels, and “assert current message text” stays in Tests / Demo.
