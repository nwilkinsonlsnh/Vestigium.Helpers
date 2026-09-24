# Vestigium.Helpers.Json — Requirements Specification

**Document ID:** VEST-HLP-JSON-SRS-000  
**Version:** 1.0  
**Status:** Accepted  
**Date:** 24 September 2026  
**Package:** `Vestigium.Helpers.Json` 1.0.1  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](../004%20--Developers%20Guide/DevelopersGuide_v1.0.md)

If implementation and this file disagree, this file wins.

This library is not Vestigium.Logging and is not FileIo. Logging writes the suite audit trail (JSON Lines under `%ProgramData%\\Vestigium\\Logs\\{APPID}\\`). FileIo moves trees. Json loads, queries, edits, and saves **payload** `.json` / `.jsonl` documents (settings, external logs, host exports) so an operator can ask a file a question and see what changed before it hits disk.

Long-form documents live under `Vestigium.Documentation/Vestigium/Helpers/Json/`.

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
| 10 | JSONL / NDJSON | One JSON value per line. Objects typical; other values allowed, including JSON `null`. `Get`/`Set` on a record require that record to be an object. v1: enumerate, query record `i`, `AppendRecord` at end. Snapshot/Diff is the whole record list. No mid-file rewrite of line 47. Save of a JSONL session rewrites the **entire** file from the committed list. |
| 11 | Collision | `JsonCollision.Fail` default on `SaveAs` / `WriteFile` when dest exists. `Overwrite` is explicit. UniqueName is **not** in this library. |
| 12 | Atomic write | Always a sibling temp in the same directory, flush, then replace dest. Dest is previous file, absent, or complete in-cap. |
| 13 | Default folder | If the host omits a directory: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests inject a temp root and never touch the real Desktop. Probe stays `%TEMP%` only. |
| 14 | Streams | 64 KiB buffer. Never `File.ReadAllBytes` / `File.ReadAllText` on a payload. `Parse(Stream)` requires a seekable stream. |
| 15 | Logging door | `HelperLog` only. Category `Helpers`. APPID `Json`. Library never calls `VestigiumLogger.Initialize`. |
| 16 | Log contents | Paths, record counts, byte lengths, Pointer/dotted paths, op counts. **Never** JSON bodies, field values, or `Exception` objects. |
| 17 | Siblings | Must not grow `Hash*`. Must not reference Csv, ClosedXml, FileIo, Charts, Analytics (v1). Must not write under `Logs\\`. `DigestWritten` is not shipped. |
| 18 | Schema / JSONPath / Merge-Patch | **Not v1.** Roadmap. |
| 19 | Source generation | Reflection-based `JsonSerializer` in v1. Source-gen options later. |
| 20 | Document cap | 32 MiB on files and on `Parse(string|Stream|ReadOnlySpan<byte>)`. JSONL line cap 1 MiB. |
| 21 | `Open` vs `OpenJsonl` | `Open` is one RFC 8259 document. A `.jsonl` path is rejected. Caller uses `OpenJsonl`. |
| 22 | JSON null | `Parse` and `FromJson` reject a JSON-null root. A JSONL `null` line is a legal record. `AppendRecord` rejects C# null. |

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

NDJSON and JSONL are the same family: **one complete JSON value per line**, UTF-8, newline-delimited (`\\n` write; read `\\n` / `\\r\\n`). Empty lines are skipped. A truncated last line is Failed. A line that is JSON `null` is a legal record.

This is a **payload** file (external instrument log, dumped records, settings-per-line). It is not `%ProgramData%\\Vestigium\\Logs\\`. Json may **read** a Vestigium log file as just another JSONL payload. Json must not **append audit lines** or format Vestigium.Logging records.

v1 JSONL mutations:

- `AppendRecord` — add one value at end of the working list. C# null is rejected.
- `Set` on path `[i].member` — member on that object record
- Save — rewrite the whole file from the committed list (atomic)
- Not v1: splice/delete a line in the middle without rewriting the file; stream-append without a session

### 4.3 Streams and strings

`Parse(string|Stream|ReadOnlySpan<byte>)` and `ToJson`/`FromJson<T>` do not create a session. Sessions begin at `Open` / `Create` / `OpenJsonl`. `Parse(ReadOnlySpan<byte>)` lives on the façade next to the string and stream overloads. Empty span and a UTF-8 BOM (`EF BB BF`) are Failed the same way the string path rejects `\\uFEFF`. All three `Parse` doors honor the 32 MiB document cap. `Parse(Stream)` requires a readable, seekable stream. `FromJson` rejects a JSON-null root.

---

## 5. Query paths

Two spellings, one target.

