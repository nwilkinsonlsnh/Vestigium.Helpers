# Vestigium.Helpers.Json — Design

**Document ID:** VEST-HLP-JSON-DSN-000  
**Version:** 1.0  
**Status:** Locked companion to SRS v1.0  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records *why* Json is shaped this way. It does not add requirements.

---

## 1. Intent

Give a host one way to read and edit **payload** JSON / JSONL: settings, exports, external logs. `System.Text.Json` only. An edit session so a developer can see the RFC 6902 patch before disk changes.

```
ToJson / FromJson / Parse     one-shot document (string, Stream, ReadOnlySpan<byte>)
Create / Open / OpenJsonl     JsonSession
    Snapshot → Set / AppendRecord → Diff → Commit → Save
Compare                       two payload files → JsonPatch (Diff only)
Logging writes audit JSONL. This library does not.
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| RFC 8259 only | No comments, trailing commas, or BOM. Fail closed on junk. |
| `System.Text.Json` only | One engine. No Newtonsoft in this package. |
| Pretty + camelCase for `.json` | Humans edit settings. JSONL Save stays compact so the line grammar holds. |
| Session owns working vs committed | Disk never sees uncommitted Sets. Diff is the review surface. |
| Collision default Fail | UniqueName belongs in FileIo. |
| Atomic write default | Sibling temp then `File.Move` overwrite of the opened path. |
| Open vs OpenJsonl | A single RFC 8259 document is not a line file. Do not guess. |
| Paths accept pointer and dotted | `/a/b` and `a.b` are the same member. Get missing throws `KeyNotFoundException`; TryGet is false. |
| Sparse HelperLog | Paths and counts. Never bodies or field values. Get is quiet. |
| Event IDs per subcategory | Block 13500–13999, step 5, used through 13625. |
| Never `Initialize` | Folder follows the host APPID. |
| Probe in memory | Must not write Desktop exports. |

---

## 3. Shape

| File | Role |
|---|---|
| `JsonHelper.cs` | Identity, Probe, ToJson/FromJson/Parse, Create/Open/OpenJsonl, WriteFile, Compare, paths |
| `JsonSession.cs` | Working / committed / snapshot, Get/Set, Diff, Commit, Save |
| `JsonPath.cs` | Pointer and dotted path |
| `JsonPatch.cs` | RFC 6902 Diff. Public `Compare(JsonNode?, JsonNode?)`. |
| `JsonCodec.cs` | Serializer options |
| `JsonIO.cs` | Streamed read/write, JSONL lines, collision |
| `JsonOptions.cs` | Kind, collision, write/read/session options |
| `HelperLog` / `JsonCatalog` / `JsonEvents` | Logging |

`JsonHelper.Compare(leftPath, rightPath)` reads both files and returns `JsonPatch.Compare`. JSONL files compare as arrays. Mixed kind is `ArgumentException`. No `Apply`.

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Session flow

```
Snapshot   freeze committed as Diff baseline
Set        mutate working only
Diff       RFC 6902 baseline → working (ops= in the log, no values)
Commit     working → committed
Save       write committed (atomic)
SaveWorking  escape hatch; Warning
Revert     working ← snapshot (or committed)
Cancel     drop working, no write
```

JSONL Save rewrites the whole file. No mid-file splice in v1.

Object member compare is ordinal: `network.Timeout` and `network.timeout` are two keys. Hosts that want Windows-ish keys call FileIo UniqueName, not this package.

---

## 5. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` / `ArgumentException` | Null value, blank path, bad path syntax, Compare kind mismatch. |
| `KeyNotFoundException` | `JsonSession.Get` on an unknown path. `TryGet` returns false instead. |
| `JsonException` | Not RFC 8259, BOM, JSON null as document root, truncated JSONL line. |
| `FileNotFoundException` | Open / OpenJsonl / Compare on a missing file. |
| `IOException` | Collision Fail; write failure. |

Expected `ArgumentException` / `JsonException` / `IOException` / `KeyNotFoundException` rethrow without `HelperLog.Trap`.

---

## 6. Still out

Comments / trailing commas, UniqueName, tree copy, mid-file JSONL splice, being the audit logger, Newtonsoft, YAML, RFC 6902 Apply, Desktop unlock, a stream-buffer knob, `Exception` objects on HelperLog.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Sep 2026 | First standalone Design. Content lifted from Guide v1.0 + shipped helper/session. |
| 1.0 | 23 Sep 2026 | PR05: Compare on JsonPatch and JsonHelper. Get throws. Event IDs through 13625. |
| 1.0 | 23 Sep 2026 | PR06-06: object member compare is ordinal. |
