# Vestigium.Helpers.Json — Developers Guide

**Document ID:** VEST-HLP-JSON-DEV-000  
**Version:** 1.0  
**Status:** Accepted with SRS v1.0. Phase 5 hardening is the running engine.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md) is the phase map. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Json/`.

## What this library is

A helper for **payload** JSON and JSONL: settings files, host exports, external logs you need to query. `System.Text.Json` only. Pretty + camelCase. Pointer and dotted path. An edit session so a developer can see the RFC 6902 patch before disk changes.

It is not the audit logger. HelperLog still writes Vestigium JSONL under `%ProgramData%\\Vestigium\\Logs\\Json\\`. Do not implement a log writer here.

It is not FileIo. Do not copy trees, UniqueName, or recon. Json may read or write one path with a 64 KiB stream.

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

`Create("*.jsonl")` starts an empty list. `Open` stays a single RFC 8259 document; `OpenJsonl` is explicit. `OpenExport(stem, JsonDocumentKind.Jsonl)` goes through `OpenJsonl`.

The class library never calls `VestigiumLogger.Initialize`. The host does (`HelperWpfHost` or `HelperLog.InitializeHost`). Category `Helpers`, APPID `Json`.

## NDJSON vs JSONL vs Logging

NDJSON and JSONL are the same idea: one JSON value per line. Vestigium.Logging uses that **shape** for the audit trail. This library uses the same shape for **your** files. Same grammar, different job. Do not append to `Logs\\`. Json may **read** a Vestigium log file as just another JSONL payload.

## Snapshot / Diff / Commit / Save

- **Snapshot** freezes the committed tree as the Diff baseline.
- **Set** / **AppendRecord** mutate working memory only.
- **Diff** is RFC 6902 from baseline → working. Show it. Do not log values.
- **Commit** copies working into committed.
- **Save** writes committed (atomic). Disk does not see uncommitted Sets.
- **SaveWorking** is the escape hatch; log Warning.
- **Revert** restores working from Snapshot (or committed).
- **Cancel** drops working, no write.

## Paths

`/network/timeoutSeconds` and `network.timeoutSeconds` are the same member. `records[3].id` and `/records/3/id` are the same. JSONL records use `[0].code`. Invalid syntax fails through `HelperGuard` / Query Reject. Get of a missing path throws; `TryGet` returns false. Set creates object parents; it does not create through a primitive or grow arrays.

## Writes

Default dest folder: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests replace that root via `JsonTestHooks.ExportRoot`. Probe never uses it.

`Save` of the opened path always replaces that file (atomic sibling temp, then `File.Move` overwrite). `SaveAs` / `WriteFile` to another existing path **fails** unless `JsonCollision.Overwrite`. There is no UniqueName here — call FileIo in the host if you need a minted name.

JSONL Save is compact, one value + `\\n` per record, UTF-8 no BOM. Pretty print would break the line grammar. Empty lines are skipped on read; `\\n` and `\\r\\n` are accepted; a truncated last line is Failed.

## Sparse HelperLog

Audit these: session start (Create / Open / Dispose), Snapshot, Diff (`ops=` only), Commit / Revert / Cancel, Save (path, bytes, collision, atomic), Jsonl (`OpenJsonl records=`, `AppendRecord index=`, rewrite), Document counts on `ToJson` / `FromJson` / `Parse` / Open, Set path spelling, Failed.

Do **not** log a quiet Get / TryGet / Record of a settings key. Never payload bodies, field values, PEM, long hex, or `Exception` objects. `HelperLog.Trap` is for unexpected failures only; expected `ArgumentException` / `JsonException` / `IOException` rethrow without Trap.

## Probe

`Probe` serializes and parses an in-memory demo payload. It must not write the Desktop export folder, must not open a durable session, and tests pin its HelperLog directory under `%TEMP%`.

## Gallery

`Vestigium.Helpers.Json.Demo` hosts APPID Json. Tabs: Overview, Settings (Get/Set/Diff/Commit/Save/Cancel), Jsonl (enumerate + append), Export, JSONL audit pane. Demo files `probe-settings.json` and `payload-records.jsonl` live under the export folder.

## Locked (do not reopen in build mode)

- RFC 8259 only. No comments, no trailing commas, no BOM.
- Pretty + camelCase for `.json`. JSONL Save is compact.
- Collision default Fail. AtomicWrite default true. 64 KiB streams.
- JSONL: append + full rewrite. No mid-file splice in v1.
- HelperLog: paths and counts, never bodies. Get is quiet.
- Phase 5 is the running engine: sparse log, Probe `%TEMP%` only, Guide matches the code, `FullyQualifiedName~Json` green.

## Sibling fences

Logging = audit JSONL. FileIo = trees. Hashing = digests. Csv = delimited tables. ClosedXml = workbooks. Json = documents and queries.
