# Vestigium.Helpers.Json — PR07 Backlog

**Document ID:** VEST-HLP-JSON-PR07-BL  
**Package:** `Vestigium.Helpers.Json` 1.0.0 (bump in PR07-05)  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Json/001 -- Implementation Plan/PR07/`  
**Binding:** `Requirements_v1.0.md` wins on conflict. Design and the package README follow it.

## Intent

PR05 shipped Event IDs, span parse, truncated JSONL, and public Compare. PR06 started host-safety (quiet Get, file caps, pack bits) and did not finish. PR07 closes the holes that still keep this below the Hashing / FileIo bar: contract matches code, fail-closed on the remaining footguns, one catalog source, tests that prove the claims, paper that no longer sells a Demo.

PR07 is not RFC 6902 apply, not `move` / `copy` / `test`, not JSON Schema, not JSONPath, not Merge Patch, not source-gen, not async file APIs, not session locks, not a mid-file JSONL splice, and not a host UI.

Hosts consume the class library. Tests under `src/Vestigium.Helpers.Tests/` are the contract surface.

Locked and left alone: Desktop export default (`%DESKTOP%\Vestigium\Exports\Json\` + `JsonTestHooks.ExportRoot`), 64 KiB stream buffer, Diff-only add/remove/replace, one session / one owner, HelperLog never receives an `Exception` argument, library never calls `VestigiumLogger.Initialize`, RFC 8259 only, System.Text.Json only, collision default Fail, UniqueName stays in FileIo.

## Defaults this wave locks unless the owner overrides

| Setting | Value |
| :--- | :--- |
| Document cap | 32 MiB (`JsonIo.MaxDocumentBytes`) — files **and** `Parse(Stream)` |
| JSONL line cap | 1 MiB (`JsonIo.MaxJsonlLineBytes`) |
| Package version | `1.0.1` if PR07-12 is cut; `1.1.0` if DigestWritten ships |
| Case-insensitive property names | Off |
| `Open("*.jsonl")` | Reject. Caller uses `OpenJsonl`. |

## What PR06 left on the table

These rows were claimed. Code on `main` (`058facbf`) does not finish them.

| PR06 ID | Status on `main` |
| :--- | :--- |
| PR06-01 quiet Get | Shipped. Leave alone. |
| PR06-02 `Open` of `.jsonl` fails closed | Not shipped. `Open` still `JsonIo.Read` and stamps `Kind = Json`. |
| PR06-03 size cap | Partial. File Open / OpenJsonl / Write honor the cap. `Parse(Stream)` does not call `EnsureStreamWithinCap`. That method is dead. |
| PR06-04 pack | Partial. README and catalog file pack. `Register` is hardcoded `Rows`. Version still `1.0.0`. |
| PR06-05 XML docs | Partial. `GenerateDocumentationFile` is on. |
| PR06-06 ordinal members | Shipped in Design + README. Leave alone. |
| PR06-07 DigestWritten | Not shipped. Still optional. |
| PR06-08 tests | Partial. Stream-cap test exists; implementation does not. No `Open("*.jsonl")` reject test. |
| PR06-09 paper | Not shipped. SRS §9 and the Guide still name a Demo. SRS §13 still points at `src/.../_Documentation/`. |

## Items

| ID | Priority | Kind | Description | Target files |
| :--- | :--- | :--- | :--- | :--- |
| **PR07-01** | **High** | Contract | Wire the document cap on `Parse(Stream)`. Call `JsonIo.EnsureStreamWithinCap` before parse. Log remaining bytes + cap, never the body. `JsonFileTests.Parse_stream_rejects_document_over_cap` already expects this. Decide in the same change whether `Parse(string)` / `Parse(ReadOnlySpan<byte>)` also honor the cap (UTF-8 byte count). If they stay uncapped, say so in the package README — do not leave the stream claim orphaned. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `JsonIO.cs`, `src/Vestigium.Helpers.Tests/JsonFileTests.cs` |
| **PR07-02** | **High** | Contract | `JsonHelper.Open` on a `.jsonl` path fails closed. Do not parse NDJSON as one RFC 8259 document and stamp `Kind = Json`. Point the caller at `OpenJsonl`. `Create("*.jsonl")` still starts an empty list. `OpenExport(..., Jsonl)` still goes through `OpenJsonl`. This is unfinished PR06-02. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `src/Vestigium.Helpers.Tests/JsonFileTests.cs` |
| **PR07-03** | **High** | Safety | Fail-closed writes over cap. Atomic path already writes a sibling temp, checks length, then `File.Move`. Non-atomic `Write` currently writes dest then throws — oversized dest remains. After this row, dest is either the previous file, absent, or a complete in-cap file. Log path + bytes + cap, never the body. | `src/Vestigium.Helpers.Json/JsonIO.cs`, `src/Vestigium.Helpers.Tests/JsonFileTests.cs` |
| **PR07-04** | **High** | Safety | Export-folder containment uses the same compare as `JsonIo.SamePath`. `ResolveExportFile` prefix check is `Ordinal`. On Windows that is a case-escape. Match `SamePath` (ordinal ignore case on Windows, ordinal elsewhere). Still reject `..` and empty stems. | `src/Vestigium.Helpers.Json/JsonIO.cs`, tests |
| **PR07-05** | **Medium** | Pack | One catalog source and a real version. `JsonCatalog.Register` stays the code path hosts call. `EventCatalog/json.json` must match `JsonCatalog.Rows` / `JsonEvents` exactly — same IDs, names, subcategories. Do not load the JSON at runtime unless Hashing already does. Copy the Hashing pack story (README, catalog content file, SourceLink already present). Bump `Version` per the table above. | `src/Vestigium.Helpers.Json/Vestigium.Helpers.Json.csproj`, `JsonCatalog.cs`, `JsonEvents.cs`, `EventCatalog/json.json` |
| **PR07-06** | **Medium** | Telemetry | Snapshot / Diff / Commit failures must not collapse onto `GuardFailed` (13620). Save / Jsonl / Document already have Failed IDs. Either add Failed rows at 13630+ (step 5, still inside 13500–13999) or write one Design sentence that those three share Guard. Pick one. Do not attach exception objects. | `JsonEvents.cs`, `HelperLog.cs`, `JsonCatalog.cs`, `EventCatalog/json.json`, `src/Vestigium.Helpers.Tests/JsonLoggingTests.cs` |
| **PR07-07** | **Medium** | Safety | Non-seekable streams: BOM and size. `RejectBom` and the cap both return when `!CanSeek`. Network / pipe input fail-open. Pick one mechanism: require seekable input, or count bytes while parsing and fail at cap / BOM. Document the choice. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `JsonIO.cs`, tests |
| **PR07-08** | **Medium** | Tests | Extend existing `Json*` tests. No second test project. Cover: `Parse(Stream)` over cap; `Open("*.jsonl")` rejects and does not return `Kind = Json`; non-atomic write over cap leaves no oversized dest; export stem cannot leave the folder on Windows case; mixed-kind Compare still throws; bodies and `EXCEPTION` objects still absent. Tests keep `JsonTestHooks.ExportRoot`. | `JsonFileTests.cs`, `JsonlTests.cs`, `JsonLoggingTests.cs`, `JsonCoverageTests.cs` |
| **PR07-09** | **Medium** | Paper | Align surviving paper with this wave. Close the PR06 plan as unfinished-then-superseded (archive under `000 -- Archived` if that is the house rule). Strike host-UI / `Vestigium.Helpers.Json.Demo` language from SRS §9 and the Developers Guide. Fix SRS §13 path: long-form docs live under `Vestigium.Documentation/Vestigium/Helpers/Json/`, not `src/Vestigium.Helpers.Json/_Documentation/`. SRS §8 Query: Get / TryGet / Record stay quiet; Set stays audited. Record caps on Parse(Stream), Open-jsonl reject, export containment, version bump. | `002 -- Requirements Document/Requirements_v1.0.md`, `003 -- Design Document/Design_v1.0.md`, `004 --Developers Guide/DevelopersGuide_v1.0.md`, package README |
| **PR07-10** | **Low** | Footgun | `FromJson<T>` of a reference type on JSON `null`. `Parse` rejects null as a document root. `FromJson` returns `value!`. Two doors, two rules. Reject with `JsonException` or document the typed-null exception. Do not leave `!`. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `src/Vestigium.Helpers.Tests/JsonPureTests.cs` |
| **PR07-11** | **Low** | JSONL | JSON null as a JSONL line vs document-null rule. `ReadJsonl` accepts a `null` line. `AppendRecord` rejects null. `Parse` rejects null root. Pick one product rule and test it. | `JsonIO.cs`, `JsonSession.cs`, `JsonlTests.cs` |
| **PR07-12** | **Cut unless owner buys** | Integrity | Optional SRS v1.1 leftover: `JsonHelper.DigestWritten(path)` calls Hashing on the **written UTF-8 file bytes** after Save / WriteFile. Do not grow `Hash*` on Json. Log path + algorithm + digest length, never payload. If it ships, package version is `1.1.0` and Json takes `Vestigium.Helpers.Hashing` as a package reference the way FileIo did. If it does not ship, cut this row in public when PR07 closes. | `JsonHelper.cs`, `.csproj`, tests |

## Out of PR07

| Topic | Why |
| :--- | :--- |
| RFC 6902 `move` / `copy` / `test` and `Apply` | SRS §2 #9 and §11. Diff is add / remove / replace for the host to render. |
| Array-diff quality (LCS vs index-aligned) | Ugly patches on insert. Not a host-safety bug. |
| `*Async` file and parse APIs | Sync IO is the shipped contract. |
| Export-directory injection / drop Desktop | SRS §2 #13 and G5. Tests already inject `JsonTestHooks.ExportRoot`. |
| Configurable `StreamBufferSize` | SRS §2 #14. Stays 64 KiB. |
| Read-locks on `JsonSession` | Type contract: one session, one owner. |
| Domain-specific path exceptions | Design §5 already lists the BCL types. |
| `Exception.ToString()` on Error / Fatal | SRS §2 #16. Type + message in `detail` only. |
| JSON Schema, JSONPath, Merge Patch, source-gen | Roadmap after v1.1. |
| Mid-file JSONL splice without rewrite | SRS v1.2. |
| Shared HelperLog extraction | Suite-wide. Not a Json PR. |
| Host UI / gallery project | Class library + tests. Not in this repo's Json wave. |

## Close gate

1. PR07-01 through PR07-09 are implemented. PR07-10 and PR07-11 are implemented or explicitly cut. PR07-12 is implemented or explicitly cut by the owner.
2. `dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json` is green.
3. Event IDs stay inside 13500–13999 and count by 5.
4. No payload body, PEM, or `Exception` object in HelperLog writes.
5. `Parse(Stream)` over cap throws `JsonException` and does not return a node.
6. `Open` of a `.jsonl` path does not return a `Json` session.
7. Package README, SRS, Design, and Guide describe the shipped surface, not a host UI.
