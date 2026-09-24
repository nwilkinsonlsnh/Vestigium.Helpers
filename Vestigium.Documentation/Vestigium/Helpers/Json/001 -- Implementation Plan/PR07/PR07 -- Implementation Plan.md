# Vestigium.Helpers.Json — PR07 Implementation Plan

**Document ID:** VEST-HLP-JSON-PLAN-PR07  
**Version:** 1.0  
**Status:** Open  
**Date:** 24 September 2026  
**Package:** `Vestigium.Helpers.Json`  
**Backlog:** [`PR07 -- Backlog.md`](PR07%20--%20Backlog.md)  
**Binding:** `Requirements_v1.0.md` wins. This plan does not add requirements.

PR05 shipped Event IDs, span parse, truncated JSONL, and public Compare. PR06 started host-safety and did not finish. PR07 is one pass: wire the claimed caps, fail-closed Open / write / export path, finish catalog + paper, tests that prove the claims.

---

## 0. How to use this file

1. Read the backlog. Do not reopen items listed under Out of PR07.
2. Implement in the order below. Do not start the next step until that step's gate is green.
3. Commit form: `Json PR07: <step goal>`.
4. Do not invent APIs that are not in this plan or SRS §7.
5. Do not call `VestigiumLogger.Initialize`. Do not log bodies or `Exception` objects. Do not UniqueName. Do not use Newtonsoft.
6. PR07-12 (`DigestWritten`) is cut unless the owner says otherwise before Step 8.

---

## 1. What this PR is

| Step | Backlog | Goal |
| :--- | :--- | :--- |
| 1 | PR07-01 | `Parse(Stream)` honors `MaxDocumentBytes`. String and span Parse use the same UTF-8 byte cap. |
| 2 | PR07-02 | `Open("*.jsonl")` fails closed. |
| 3 | PR07-03 | Write over cap leaves no oversized dest. |
| 4 | PR07-04 | Export containment uses `SamePath` compare. |
| 5 | PR07-07 | Non-seekable `Parse(Stream)` fails closed. |
| 6 | PR07-06 | Snapshot / Diff / Commit Failed IDs (13630–13640). |
| 7 | PR07-05 | Catalog rows match `json.json`. Version bump. |
| 8 | PR07-10, PR07-11 | Typed-null and JSONL-null rules. |
| 9 | PR07-08 | `FullyQualifiedName~Json` proves Steps 1–8. |
| 10 | PR07-09 | Paper matches code. Archive PR06 as superseded. |

One PR on `main` is allowed if the steps land in that order in the same branch. Split commits still.

---

## 2. Locks this plan adds (not new product)

These are implementation choices for holes the backlog already named. They do not grow the façade.

| Topic | Lock |
| :--- | :--- |
| Cap on Parse | All three `Parse` overloads honor `JsonIo.MaxDocumentBytes`. Stream: remaining bytes when seekable. String / span: UTF-8 byte count (`Encoding.UTF8.GetByteCount` / `span.Length`). |
| Non-seekable stream | `Parse(Stream)` requires `CanSeek`. No seek → `ArgumentException` (same family as unreadability). Do not count-while-parse in this wave. |
| `Open` + `.jsonl` | Extension check via `JsonIo.KindFromPath` before read. Reject with `ArgumentException` + `HelperLog.Reject` under Session. Message names `OpenJsonl`. |
| Over-cap write | Check planned output before dest is the live file. Atomic: temp + length check + delete temp on reject + no `Move`. Non-atomic: write temp, check, then `Move` overwrite only if in cap — or refuse to write dest at all. Dest after throw: previous file or absent. |
| Export prefix | Use `JsonIo.SamePath` rules (ordinal ignore case on Windows). |
| Failed Event IDs | Add three rows. Do not document a collapse onto GuardFailed. |
| `FromJson` JSON null | Same as `Parse`: `JsonException`, not `value!`. |
| JSONL `null` line | Legal record (`null` + newline). `AppendRecord` still rejects a C# null argument. |
| Version | `1.0.1`. Becomes `1.1.0` only if the owner un-cuts PR07-12 before Step 8. |

---

## 3. Step 1 — Parse cap (PR07-01)

`JsonIo.EnsureStreamWithinCap` is dead. Wire it.

- `Parse(Stream)`: after `NotNull` / `CanRead` / BOM reject, call `EnsureStreamWithinCap`. Then `JsonNode.Parse`.
- `Parse(string)`: after `RequireRfc8259Text`, reject when `Encoding.UTF8.GetByteCount(text) > MaxDocumentBytes`.
- `Parse(ReadOnlySpan<byte>)`: reject when `utf8Json.Length > MaxDocumentBytes`.
- Log: `document exceeds cap bytes= cap=` under Document. Never the body.
- `JsonException` text keeps the word `cap` so the existing file test stays valid.

