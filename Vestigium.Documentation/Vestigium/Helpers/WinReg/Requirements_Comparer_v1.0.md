# Vestigium.Helpers.WinReg — Comparer Requirements

**Document ID:** VEST-HLP-WINREG-SRS-CMP-000  
**Version:** 1.0  
**Status:** Locked defaults — 13 September 2026  
**Package:** `Vestigium.Helpers.WinReg` (no new project)  
**Parent SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Design:** [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md)

If this file and the parent SRS disagree on CRUD / export / mount, the parent wins.  
If they disagree on compare / index / verify, **this file wins.**

---

## 0. Purpose

Give a host one way to:

1. Collect a **compare index** on a box (sneakernet primary).
2. Take that index to a desk and compare it to another index.
3. **Verify first** (key paths only) so unrelated trees are not dumped as millions of deltas.
4. Merge values by identity + content hash and emit Left / Right / Changed.
5. Report progress on long jobs.

This is not `fc.exe` on two `.reg` files. Compare is canonical snapshots, not text.

```text
On BOX1:  WriteIndex(BOX1.jsonl, hive, key)
On BOX2:  WriteIndex(BOX2.jsonl, hive, key)
At desk:  Compare(BOX1.jsonl, BOX2.jsonl) → BOX1-vs-BOX2.jsonl
```

---

## 1. Defaults locked (13 September 2026)

| # | Question | Locked |
|---|---|---|
| 1 | Unrelated trees | Stop after verify. Write header + verify + footer. No delta lines. Host may pass `force: true`. |
| 2 | Same values | Count in footer only. `includeSame: true` writes Same delta lines. |
| 3 | Sneakernet currency | Compare input is **index JSONL** (`vest-regidx/1`). `.reg` is a human copy; v1 merge does not parse `.reg`. |
| 4 | Verify thresholds | Related ≥ 0.70. Unrelated < 0.30 unless a coverage ≥ 0.90 (subset). Weak is the band between. Host may override. |
| 5 | Package | `RegistryComparer` / `RegistryHelper.WriteIndex` / `Compare` live in WinReg. |

Deferred: Changed payloads, hive-file source, remote live source, ignore-lists, index resume, Kql over the result.

---

## 2. Constraints

| ID | Constraint |
|---|---|
| C1 | No `reg.exe`. No text-diff of `.reg`. |
| C2 | Value bytes never appear in HelperLog or in index/compare JSONL unless `includePayload: true` on a Changed row and size ≤ 256 bytes. |
| C3 | Index and compare writes need `confirm: true`. |
| C4 | Progress via `IProgress<RegistryCompareProgress>` and `CancellationToken`. Report every N keys or ≤ 200 ms. |
| C5 | Unreachable remote is Denied. The library does not invent a file fallback. |
| C6 | Tests use HKCU test root or committed fixture indexes under test data. Never whole live HKLM in CI. |

---

## 3. Sources (v1)

| Source | v1 |
|---|---|
| Index JSONL file | **Yes** — primary |
| Live local key → WriteIndex | **Yes** |
| `.reg` as merge input | No (export stays; convert later) |
| Hive file | Deferred |
| `For(machine)` live index | Deferred (RPC is optional later) |

---

## 4. Verify (keys only)

Relative key paths under the header root.

```text
Relatedness = |A ∩ B| / |A ∪ B|
Delta       = 1 − Relatedness
CoverageL   = |A ∩ B| / |A|
CoverageR   = |A ∩ B| / |B|
```

| Verdict | When |
|---|---|
| Unrelated | Header hive/path/view mismatch, **or** relatedness < 0.30 and both coverages < 0.90 |
| Related | Headers agree and relatedness ≥ 0.70 |
| SubsetLeft / SubsetRight | Headers agree and one coverage ≥ 0.90 |
| Weak | Headers agree, none of the above |

Unrelated + `force: false` → no value merge.

---

## 5. Value merge

Identity: `Hive + relative path + value name` (default name `""`).  
Equality: same identity and same type and same SHA-256 of canonical bytes.

Kinds: `LeftOnly`, `RightOnly`, `Changed`, `Same` (Same omitted from the file unless asked).

---

## 6. Progress

```text
Phase: OpenLeft | IndexLeft | OpenRight | IndexRight | Verify | Merge | Done
KeysSeen, ValuesSeen, LeftOnly, RightOnly, Changed, Same
CurrentPath, Elapsed
```

---

## 7. Caps

| Knob | Default | Cap (reject, no clamp) |
|---|---|---|
| Values in one index | — | 2,000,000 |
| Depth | 64 | 64 |

---

## 8. Acceptance

1. Two indexes from the same HKCU test tree verify Related and compare footer Same > 0.
2. Two indexes with different roots verify Unrelated, compare file has no `delta` lines, `footer.stopped = true`.
3. Subset tree → Subset* verdict, merge still runs.
4. `includeSame: false` writes no Same deltas; footer still counts them.
5. Progress fires at least once on a tree of 20+ keys.
6. Logs contain paths/names, never the secret payload used in the test.
