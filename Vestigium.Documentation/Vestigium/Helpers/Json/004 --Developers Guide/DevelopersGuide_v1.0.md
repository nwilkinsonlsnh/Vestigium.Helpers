# Vestigium.Helpers.Json — Developers Guide

**Document ID:** VEST-HLP-JSON-DEV-000  
**Version:** 1.0  
**Status:** Accepted with SRS v1.0. PR07 is the shipped engine.  
**Date:** 24 September 2026

[`Requirements_v1.0.md`](../002%20--%20Requirements%20Document/Requirements_v1.0.md) is the contract. Implementation lives in `src/Vestigium.Helpers.Json/`. Long-form paper lives under `Vestigium.Documentation/Vestigium/Helpers/Json/`.

Package: `Vestigium.Helpers.Json` 1.0.1. EVENTID 13500–13999, used through 13640.

## What this library is

A helper for **payload** JSON and JSONL: settings files, host exports, external logs you need to query. `System.Text.Json` only. Pretty + camelCase. Pointer and dotted path. An edit session so a developer can see the RFC 6902 patch before disk changes.

It is not the audit logger. HelperLog still writes Vestigium JSONL under `%ProgramData%\\Vestigium\\Logs\\{host-APPID}\\`. Do not implement a log writer here.

It is not FileIo. Do not copy trees, UniqueName, or recon. Json may read or write one path with a 64 KiB stream.

There is no `Vestigium.Helpers.Json.Demo` in this repo. Hosts consume the class library. Tests under `src/Vestigium.Helpers.Tests/` are the contract surface.

## Host a document

```csharp
var doc = JsonHelper.Open(@"D:\\Settings\\probe.json");
doc.Snapshot();
doc.Set("network.timeoutSeconds", 15);
var patch = doc.Diff();          // RFC 6902 — show it; do not log values
doc.Commit();
doc.Save();                      // atomic replace of the opened path
```

Create + default folder:

```csharp
var doc = JsonHelper.Create(JsonHelper.NewExportPath("probe-settings"));
doc.Set("/appId", "PingIQ");
doc.Commit();
doc.Save();
```

JSONL (one RFC 8259 value per line; Save rewrites the whole file compact):

```csharp
var log = JsonHelper.OpenJsonl(path);
var code = log.Get<string>("[0].code");   // first line, member code
var rec = log.Record(0);
log.AppendRecord(JsonNode.Parse("""{"code":"ok"}"""));
log.Commit();
log.Save();
```

`Create("*.jsonl")` starts an empty list. `Open` stays a single RFC 8259 document; a `.jsonl` path is rejected. `OpenJsonl` is explicit. `OpenExport(stem, JsonDocumentKind.Jsonl)` goes through `OpenJsonl`.

The class library never calls `VestigiumLogger.Initialize`. The host does. Category `Helpers`, APPID `Json`.

## Fail-closed

- Document cap is 32 MiB on Open / OpenJsonl / Write and on `Parse(string|Stream|ReadOnlySpan<byte>)`.
- `Parse(Stream)` requires a readable, seekable stream.
- Write over cap: dest is the previous file or absent. No leftover `*.tmp`.
- `FromJson("null")` throws `JsonException`. JSONL `null` line is a legal record. `AppendRecord` rejects C# null.
- Export stems stay under the export folder. Containment uses the same compare as `SamePath`.

## NDJSON vs JSONL vs Logging

NDJSON and JSONL are the same idea: one JSON value per line. Vestigium.Logging uses that **shape** for the audit trail. This library uses the same shape for **your** files. Same grammar, different job. Do not append to `Logs\\`. Json may **read** a Vestigium log file as just another JSONL payload.

## Snapshot / Diff / Commit / Save

- **Snapshot** freezes the committed tree as the Diff baseline.
- **Set** / **AppendRecord** mutate working memory only.
- **Diff** is RFC 6902 from baseline → working. Show it. Do not log values.
- **Commit** copies working into committed.
- **Save** writes committed (always a sibling temp, then `File.Move`). Disk does not see uncommitted Sets.
- **SaveWorking** is the escape hatch; log Warning.
- **Revert** restores working from Snapshot (or committed).
- **Cancel** drops working, no write.

## Paths

`/network/timeoutSeconds` and `network.timeoutSeconds` are the same member. `records[3].id` and `/records/3/id` are the same. JSONL records use `[0].code`. Object member compare is ordinal: `network.Timeout` and `network.timeout` are two keys. Hosts that want Windows-ish keys call FileIo UniqueName, not this package. Invalid syntax fails through `HelperGuard` / Query Reject. Get of a missing path throws; `TryGet` returns false. Set creates object parents; it does not create through a primitive or grow arrays.

## Writes

Default dest folder: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests replace that root via `JsonTestHooks.ExportRoot`. Probe never uses it.

`Save` of the opened path always replaces that file (sibling temp, then `File.Move` overwrite). `SaveAs` / `WriteFile` to another existing path **fails** unless `JsonCollision.Overwrite`. There is no UniqueName here — call FileIo in the host if you need a minted name.

JSONL Save is compact, one value + `\\n` per record, UTF-8 no BOM. Pretty print would break the line grammar. Empty lines are skipped on read; `\\n` and `\\r\\n` are accepted; a truncated last line is Failed.

## Sparse HelperLog

Audit these: session start (Create / Open / Dispose), Snapshot, Diff (`ops=` only), Commit / Revert / Cancel, Save (path, bytes, collision, atomic), Jsonl (`OpenJsonl records=`, `AppendRecord index=`, rewrite), Document counts on `ToJson` / `FromJson` / `Parse` / Open, Set path spelling, Failed.

Do **not** log a quiet Get / TryGet / Record of a settings key. Never payload bodies, field values, PEM, long hex, or `Exception` objects. `HelperLog.Trap` is for unexpected failures only; expected `ArgumentException` / `JsonException` / `IOException` rethrow without Trap.

Failed IDs: Snapshot 13630, Diff 13635, Commit 13640. They do not collapse onto Guard 13620.

## Probe

`Probe` serializes and parses an in-memory payload. It must not write the Desktop export folder, must not open a durable session, and tests pin its HelperLog directory under `%TEMP%`.

## Locked (do not reopen in build mode)

- RFC 8259 only. No comments, no trailing commas, no BOM.
- Pretty + camelCase for `.json`. JSONL Save is compact.
- Collision default Fail. Writes always go through a sibling temp. 64 KiB streams.
- JSONL: append + full rewrite. No mid-file splice in v1.
- HelperLog: paths and counts, never bodies. Get is quiet.
- PR07 is the shipped engine: caps, Open-jsonl reject, seekable Parse, catalog 1.0.1, `FullyQualifiedName~Json` on the clone.

## Sibling fences

Logging = audit JSONL. FileIo = trees. Hashing = digests. Csv = delimited tables. ClosedXml = workbooks. Json = documents and queries.
