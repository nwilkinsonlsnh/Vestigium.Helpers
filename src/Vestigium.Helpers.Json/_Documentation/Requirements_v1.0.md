# Vestigium.Helpers.Json — Requirements Specification

**Document ID:** VEST-HLP-JSON-SRS-000  
**Version:** 1.0  
**Status:** Accepted  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Json`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Build plan:** [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md)

If implementation and this file disagree, this file wins.

This library is not Vestigium.Logging and is not FileIo. Logging writes the suite audit trail (JSON Lines under `%ProgramData%\\Vestigium\\Logs\\{APPID}\\`). FileIo moves trees. Json loads, queries, edits, and saves **payload** `.json` / `.jsonl` documents (settings, external logs, host exports) so an operator can ask a file a question and see what changed before it hits disk.

---

## 0. How to read this document

It records:

- one façade (`JsonHelper`) for typed serialize/deserialize, parse, and file/stream open
- one session (`JsonSession`) per opened document with working vs committed state
- query by JSON Pointer (RFC 6901) and dotted path
- Snapshot → Set → Diff (RFC 6902) → Commit or Revert → Save or Cancel
- `.json` = one RFC 8259 value; `.jsonl` / NDJSON = one RFC 8259 value per line
- HelperLog only; Category `Helpers`; APPID `Json`; subcategories in §8
- no Newtonsoft; System.Text.Json only
- no FileIo verbs (copy, mirror, UniqueName, recon)
- no second logger (do not write `%ProgramData%\\Vestigium\\Logs\\`)

---

## 1. Purpose

Give every Vestigium host one way to read a settings file or an external JSON/JSONL payload, get a value by path, change it in memory, review the diff, and either commit+save or revert.

```csharp
var doc = JsonHelper.Open(path);          // or OpenExport("probe-settings")
doc.Snapshot();                           // baseline = committed tree
doc.Set("network.timeoutSeconds", 15);
doc.Set("/logging/level", "Information");
var patch = doc.Diff();                   // RFC 6902, in-memory
if (accept)
{
    doc.Commit();
    doc.Save();                           // atomic replace of the opened path
}
else
    doc.Cancel();                         // drop working; disk unchanged
```

This is not Logging. Logging still owns the audit JSONL. This is not FileIo. FileIo still owns trees. Hashing may digest bytes Json already wrote. Json does not grow `Hash*`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Engine | `System.Text.Json` only. No Newtonsoft. No custom parser. |
| 2 | TFM | `net10.0`. Not Windows-only. |
| 3 | Product | Payload documents and queries. Settings files, host exports, external `.json` / `.jsonl`. Not the Vestigium audit logger. |
| 4 | Dialect | Strict RFC 8259 on read and write. UTF-8, **no BOM**, no comments, no trailing commas. Reject JSONC. |
| 5 | Write style | `WriteIndented = true`, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`. |
| 6 | Shape | Static `JsonHelper` (typed ToJson/FromJson, parse, Open/Create/Write) **and** `JsonSession` for an opened document. |
| 7 | Query | JSON Pointer (RFC 6901) **and** dotted path (`network.timeoutSeconds`, `records[0].code`). Same target. No JSONPath wildcards/filters in v1. |
| 8 | Edit lifecycle | Read → Snapshot (baseline = committed) → Set on working → Diff → Commit or Revert → Save or Cancel. Save writes the **committed** tree unless `SaveWorking` is explicit. Cancel drops working and does not write. |
| 9 | Diff | RFC 6902 JSON Patch, in-memory for the host. HelperLog records path + op count, never old/new bodies. |
| 10 | JSONL / NDJSON | One JSON value per line. Objects typical; other values allowed. `Get`/`Set` on a record require that record to be an object. v1: enumerate, query record `i`, `AppendRecord` at end. Snapshot/Diff is the whole record list. No mid-file rewrite of line 47. Save of a JSONL session rewrites the **entire** file from the committed list. |
| 11 | Collision | `JsonCollision.Fail` default on `SaveAs` / `WriteFile` when dest exists. `Overwrite` is explicit. UniqueName is **not** in this library. |
| 12 | Atomic write | `AtomicWrite = true` default. Write a sibling temp in the same directory, flush, then replace dest. |
| 13 | Default folder | If the host omits a directory: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests inject a temp root and never touch the real Desktop. Probe stays `%TEMP%` only. |
| 14 | Streams | 64 KiB buffer. Never `File.ReadAllBytes` / `File.ReadAllText` on a payload. |
| 15 | Logging door | `HelperLog` only. Category `Helpers`. APPID `Json`. Library never calls `VestigiumLogger.Initialize`. |
| 16 | Log contents | Paths, record counts, byte lengths, Pointer/dotted paths, op counts. **Never** JSON bodies, field values, or `Exception` objects. |
| 17 | Siblings | May call Hashing for a digest of **written UTF-8 bytes**. Must not grow `Hash*`. Must not reference Csv, ClosedXml, FileIo, Charts, Analytics (v1). Must not write under `Logs\\`. |
| 18 | Schema / JSONPath / Merge-Patch | **Not v1.** Roadmap. |
| 19 | Source generation | Reflection-based `JsonSerializer` in v1. Source-gen options later. |

