# Vestigium.Helpers.WinReg — Comparer Design

**Document ID:** VEST-HLP-WINREG-DSN-CMP-000  
**Version:** 1.0  
**Status:** Locked with [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md)  
**Date:** 13 September 2026

---

## 1. Pipeline

```text
Collect (on the box)              Desk
  live key  →  vest-regidx/1      vest-regidx/1  × 2
                                   ↓
                              header check
                                   ↓
                              key-only verify
                                   ↓
                     Unrelated? stop : value merge
                                   ↓
                              vest-regcmp/1
```

Sneakernet is the primary topology. RPC is not required to compare.

---

## 2. Index file (`vest-regidx/1`)

Compare input. Produced by `WriteIndex`.

```text
{"rec":"header","schema":"vest-regidx/1","machine":"BOX1","capturedAt":"...Z","hive":"LocalMachine","path":"SOFTWARE\\Vestigium","view":"Registry64","source":"live"}
{"rec":"key","path":"Client"}
{"rec":"value","path":"Client","name":"InstallPath","type":"String","hash":"…"}
{"rec":"footer","keys":12,"values":40}
```

- `path` is relative to the header root.
- `hash` is SHA-256 of `type-byte || canonical payload`. Not logged.
- No value bytes on these lines.

---

## 3. Compare file (`vest-regcmp/1`)

Desk output. Typed lines, fixed order. JSONL has no sections; `rec` is the section.

```text
{"rec":"header",  "schema":"vest-regcmp/1", "left":{...}, "right":{...}}
{"rec":"verify",  "verdict":"Related", "relatedness":0.94, "delta":0.06, "coverageLeft":0.97, "coverageRight":0.96, "keysLeft":412, "keysRight":408, "keysShared":390, "sampleLeftOnly":["Plugins\\Old"], "sampleRightOnly":["Plugins\\New"]}
{"rec":"delta",   "kind":"Changed", "path":"Client", "name":"InstallPath", "leftType":"String", "rightType":"String", "leftHash":"…", "rightHash":"…"}
{"rec":"footer",  "same":12004, "changed":18, "leftOnly":4, "rightOnly":7, "stopped":false}
```

Rules:

- Header is line 1. Verify is line 2.
- Deltas only after verify, and only if verdict is not Unrelated or `force` is true.
- Same deltas omitted unless `includeSame`.
- Footer always present. Cancelled / Unrelated jobs still get a footer with `stopped: true`.
- Hosts that only need the gate read two lines.

A compare file is **not** a valid index. `WriteIndex` / `Compare` refuse the wrong `schema`.

---

## 4. Verify math

Sets A, B = relative key paths (no values).

```text
Relatedness = |A ∩ B| / |A ∪ B|
Delta       = 1 − Relatedness
CoverageL   = |A ∩ B| / |A|
CoverageR   = |A ∩ B| / |B|
```

Empty union → Unrelated (both indexes have no keys).

Thresholds (overrideable, defaults locked): Related ≥ 0.70; Unrelated < 0.30 unless a coverage ≥ 0.90 → SubsetLeft / SubsetRight; else Weak.

Header hive / path / view mismatch → Unrelated before set math.

---

## 5. Public surface (paper)

```text
RegistryWriteResult WriteIndex(string path, hive, key, view = Default, confirm = false, IProgress<RegistryCompareProgress>? progress = null, CancellationToken cancel = default)

RegistryWriteResult Compare(
    string leftIndex,
    string rightIndex,
    string output,
    bool confirm = false,
    bool force = false,
    bool includeSame = false,
    bool includePayload = false,
    IProgress<RegistryCompareProgress>? progress = null,
    CancellationToken cancel = default)
```

`RegistryCompareProgress`: Phase, KeysSeen, ValuesSeen, CurrentPath, LeftOnly, RightOnly, Changed, Same, Elapsed.

---

## 6. Logging

APPID `WinReg`. Lines may name files, hive, root path, counts, verdict.  
Never hash inputs' raw bytes, never payload, never product keys.
