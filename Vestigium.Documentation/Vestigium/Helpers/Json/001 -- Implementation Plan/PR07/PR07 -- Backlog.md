# Vestigium.Helpers.Json — PR07 Backlog

**Document ID:** VEST-HLP-JSON-PR07-BL  
**Package:** `Vestigium.Helpers.Json` 1.0.1  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Json/001 -- Implementation Plan/PR07/`  
**Binding:** `Requirements_v1.0.md` wins on conflict. Design and the package README follow it.

## Intent

PR07 closed the host-safety holes PR06 left: document cap on every Parse door, `Open` of `.jsonl` fails closed, writes over cap never leave an oversized dest, export containment matches `SamePath`, seekable streams only, Snapshot / Diff / Commit Failed IDs, one catalog source, `FromJson` rejects JSON null, JSONL `null` line is a record.

PR07 is not RFC 6902 apply, not `move` / `copy` / `test`, not JSON Schema, not JSONPath, not Merge Patch, not source-gen, not async file APIs, not session locks, not a mid-file JSONL splice, not `DigestWritten`, and not a host UI.

## Defaults this wave locked

| Setting | Value |
| :--- | :--- |
| Document cap | 32 MiB (`JsonIo.MaxDocumentBytes`) — files and all three `Parse` doors |
| JSONL line cap | 1 MiB (`JsonIo.MaxJsonlLineBytes`) |
| Package version | `1.0.1` |
| Case-insensitive property names | Off |
| `Open("*.jsonl")` | Reject. Caller uses `OpenJsonl`. |
| `Parse(Stream)` | Seekable. `ArgumentException` otherwise. |
| EVENTID | 13500–13999, step 5, used through 13640 |

PR07-12 (`DigestWritten`) is cut.

PR06 paper lives under `000 -- Archived/001 -- Implementation Plan/PR06/`.

## Close gate

1. PR07-01 through PR07-07 and PR07-09 through PR07-11 are on `main`. PR07-12 is cut.
2. PR07-08 is `dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json` on the clone.
3. Event IDs stay inside 13500–13999 and count by 5.
4. No payload body, PEM, or `Exception` object in HelperLog writes.
5. Package README, SRS, Design, and Guide describe the shipped surface, not a host UI.