---

## 3. Goals

**G1.** One façade (`JsonHelper`) owns Identity, Probe, typed JSON, and Open/Create.  
**G2.** One `JsonSession` per document owns working vs committed trees, Snapshot, Diff, Commit, Revert, Save, Cancel.  
**G3.** Get/Set by Pointer or dotted path on objects.  
**G4.** `.jsonl` is a first-class payload format (NDJSON = JSONL).  
**G5.** Default export folder is Desktop `\\Vestigium\\Exports\\Json\\`.  
**G6.** Collision default Fail; Overwrite explicit; writes atomic.  
**G7.** Logs meet ALCOA+ through HelperLog. Category `Helpers`, APPID `Json`.  
**G8.** `Probe` is `%TEMP%` only. It must not write the Desktop and must not open a durable session.  
**G9.** Hosts can review Diff in-memory and revert before anything hits disk.

---

## 4. Documents

### 4.1 `.json`

One RFC 8259 value. Settings files are objects. Arrays and primitives are legal documents; path Set that requires an object member fails if the root is not an object (or the parent is not an object).

### 4.2 `.jsonl` (NDJSON)

NDJSON and JSONL are the same family: **one complete JSON value per line**, UTF-8, newline-delimited (`\\n` write; read `\\n` / `\\r\\n`). Empty lines are skipped. A truncated last line is Failed.

This is a **payload** file (external instrument log, dumped records, settings-per-line). It is not `%ProgramData%\\Vestigium\\Logs\\`. Json may **read** a Vestigium log file as just another JSONL payload. Json must not **append audit lines** or format Vestigium.Logging records.

v1 JSONL mutations:

- `AppendRecord` — add one value at end of the working list
- `Set` on path `[i].member` — member on that object record
- Save — rewrite the whole file from the committed list (atomic)
- Not v1: splice/delete a line in the middle without rewriting the file; stream-append without a session

### 4.3 Streams and strings

`Parse(string|Stream|ReadOnlySpan<byte>)` and `ToJson`/`FromJson<T>` do not create a session. Sessions begin at `Open` / `Create` / `OpenJsonl`.

---

## 5. Query paths

Two spellings, one target.

| Kind | Example | Notes |
|---|---|---|
| JSON Pointer | `/network/timeoutSeconds` | RFC 6901. `~1` `/`, `~0` `~`. |
| Dotted | `network.timeoutSeconds` | Object members. |
| Index | `records[0].code` or `/records/0/code` | Zero-based. |
| JSONL index | `[0].code` | First line, member `code`. |

Unknown path on Get: return missing (nullable / `TryGet`). Unknown path on Set: create object parents when the parent is an object; do not create through a primitive. Invalid syntax: `HelperGuard` Failed then throw.

No `$..`, `*`, or filter expressions in v1.

---

## 6. Session lifecycle

```
Open / Create
  committed = parsed tree (Create: empty object or empty JSONL list)
  working   = copy of committed
  Snapshot()  → snapshot = copy of committed   (explicit baseline for Diff)
  Set / AppendRecord  → working only
  Diff()      → RFC 6902 patch from snapshot (or committed if no Snapshot) to working
  Commit()    → committed = copy of working
  Revert()    → working = copy of snapshot (if set) else committed
  Save()      → write committed, atomic replace of Path
  SaveWorking() → write working without Commit (explicit; logged Warning)
  Cancel()    → working = committed; no disk write
```

`Save` of an opened path is replace-in-place (atomic). It is not a collision. `SaveAs` / `WriteFile` to a **different** existing path uses `JsonCollision` (default Fail).

Cancel here means session Cancel (drop working), not FileIo job Cancel.

---

## 7. Public surface (v1)

Names may move a token. The shapes may not.

```csharp
public static class JsonHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Json"
    public static string Probe();

    public static string DefaultExportDirectory();
    public static string NewExportPath(string? stem = null, JsonDocumentKind kind = JsonDocumentKind.Json);

    public static string ToJson<T>(T value, JsonWriteOptions? options = null);
    public static T FromJson<T>(string json, JsonReadOptions? options = null);
    public static JsonNode Parse(string json);
    public static JsonNode Parse(Stream stream);

    public static JsonSession Create(string? path = null, JsonSessionOptions? options = null);
    public static JsonSession Open(string path, JsonSessionOptions? options = null);
    public static JsonSession OpenJsonl(string path, JsonSessionOptions? options = null);
    public static JsonSession OpenExport(string stem, JsonDocumentKind kind = JsonDocumentKind.Json,
        JsonSessionOptions? options = null);

    public static void WriteFile<T>(string path, T value, JsonWriteOptions? options = null);
}

public sealed class JsonSession : IDisposable
{
    public string SessionId { get; }
    public string? Path { get; }
    public JsonDocumentKind Kind { get; }
    public bool HasUncommittedWork { get; }
    public bool HasUnsavedCommit { get; }

    public void Snapshot();
    public bool TryGet<T>(string path, out T? value);
    public T Get<T>(string path);
    public void Set(string path, object? value);
    public void AppendRecord(JsonNode record);          // JSONL only
    public JsonNode? Record(int index);                 // JSONL
    public int RecordCount { get; }

    public JsonPatch Diff();
    public void Commit();
    public void Revert();
    public void Cancel();
    public string Save();
    public string SaveWorking();
    public string SaveAs(string path, JsonCollision collision = JsonCollision.Fail);
}

public enum JsonDocumentKind { Json = 0, Jsonl = 1 }
public enum JsonCollision { Fail = 0, Overwrite = 1 }

public sealed class JsonWriteOptions
{
    public bool WriteIndented { get; init; } = true;
    public JsonCollision Collision { get; init; } = JsonCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
}

public sealed class JsonSessionOptions
{
    public JsonCollision Collision { get; init; } = JsonCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
}
```

