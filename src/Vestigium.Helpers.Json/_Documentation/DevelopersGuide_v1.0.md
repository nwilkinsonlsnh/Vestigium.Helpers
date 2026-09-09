# Vestigium.Helpers.Json — Developers Guide

**Document ID:** VEST-HLP-JSON-DEV-000  
**Version:** 1.0  
**Status:** Accepted with SRS v1.0.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md) is the phase map. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Json/`.

## What this library is

A helper for **payload** JSON and JSONL: settings files, host exports, external logs you need to query. System.Text.Json only. Pretty + camelCase. Pointer and dotted path. An edit session so a developer can see the RFC 6902 patch before disk changes.

It is not the audit logger. HelperLog still writes Vestigium JSONL under `%ProgramData%\\Vestigium\\Logs\\Json\\`. Do not implement a log writer here.

It is not FileIo. Do not copy trees, UniqueName, or recon. Json may read or write one path with a 64 KiB stream.

## Host a document

```csharp
var doc = JsonHelper.Open(@"D:\\Settings\\probe.json");
doc.Snapshot();
doc.Set("network.timeoutSeconds", 15);
var patch = doc.Diff();          // review in the gallery
doc.Commit();
doc.Save();                      // atomic replace
```

Create + default folder:

```csharp
var doc = JsonHelper.Create(JsonHelper.NewExportPath("probe-settings"));
doc.Set("/appId", "PingIQ");
doc.Commit();
doc.Save();
```

JSONL:

```csharp
var log = JsonHelper.OpenJsonl(path);
var code = log.Get<string>("[0].code");   // first line, member code
var rec = log.Record(0);
log.AppendRecord(JsonNode.Parse("""{"code":"ok"}"""));
log.Commit();
log.Save();
```

The class library never calls `VestigiumLogger.Initialize`. The host does (`HelperWpfHost` or `HelperLog.InitializeHost`). Category `Helpers`, APPID `Json`.

## NDJSON vs JSONL vs Logging

NDJSON and JSONL are the same idea: one JSON value per line. Vestigium.Logging uses that **shape** for the audit trail. This library uses the same shape for **your** files. Same grammar, different job. Do not append to `Logs\\`.

## Snapshot / Diff / Commit / Save

- **Snapshot** freezes the committed tree as the Diff baseline.
- **Set** mutates working memory only.
- **Diff** is RFC 6902 from baseline → working. Show it. Do not log values.
- **Commit** copies working into committed.
- **Save** writes committed (atomic). Disk does not see uncommitted Sets.
- **SaveWorking** is the escape hatch; log Warning.
- **Revert** restores working from Snapshot (or committed).
- **Cancel** drops working, no write.

## Paths

`/network/timeoutSeconds` and `network.timeoutSeconds` are the same member. `records[3].id` and `/records/3/id` are the same. Invalid syntax fails through `HelperGuard`.

## Writes

Default dest folder: `%DESKTOP%\\Vestigium\\Exports\\Json\\`. Tests replace that root. Probe never uses it.

`Save` of the opened path always replaces that file (atomic). `SaveAs` / `WriteFile` to another existing path **fails** unless `JsonCollision.Overwrite`. There is no UniqueName here — call FileIo in the host if you need a minted name.

## Locked (do not reopen in build mode)

- RFC 8259 only. No comments, no trailing commas, no BOM.
- Pretty + camelCase.
- Collision default Fail. AtomicWrite default true.
- JSONL: append + full rewrite. No mid-file splice in v1.
- HelperLog: paths and counts, never bodies.
- Phase 1 ships Identity, Probe, ToJson/FromJson, Parse, and the path parser. Session and files start at Phase 2.

## Sibling fences

Logging = audit JSONL. FileIo = trees. Hashing = digests. Csv = delimited tables. ClosedXml = workbooks. Json = documents and queries.
