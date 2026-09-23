# Vestigium.Helpers.Json — PR05 Backlog

**Document ID:** VEST-HLP-JSON-PR05-BL  
**Package:** `Vestigium.Helpers.Json` 1.0.0  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Json/001 -- Implementation Plan/PR05/`  
**Binding:** `Requirements_v1.0.md` wins on conflict. Design and the package README follow it.

## Intent

PR05 closes three holes left after Phase 5, adds the cheap half of roadmap v1.1 (node and file compare), and keeps the paper aligned with the code.

PR05 is not RFC 6902 apply, not `move` / `copy` / `test`, not async file APIs, not session locks, not a new export-directory injector, not a configurable stream buffer, not domain exception types, and not logging `Exception` objects.

Locked and left alone: Desktop export default (`%DESKTOP%\Vestigium\Exports\Json\` + `JsonTestHooks.ExportRoot`), 64 KiB streams, Diff-only add/remove/replace, one session / one owner, HelperLog never receives an `Exception` argument, library never calls `VestigiumLogger.Initialize`.

## Items

| ID | Priority | Kind | Description | Target files |
| :--- | :--- | :--- | :--- | :--- |
| **PR05-01** | **High** | Telemetry | Assign distinct Event IDs in block 13500–13999 (step 5) by subcategory + level. Register the same rows in `JsonEvents`, `JsonCatalog.Rows`, and `EventCatalog/json.json`. Pattern is the Csv catalog: enter / complete / reject / thrown per live subcategory. Keep Probe at 13500 and 13505. Catalog `subcategory` values must be the HelperLog names (`Probe`, `Session`, `Document`, `Query`, `Snapshot`, `Diff`, `Commit`, `Save`, `Jsonl`, `Guard`). Today `json.json` stamps `Json` on every row and `HelperLog.EventId` collapses non-Probe traffic onto 13510 / 13515 / 13520 / 13525. Do not attach exception objects. | `src/Vestigium.Helpers.Json/JsonEvents.cs`, `JsonCatalog.cs`, `HelperLog.cs`, `EventCatalog/json.json` |
| **PR05-02** | **High** | Contract | SRS §4.3 names `Parse(ReadOnlySpan<byte>)`. The façade does not have it. Add the overload beside `Parse(string)` and `Parse(Stream)`: RFC 8259, no session, reject empty span and UTF-8 BOM (`EF BB BF`) the same way the string path rejects `\uFEFF`. | `src/Vestigium.Helpers.Json/JsonHelper.cs` |
| **PR05-03** | **Medium** | JSONL | SRS §4.2: a truncated last line is Failed. `JsonIo.ReadJsonl` uses `StreamReader.ReadLine()`, which returns a final fragment. Reject a last line that is not a complete RFC 8259 value (`HelperLog.Reject` + `JsonException`). Empty lines still skip. Test: three valid lines plus a chopped `{` fails and does not return a partial array. | `src/Vestigium.Helpers.Json/JsonIO.cs`, `src/Vestigium.Helpers.Tests/JsonlTests.cs` |
| **PR05-04** | **Medium** | Feature | Roadmap v1.1 compare, Diff only. Promote `JsonPatch.Compare(JsonNode? from, JsonNode? to)` from `internal` to public. Add `JsonHelper.Compare(string leftPath, string rightPath)` that reads both payload files and returns the same add/remove/replace list. JSONL files compare as arrays. Log the path pair and op count; never bodies. No `Apply`. | `src/Vestigium.Helpers.Json/JsonPatch.cs`, `JsonHelper.cs` |
| **PR05-05** | **Medium** | Paper | SRS §5 says unknown Get “returns missing.” §7, Design §5, the package README, and `JsonSession.Get` throw `KeyNotFoundException`. Correct §5: Get throws; `TryGet` returns false. Record `Parse(ReadOnlySpan<byte>)` on the façade and Event IDs per subcategory. Do not unlock Desktop, 64 KiB, or exception-object logging. | `Vestigium.Documentation/Vestigium/Helpers/Json/002 -- Requirements Document/Requirements_v1.0.md`, `003 -- Design Document/Design_v1.0.md`, `src/Vestigium.Helpers.Json/README.md` |
| **PR05-06** | **Low** | Tests | `dotnet test --filter FullyQualifiedName~Json` covers Event IDs, span parse, truncated JSONL, and node/file compare. Save / Commit / Jsonl must not share 13515. Captured HelperLog lines still contain no payload bodies. Tests keep `JsonTestHooks.ExportRoot`. | `src/Vestigium.Helpers.Tests/JsonLoggingTests.cs`, `JsonCoverageTests.cs`, `JsonlTests.cs`, `JsonFileTests.cs` |

## Out of PR05

| Topic | Why |
| :--- | :--- |
| RFC 6902 `move` / `copy` / `test` and `Apply` | SRS §2 #9 and §11: Diff is add / remove / replace for the host to render. Apply is a later product. |
| `*Async` file and parse APIs | Sync IO is the shipped contract. Async is a later host-driven change, scoped to `JsonIo` file methods if it happens. |
| Export-directory injection / drop Desktop | SRS §2 #13 and G5. Tests already inject `JsonTestHooks.ExportRoot`. |
| Configurable `StreamBufferSize` | SRS §2 #14. |
| Read-locks on `JsonSession` | Type contract: one session, one owner. |
| Domain-specific path exceptions | Design §5 already lists `ArgumentException`, `JsonException`, `IOException`, `FileNotFoundException`. |
| `Exception.ToString()` on Error / Fatal | SRS §2 #16. Type + message in `detail` only. |
| JSON Schema, JSONPath, Merge Patch, source-gen, `DigestWritten` | Roadmap after v1.1 compare, or a later version. |

## Close gate

1. The six items above are implemented or explicitly cut by the owner.
2. `dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json` is green.
3. Event IDs stay inside 13500–13999 and count by 5.
4. No payload body, PEM, or `Exception` object in HelperLog writes.
