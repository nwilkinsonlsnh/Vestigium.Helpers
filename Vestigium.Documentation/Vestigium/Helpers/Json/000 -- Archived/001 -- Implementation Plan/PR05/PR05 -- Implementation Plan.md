# Vestigium.Helpers.Json — PR05 Implementation Plan

**Document ID:** VEST-HLP-JSON-PLAN-PR05  
**Version:** 1.0  
**Status:** Open  
**Date:** 23 September 2026  
**Package:** `Vestigium.Helpers.Json`  
**Backlog:** [`PR05 -- Backlog.md`](PR05%20--%20Backlog.md)  
**Binding:** `Requirements_v1.0.md` wins. This plan does not add requirements.

Phase 0–5 shipped the library. PR05 is one pass: Event IDs, span parse, truncated JSONL, public compare, paper, tests.

---

## 0. How to use this file

1. Read the backlog. Do not reopen items listed under Out of PR05.
2. Implement in the order below. Do not start the next step until that step's gate is green.
3. Commit form: `Json PR05: <step goal>`.
4. Do not invent APIs that are not in this plan or SRS §7 (plus the span overload SRS §4.3 already named).
5. Do not call `VestigiumLogger.Initialize`. Do not log bodies or `Exception` objects. Do not UniqueName. Do not use Newtonsoft.

---

## 1. What this PR is

| Step | Backlog | Goal |
| :--- | :--- | :--- |
| 1 | PR05-01 | Event IDs by subcategory + level in 13500–13999 |
| 2 | PR05-02 | `JsonHelper.Parse(ReadOnlySpan<byte>)` |
| 3 | PR05-03 | Truncated JSONL last line is Failed |
| 4 | PR05-04 | Public `JsonPatch.Compare` + `JsonHelper.Compare` |
| 5 | PR05-05 | Paper matches code (Get throws; span parse; Event IDs) |
| 6 | PR05-06 | Suite filter `FullyQualifiedName~Json` green |

One PR on `main` is allowed if the steps land in that order in the same branch. Split commits still.

---

## 2. Step 1 — Event IDs (PR05-01)

Keep Probe at 13500 / 13505. Count by 5. Do not leave the block.

### Assigned IDs

| EventId | Name | Level | Subcategory |
| ---: | :--- | :--- | :--- |
| 13500 | ProbeEnter | Debug | Probe |
| 13505 | ProbeComplete | Information | Probe |
| 13510 | SessionEnter | Debug | Session |
| 13515 | SessionComplete | Information | Session |
| 13520 | SessionFailed | Error | Session |
| 13525 | DocumentEnter | Debug | Document |
| 13530 | DocumentComplete | Information | Document |
| 13535 | DocumentFailed | Error | Document |
| 13540 | QueryEnter | Debug | Query |
| 13545 | QueryComplete | Information | Query |
| 13550 | QueryFailed | Error | Query |
| 13555 | SnapshotEnter | Debug | Snapshot |
| 13560 | SnapshotComplete | Information | Snapshot |
| 13565 | DiffEnter | Debug | Diff |
| 13570 | DiffComplete | Information | Diff |
| 13575 | CommitEnter | Debug | Commit |
| 13580 | CommitComplete | Information | Commit |
| 13585 | SaveEnter | Debug | Save |
| 13590 | SaveComplete | Information | Save |
| 13595 | SaveWarning | Warning | Save |
| 13600 | SaveFailed | Error | Save |
| 13605 | JsonlEnter | Debug | Jsonl |
| 13610 | JsonlComplete | Information | Jsonl |
| 13615 | JsonlFailed | Error | Jsonl |
| 13620 | GuardFailed | Error | Guard |
| 13625 | OperationWarning | Warning | Guard |

`OperationEnter` / `OperationComplete` / `OperationFailed` at 13510 / 13515 / 13520 go away. Callers that only knew those three constants update to the subcategory IDs.

### Edit list

- `JsonEvents.cs` — public consts matching the table.
- `JsonCatalog.Rows` — one row per const. `Register` already lists the subcategory names; leave that list.
- `EventCatalog/json.json` — same rows. `subcategory` is the HelperLog name, not `Json`.
- `HelperLog.EventId` — switch on subcategory, then level. Probe stays special-cased on enter vs complete. Unknown subcategory falls to GuardFailed / OperationWarning / SessionComplete by level, never to a silent 13515.
- `HelperLog.CatalogMessage` — one string per ID.
- `Write` still passes `exception: null`.

### Gate

A logging test that runs `Open` + `Set` + `Commit` + `Save` + `OpenJsonl` asserts EventIds are the new constants, not a single Operation* bucket.