Do not raise the default 32 MiB. Tests keep using `JsonTestHooks.MaxDocumentBytes`.

### Gate

`JsonFileTests.Parse_stream_rejects_document_over_cap` passes. Add one string and one span case at the same hook value.

---

## 4. Step 2 — Open rejects JSONL (PR07-02)

In `JsonHelper.Open`, after `FileExists` and full path:

```text
if (JsonIo.KindFromPath(target) == JsonDocumentKind.Jsonl)
    reject + ArgumentException("Open is a single RFC 8259 document. Use OpenJsonl.")
```

Do not call `JsonIo.Read` on that path. `Create("*.jsonl")` and `OpenExport(..., Jsonl)` stay as they are.

### Gate

A one-line `.jsonl` file (`{"n":1}\n`) thrown from `Open` is `ArgumentException`. `OpenJsonl` on the same file still works.

---

## 5. Step 3 — Write over cap (PR07-03)

Today `WriteJson` / `WriteJsonlTo` write dest (or temp), then `RejectWrittenTooLarge`. Non-atomic dest is already oversized when the throw happens.

Change:

- Always write the sibling temp first when size is unknown until serialized.
- After flush, if `stream.Length > MaxDocumentBytes`, delete temp, `HelperLog.Reject`, throw `JsonException`.
- `Move` onto dest only after the check.
- Non-atomic (`AtomicWrite = false`) uses the same temp-then-replace so dest is never the overflow file. That is a write-path change, not a new option.

Collision still runs before any write.

### Gate

`WriteFile` with the cap hook below payload size: dest missing, no `*.tmp` left. Repeat with `AtomicWrite = false` on `JsonWriteOptions`.

---

## 6. Step 4 — Export containment (PR07-04)

`ResolveExportFile` prefix test must use the same comparison as `SamePath`:

- Windows: `StringComparison.OrdinalIgnoreCase`
- elsewhere: `StringComparison.Ordinal`

Keep invalid-character rewrite, reject `.` / `..` / blank, reject a dest that is not under the export root.

### Gate

On Windows, a stem that case-walks out of the injected export root throws `ArgumentException`. A normal stem under the root still resolves.

---

## 7. Step 5 — Non-seekable stream (PR07-07)

`Parse(Stream)` after `CanRead`:

- `!CanSeek` → `HelperLog.Reject` + `ArgumentException` ("Stream must be seekable.").
- Then BOM, then cap, then parse.

File `Open` / `OpenJsonl` already open seekable `FileStream`. No change there.

### Gate

A non-seekable wrapper around a small valid payload throws `ArgumentException`. A seekable `MemoryStream` of the same bytes still parses.

---

## 8. Step 6 — Failed Event IDs (PR07-06)

Add to the existing block. Count by 5. Do not leave 13999.

| EventId | Name | Level | Subcategory |
| ---: | :--- | :--- | :--- |
| 13630 | SnapshotFailed | Error | Snapshot |
| 13635 | DiffFailed | Error | Diff |
| 13640 | CommitFailed | Error | Commit |

Edit list:

- `JsonEvents.cs` — three consts.
- `JsonCatalog.Rows` — three rows. Keep `Register` subcategory list as-is (names already exist).
- `EventCatalog/json.json` — same three rows.
- `HelperLog.EventId` — Snapshot / Diff / Commit `Map(...)` uses the new Failed ID, not `GuardFailed`.
- `HelperLog.CatalogMessage` — one string per ID.
- `Write` still passes `exception: null`.

Used-through line becomes 13640.

### Gate

Catalog row count is 29. Logging test still asserts Save / Commit / Jsonl do not share 13515. A forced Snapshot/Diff/Commit reject (disposed session is enough) lands on 13630 / 13635 / 13640, not 13620.

---

## 9. Step 7 — Pack and version (PR07-05)

- `Version` → `1.0.1` (or `1.1.0` only if PR07-12 is live).
- `json.json` and `JsonCatalog.Rows` / `JsonEvents` stay byte-for-byte aligned on id, name, subcategory, severity.
- Do not load the JSON at runtime. Hashing does not. Copy that story.
- README already packs. Leave SourceLink / snupkg as they are.
- Package README EVENTID line: used through 13640.

### Gate