| Kind | Example | Notes |
|---|---|
| JSON Pointer | `/network/timeoutSeconds` | RFC 6901. `~1` `/`, `~0` `~`. |
| Dotted | `network.timeoutSeconds` | Object members. |
| Index | `records[0].code` or `/records/0/code` | Zero-based. |
| JSONL index | `[0].code` | First line, member `code`. |

Unknown path on Get: throw `KeyNotFoundException`. Unknown path on `TryGet`: return false. Unknown path on Set: create object parents when the parent is an object; do not create through a primitive. Invalid syntax: `HelperGuard` Failed then throw.

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
    public static JsonNode Parse(ReadOnlySpan<byte> utf8Json);

    public static JsonSession Create(string? path = null, JsonSessionOptions? options = null);
    public static JsonSession Open(string path, JsonSessionOptions? options = null);
    public static JsonSession OpenJsonl(string path, JsonSessionOptions? options = null);
    public static JsonSession OpenExport(string stem, JsonDocumentKind kind = JsonDocumentKind.Json,
        JsonSessionOptions? options = null);

    public static void WriteFile<T>(string path, T value, JsonWriteOptions? options = null);
    public static JsonPatch Compare(string leftPath, string rightPath);
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

`DefaultExportDirectory` is `%DESKTOP%\\Vestigium\\Exports\\Json\\` unless a test injects `JsonTestHooks.ExportRoot`. `NewExportPath` uses stem + `.json` / `.jsonl` under that folder. Containment uses the same compare as `SamePath`.

`JsonPatch` is the RFC 6902 operation list (add / remove / replace). `JsonPatch.Compare(JsonNode?, JsonNode?)` and `JsonHelper.Compare(string, string)` are public. Hosts render the list. Json does not apply the patch and does not reference Charts. Mixed `.json` / `.jsonl` is `ArgumentException`. JSONL files compare as arrays.

`DigestWritten` is not shipped.

---

## 8. Logging

Category = `Helpers`. APPID = `Json`.

Event IDs live in block 13500–13999 and count by 5. Probe stays 13500 / 13505. Every other live subcategory has its own enter / complete / reject IDs. Catalog `subcategory` values are the HelperLog names (`Probe`, `Session`, `Document`, `Query`, `Snapshot`, `Diff`, `Commit`, `Save`, `Jsonl`, `Guard`). Used through 13640 (SnapshotFailed 13630, DiffFailed 13635, CommitFailed 13640). Save, Commit, and Jsonl must not share 13515.

Register these subcategories (reuse existing names where they already exist):

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing suite |
| Session | Open, Create, Dispose, SessionId |
| Document | Parse, kind, path, byte length, record count |
| Query | Set path spelling (not the value). Get / TryGet / Record stay quiet. |
| Snapshot | Baseline taken |
| Diff | Operation count only |
| Commit | Commit / Revert / Cancel |
| Save | Path, bytes, collision, atomic |
| Jsonl | OpenJsonl, AppendRecord (index only), rewrite |

Sparse audit: session start, snapshot, commit/revert/cancel, save, Failed. Not a line per Get of a quiet settings key.

Never: payload body, PEM, long hex, `Exception` argument to HelperLog.

---

## 9. Hosts

This package is a class library plus tests. There is no `Vestigium.Helpers.Json.Demo` in this repo. A host initializes `VestigiumLogger` and calls `JsonCatalog.Register`. Tests inject `JsonTestHooks.ExportRoot` and never touch the real Desktop.

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
- Atomic write: dest is either the previous file or the complete new file. Over-cap write leaves dest previous or absent.
- `Parse` over cap throws. Non-seekable `Parse(Stream)` throws `ArgumentException`.
- `Open("*.jsonl")` rejects. `FromJson("null")` throws. JSONL `null` line is a record.
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
- RFC 6902 Apply / `move` / `copy` / `test`
- Mid-file JSONL splice without rewrite
- Source generators
- BOM, comments, trailing commas
- Admin rights, scheduler
- Host UI / gallery project
- `DigestWritten`

---

## 12. Roadmap

| Version | Item |
|---|---|
| **v1.0** | This document |
| **v1.0.1** | PR07 host-safety. Shipped. |
| **v1.1** | `DigestWritten` via Hashing if the owner buys it. Apply stays later. |
| **v1.2** | JSONL delete/replace record by index without a full in-memory list when file is large |
| **later** | JSON Schema, JSONPath, source-gen, Merge Patch |

Never here: being the suite logger; being FileIo; being Csv.

---

## 13. Acceptance

This SRS is **Accepted** on `main`.

1. This file, the Design, and the Developers Guide live under `Vestigium.Documentation/Vestigium/Helpers/Json/`.
2. §8 subcategories are registered through `JsonCatalog.Register` during host `VestigiumLogger.Initialize`.
3. A follow-up implementation PR can be reviewed against this text without inventing UniqueName, a second logger, Newtonsoft, or a Demo project.