---

## 3. Step 2 — Span parse (PR05-02)

```csharp
public static JsonNode Parse(ReadOnlySpan<byte> utf8Json)
```

Same reject rules as `Parse(string)`:

- empty span → `ArgumentException`
- leading `EF BB BF` → `JsonException` (BOM)
- `JsonNode.Parse` failure → `JsonException` (not RFC 8259)
- JSON null root → `JsonException`

Use `JsonCodec.NodeOptions` / `DocumentOptions`. Log under `Document` (`ParseSpan`). No session. No file IO.

### Gate

Coverage tests: object payload, BOM prefix, empty span, `null` literal.

---

## 4. Step 3 — Truncated JSONL (PR05-03)

In `JsonIo.ReadJsonl`:

- Keep skipping blank lines.
- After the last non-empty line, if the file does not end in `\n` or `\r\n` **and** that line is not a complete RFC 8259 value, reject.
- A complete value with no trailing newline is accepted (common NDJSON writer). A chopped `{` or `"abc` is Failed.
- `HelperLog.Reject` with `index=` then `JsonException`.

Do not keep a partial `JsonArray` after the throw. Dispose the stream as today.

### Gate

`JsonlTests`: three good object lines + a final `{` throws and does not yield three records to the caller. Three good lines with no trailing newline still open.

---

## 5. Step 4 — Compare (PR05-04)

- `JsonPatch.Compare(JsonNode? from, JsonNode? to)` becomes `public static`. Same algorithm. Still add / remove / replace only.
- `JsonHelper.Compare(string leftPath, string rightPath)`:
  - both paths required, full-pathed via `HelperGuard.FileExists`
  - kind from extension (`JsonIo.KindFromPath`); mixed kind (`.json` vs `.jsonl`) is `ArgumentException`
  - JSON → `JsonIo.Read`; JSONL → `JsonIo.ReadJsonl`
  - return `JsonPatch.Compare(left, right)`
  - log under `Diff`: both paths + `ops=`

No `Apply`. No `from` field on operations. No Charts.

### Gate

Node compare of identical trees is empty. File compare of two temp exports with one replaced member has one `replace`. JSON vs JSONL throws.

---

## 6. Step 5 — Paper (PR05-05)

| File | Change |
| :--- | :--- |
| `002 -- Requirements Document/Requirements_v1.0.md` | §5: unknown Get throws `KeyNotFoundException`; `TryGet` is false. §4.3 / §7: `Parse(ReadOnlySpan<byte>)` on the façade. §8: Event IDs are per subcategory in 13500–13999. Roadmap v1.1 compare: mark the Diff half as PR05. |
| `003 -- Design Document/Design_v1.0.md` | Shape table: `Compare` lives on `JsonPatch` and `JsonHelper`. Exception policy unchanged. |
| `src/Vestigium.Helpers.Json/README.md` | Surface row for `Parse(ReadOnlySpan<byte>)` and `Compare`. EVENTID line: used through 13625. |

Do not change Desktop default, 64 KiB, or the “never log Exception objects” rule.

### Gate

The three files no longer contradict `JsonSession.Get` or the new façade members.

---

## 7. Step 6 — Tests (PR05-06)

Extend existing files. Do not add a second test project.

| File | Covers |
| :--- | :--- |
| `JsonLoggingTests.cs` | Event IDs per subcategory; no bodies |
| `JsonCoverageTests.cs` | span parse; `JsonPatch.Compare` empty / replace |
| `JsonlTests.cs` | truncated last line; clean last line without newline |
| `JsonFileTests.cs` | `JsonHelper.Compare` same file / different member / mixed kind |

Command:

```text
dotnet build src/Vestigium.Helpers.Json/Vestigium.Helpers.Json.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json
```

Tests set `JsonTestHooks.ExportRoot`. They do not touch the real Desktop.

---

## 8. Out of this plan

Same list as the backlog: Apply and `move` / `copy` / `test`, async IO, Desktop injector, buffer knob, session locks, domain exceptions, stack traces, Schema, JSONPath, Merge Patch, source-gen, `DigestWritten`.

If a step wants one of those, the step is wrong.

---

## 9. Close gate

1. Steps 1–6 are in the tree.
2. Event IDs match the table in §2 and stay inside 13500–13999, step 5.
3. `FullyQualifiedName~Json` is green.
4. HelperLog writes still pass `exception: null` and never include payload bodies.
5. This file Status flips to Closed. Date the close.

---

## Document control

| Version | Date | Change |
| :--- | :--- | :--- |
| 1.0 | 23 Sep 2026 | Open. Six steps bound to PR05 backlog. |