A test (or the existing catalog test) asserts every `JsonEvents` const appears once in `Rows` and that `Rows.Length` matches the JSON event array length.

---

## 10. Step 8 — Null rules (PR07-10, PR07-11)

`FromJson<T>`:

- After deserialize, if the payload text is JSON `null` (or `value` is null and `T` is a non-nullable reference), reject with `JsonException` ("RFC 8259 JSON null is not a document root for FromJson.") and `HelperLog.Reject` under Document.
- Do not return `value!`.

JSONL:

- `ReadJsonl` keeps a line that parses as JSON `null` as a null slot in the array (legal NDJSON).
- `AppendRecord` keeps `HelperGuard.NotNull`.
- One test: file containing `null\n{"n":1}\n` opens with `RecordCount == 2` and `Record(0)` is null.

PR07-12 stays cut. Do not add a Hashing package reference in this step.

### Gate

`FromJson<Dictionary<string,int>>("null")` throws `JsonException`. JSONL null-line test green. No `Hashing` reference on the Json csproj.

---

## 11. Step 9 — Tests (PR07-08)

Extend existing files. Do not add a second test project.

| File | Covers |
| :--- | :--- |
| `JsonFileTests.cs` | stream / string / span cap; `Open("*.jsonl")` reject; non-atomic over-cap write; export case-escape; seekable vs non-seekable Parse |
| `JsonlTests.cs` | JSON null line; line cap and file cap still reject without bodies |
| `JsonLoggingTests.cs` | new Failed IDs; catalog row count 29; no `EXCEPTION` objects |
| `JsonPureTests.cs` | `FromJson` of `null` |
| `JsonCoverageTests.cs` | only if a branch in Steps 1–8 has no home above |

Command:

```text
dotnet build src/Vestigium.Helpers.Json/Vestigium.Helpers.Json.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json
```

Tests set `JsonTestHooks.ExportRoot`. They do not touch the real Desktop.

---

## 12. Step 10 — Paper (PR07-09)

| File | Change |
| :--- | :--- |
| `002 -- Requirements Document/Requirements_v1.0.md` | §4.3: Parse cap + seekable stream. §7 `Open`: `.jsonl` path rejected. §8 Query: Get / TryGet / Record quiet; Set audited. Used-through 13640. §9: strike Demo / gallery. §13: docs live under `Vestigium.Documentation/Vestigium/Helpers/Json/`. |
| `003 -- Design Document/Design_v1.0.md` | Caps on Parse. Open vs OpenJsonl. Export compare. Failed IDs. Null rules. Version 1.0.1. |
| `004 --Developers Guide/DevelopersGuide_v1.0.md` | Strike `Vestigium.Helpers.Json.Demo` / gallery tabs. Same Open / cap / quiet-Get sentences as the README. |
| `src/Vestigium.Helpers.Json/README.md` | Surface: Open rejects `.jsonl`. Parse honors cap. EVENTID through 13640. Version 1.0.1. |
| PR06 folder | Move under `000 -- Archived/001 -- Implementation Plan/PR06/` if that is still the house rule; otherwise mark PR06 backlog Status superseded by this file. |

Do not change Desktop default, 64 KiB, or the “never log Exception objects” rule.

### Gate

The four living documents no longer name a Json Demo, no longer point at `src/Vestigium.Helpers.Json/_Documentation/`, and no longer claim `Open` accepts `.jsonl`.

---

## 13. Out of this plan

Same list as the backlog: Apply and `move` / `copy` / `test`, LCS array diff, async IO, Desktop injector, buffer knob, session locks, domain exceptions, stack traces, Schema, JSONPath, Merge Patch, source-gen, mid-file JSONL splice, shared HelperLog, host UI, `DigestWritten` unless the owner un-cuts it before Step 8.

If a step wants one of those, the step is wrong.

---

## 14. Close gate

1. Steps 1–10 are in the tree. PR07-12 is absent unless the owner un-cut it.
2. Event IDs stay inside 13500–13999, step 5, used through 13640.
3. `FullyQualifiedName~Json` is green.
4. HelperLog writes still pass `exception: null` and never include payload bodies.
5. `Parse(Stream)` over cap throws `JsonException` and does not return a node.
6. `Open` of a `.jsonl` path does not return a `Json` session.
7. Package version is `1.0.1` (or `1.1.0` only with DigestWritten).
8. This file Status flips to Closed. Date the close.

---

## Document control

| Version | Date | Change |
| :--- | :--- | :--- |
| 1.0 | 24 Sep 2026 | Open. Ten steps bound to PR07 backlog. DigestWritten cut. |
