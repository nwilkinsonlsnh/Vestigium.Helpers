# Vestigium.Helpers.Json — PR07 Implementation Plan

**Document ID:** VEST-HLP-JSON-PLAN-PR07  
**Status:** Open  
**Date:** 24 September 2026  
**Backlog:** [`PR07 -- Backlog.md`](PR07%20--%20Backlog.md)  
**Binding:** Requirements win. Do these in order. Commit: `Json PR07: <step>`.

PR07-12 (`DigestWritten`) is cut.

## Implementation table

| Order | ID | Do |
| ---: | :--- | :--- |
| 1 | PR07-01 | Wire `MaxDocumentBytes` on all three `Parse` overloads. |
| 2 | PR07-02 | `Open("*.jsonl")` rejects. Use `OpenJsonl`. |
| 3 | PR07-03 | Write over cap: dest is previous file or absent. No leftover `*.tmp`. |
| 4 | PR07-04 | Export path containment uses `SamePath` compare. |
| 5 | PR07-07 | Non-seekable `Parse(Stream)` throws `ArgumentException`. |
| 6 | PR07-06 | Add SnapshotFailed 13630, DiffFailed 13635, CommitFailed 13640. |
| 7 | PR07-05 | `json.json` matches `Rows`. Version `1.0.1`. |
| 8 | PR07-10 | `FromJson("null")` throws `JsonException`. |
| 9 | PR07-11 | JSONL `null` line is a legal record. `AppendRecord` still rejects C# null. |
| 10 | PR07-08 | `dotnet test --filter FullyQualifiedName~Json` green. |
| 11 | PR07-09 | SRS / Design / Guide / README match the code. Archive PR06. |
| — | PR07-12 | Cut. |