`DefaultExportDirectory` is `%DESKTOP%\\Vestigium\\Exports\\Json\\` unless a test injects `JsonTestHooks.ExportRoot`. `NewExportPath` uses stem + `.json` / `.jsonl` under that folder.

`JsonPatch` is the RFC 6902 operation list (add / remove / replace). Hosts render it. Json does not reference Charts.

Optional v1: `JsonHelper.DigestWritten(path, HashingAlgorithm = Sha256)` calls Hashing on the file bytes after Save. Not required to ship Phase 1.

---

## 8. Logging

Category = `Helpers`. APPID = `Json`.

Register these subcategories (reuse existing names where they already exist):

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing suite |
| Session | Open, Create, Dispose, SessionId |
| Document | Parse, kind, path, byte length, record count |
| Query | Get/Set path spelling (not the value) |
| Snapshot | Baseline taken |
| Diff | Operation count only |
| Commit | Commit / Revert / Cancel |
| Save | Path, bytes, collision, atomic |
| Jsonl | OpenJsonl, AppendRecord (index only), rewrite |

Sparse audit: session start, snapshot, commit/revert/cancel, save, Failed. Not a line per Get of a quiet settings key.

Never: payload body, PEM, long hex, `Exception` argument to HelperLog.

---

## 9. Demo

`Vestigium.Helpers.Json.Demo` stays on `HelperWpfHost` + APPID `Json` until this SRS is Accepted. After Phase 4 it becomes a shipped gallery:

- Overview, Settings (`.json` Get/Set/Diff/Commit/Save/Cancel), Jsonl (enumerate + append), Export folder, JSONL audit pane
- Demo files under the export folder or `%TEMP%\\Vestigium.Helpers.Json.Demo` when tests demand isolation
- Probe remains `%TEMP%` only

---

## 10. Tests

xUnit, serial logger collection, temp `LogDirectory`, injected export root.

- Identity is `Vestigium.Helpers.Json`.
- Probe writes Pending then Success; writes only under `%TEMP%`.
- Strict RFC 8259: comments and trailing commas throw.
- Pointer and dotted path resolve the same member.
- Snapshot → Set → Diff has one replace → Revert restores → Cancel leaves disk untouched.
- Commit then Save persists; Save without Commit does not (unless `SaveWorking`).
- `SaveAs` existing dest with default collision throws; Overwrite replaces.
- Atomic write: dest is either the previous file or the complete new file.
- JSONL: three object lines enumerate; `Get` on `[1].field`; `AppendRecord`; Save rewrites four lines.
- JSONL primitive line is a legal record; `Set` on that record throws.
- Tests never touch the real Desktop or live ProgramData.
- No payload body in captured HelperLog lines.

---

## 11. Non-goals (v1)

- Vestigium.Logging writer / `%ProgramData%\\Vestigium\\Logs\\`
- FileIo copy, move, delete, mirror, UniqueName, recon, buckets
- Csv / ClosedXml / Excel
- Newtonsoft
- JSON Schema
- JSONPath filters
- RFC 7396 Merge Patch (6902 Patch is Diff only)
- Mid-file JSONL splice without rewrite
- Source generators
- BOM, comments, trailing commas
- Admin rights, scheduler

---

## 12. Roadmap

| Version | Item |
|---|---|
| **v1.0** | This document |
| **v1.1** | `DigestWritten` via Hashing; Compare two files (semantic node compare + patch) |
| **v1.2** | JSONL delete/replace record by index without a full in-memory list when file is large |
| **later** | JSON Schema, JSONPath, source-gen, Merge Patch |

Never here: being the suite logger; being FileIo; being Csv.

---

## 13. Acceptance

This SRS is **Accepted** on `main` (Phase 0). Acceptance means:

1. This file and the Developers Guide and Implementation Plan live under `src/Vestigium.Helpers.Json/_Documentation/`.
2. §8 subcategories are registered in `HelperLog.CreateTaxonomy` (Phase 0 commit).
3. A follow-up implementation PR can be reviewed against this text without inventing UniqueName, a second logger, or Newtonsoft.

Do not grow `JsonHelper` past Identity + Probe until Status is Accepted. Phase 0 closed that gate.
