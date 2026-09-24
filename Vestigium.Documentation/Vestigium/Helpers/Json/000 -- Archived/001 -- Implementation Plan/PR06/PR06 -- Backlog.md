# Vestigium.Helpers.Json — PR06 Backlog

**Document ID:** VEST-HLP-JSON-PR06-BL  
**Package:** `Vestigium.Helpers.Json` 1.0.0 (bump in PR06-04)  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Json/001 -- Implementation Plan/PR06/`  
**Binding:** `Requirements_v1.0.md` wins on conflict. Design and the package README follow it.

## Intent

PR05 shipped Event IDs, span parse, truncated JSONL, and public Compare. PR06 makes the library host-safe to take as a dependency: contract matches code, fail-closed on the two footguns, a size cap, packable docs, and paper that no longer contradicts `main`.

PR06 is not RFC 6902 apply, not `move` / `copy` / `test`, not JSON Schema, not JSONPath, not Merge Patch, not source-gen, not async file APIs, not session locks, not a mid-file JSONL splice, and not a host UI.

Hosts consume the class library. Tests under `src/Vestigium.Helpers.Tests/` are the contract surface.

Locked and left alone: Desktop export default (`%DESKTOP%\Vestigium\Exports\Json\` + `JsonTestHooks.ExportRoot`), 64 KiB stream buffer, Diff-only add/remove/replace, one session / one owner, HelperLog never receives an `Exception` argument, library never calls `VestigiumLogger.Initialize`, RFC 8259 only, System.Text.Json only, collision default Fail, UniqueName stays in FileIo.

## Defaults this wave locks unless the owner overrides

| Setting | Value |
| :--- | :--- |
| Document cap | 32 MiB (`JsonIo.MaxDocumentBytes`) |
| JSONL line cap | 1 MiB (`JsonIo.MaxJsonlLineBytes`) |
| Package version | `1.0.1` if PR06-07 is cut; `1.1.0` if DigestWritten ships |
| Case-insensitive property names | Off. Opt-in only if PR06-06 adds the flag |

## Items

| ID | Priority | Kind | Description | Target files |
| :--- | :--- | :--- | :--- | :--- |
| **PR06-01** | **High** | Contract | Get is quiet. `JsonSession.Get` throws `KeyNotFoundException` / `InvalidOperationException` and must not call `HelperLog.Reject`. `TryGet` and `Record` stay silent. Set / Commit / Save / Failed Open stay audited. SRS §8 and the Guide already require this; the method does not. | `src/Vestigium.Helpers.Json/JsonSession.cs`, `src/Vestigium.Helpers.Tests/JsonLoggingTests.cs` |
| **PR06-02** | **High** | Contract | `JsonHelper.Open` on a `.jsonl` path fails closed. Do not parse NDJSON as one RFC 8259 document and stamp `Kind = Json`. Point the caller at `OpenJsonl`. `Create("*.jsonl")` still starts an empty list. `OpenExport(..., Jsonl)` still goes through `OpenJsonl`. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `src/Vestigium.Helpers.Tests/JsonFileTests.cs` |
| **PR06-03** | **High** | Safety | Cap payload size. `Open` / `OpenJsonl` / `Parse(Stream)` / `Write` reject above `MaxDocumentBytes`. JSONL reader rejects a line above `MaxJsonlLineBytes`. Log path + bytes or `index=`, never the body. Defaults: 32 MiB document, 1 MiB line. | `src/Vestigium.Helpers.Json/JsonIO.cs`, `JsonHelper.cs`, `src/Vestigium.Helpers.Tests/JsonFileTests.cs`, `JsonlTests.cs` |
| **PR06-04** | **High** | Pack | Make the nupkg a real package. Public XML docs (see PR06-05). README already packs. Event catalog must be loadable by `JsonCatalog.Register` from the package, not only from a sibling content file in this repo. Add SourceLink / snupkg if the other Helpers packages already do; do not invent a new pack story. Bump `Version` per the table above. | `src/Vestigium.Helpers.Json/Vestigium.Helpers.Json.csproj`, `JsonCatalog.cs`, `EventCatalog/json.json` |
| **PR06-05** | **Medium** | API docs | `///` on every public type and member in this project. Remove `CS1591` from this csproj `NoWarn`. Do not flip repo-wide `TreatWarningsAsErrors`. | Public types under `src/Vestigium.Helpers.Json/` |
| **PR06-06** | **Medium** | Footgun | Writes use camelCase. Reads are case-sensitive. State that in the package README and Design in one sentence. Do not default to case-insensitive. Optional: `JsonReadOptions.PropertyNameCaseInsensitive` default `false`, tested opt-in. | `src/Vestigium.Helpers.Json/README.md`, `003 -- Design Document/Design_v1.0.md`, `JsonOptions.cs` only if the flag is added |
| **PR06-07** | **Medium** | Integrity | Optional SRS v1.1 leftover: `JsonHelper.DigestWritten(path)` calls Hashing on the **written UTF-8 file bytes** after Save / WriteFile. Do not grow `Hash*` on Json. Log path + algorithm + digest length, never payload. Cut this row if pack-only is the publish goal. If it ships, package version is `1.1.0`. | `src/Vestigium.Helpers.Json/JsonHelper.cs`, `Vestigium.Helpers.Json.csproj`, tests |
| **PR06-08** | **Medium** | Tests | Extend existing `Json*` tests. No second test project. Cover: Get-miss produces no HelperLog Query Failed line; `Open("*.jsonl")` rejects; document and line caps; mixed-kind Compare still throws; bodies and `EXCEPTION` objects still absent. Tests keep `JsonTestHooks.ExportRoot`. | `src/Vestigium.Helpers.Tests/JsonLoggingTests.cs`, `JsonFileTests.cs`, `JsonlTests.cs`, `JsonCoverageTests.cs` |
| **PR06-09** | **Low** | Paper | Align surviving paper with this wave. Close the PR05 plan Status. Strike host-UI language from SRS §9 and the Developers Guide. Fix SRS §13 path: long-form docs live under `Vestigium.Documentation/Vestigium/Helpers/Json/`, not `src/Vestigium.Helpers.Json/_Documentation/`. Record caps, Open-jsonl reject, quiet Get, and the version bump. | `002 -- Requirements Document/Requirements_v1.0.md`, `003 -- Design Document/Design_v1.0.md`, `004 --Developers Guide/DevelopersGuide_v1.0.md`, `001 -- Implementation Plan/PR05/PR05 -- Implementation Plan.md`, package README |

## Out of PR06

| Topic | Why |
| :--- | :--- |
| RFC 6902 `move` / `copy` / `test` and `Apply` | SRS §2 #9 and §11. Diff is add / remove / replace for the host to render. |
| `*Async` file and parse APIs | Sync IO is the shipped contract. |
| Export-directory injection / drop Desktop | SRS §2 #13 and G5. Tests already inject `JsonTestHooks.ExportRoot`. |
| Configurable `StreamBufferSize` | SRS §2 #14. Stays 64 KiB. |
| Read-locks on `JsonSession` | Type contract: one session, one owner. |
| Domain-specific path exceptions | Design §5 already lists the BCL types. |
| `Exception.ToString()` on Error / Fatal | SRS §2 #16. Type + message in `detail` only. |
| JSON Schema, JSONPath, Merge Patch, source-gen | Roadmap after v1.1. |
| Mid-file JSONL splice without rewrite | SRS v1.2. |
| Shared HelperLog extraction | Suite-wide. Not a Json PR. |
| Host UI / gallery project | Class library + tests. Not in this repo’s Json wave. |

## Close gate

1. PR06-01 through PR06-06 and PR06-08–09 are implemented. PR06-07 is implemented or explicitly cut by the owner.
2. `dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Json` is green.
3. Event IDs stay inside 13500–13999 and count by 5.
4. No payload body, PEM, or `Exception` object in HelperLog writes.
5. Get of a missing path throws and produces no Query Failed log line.
6. `Open` of a `.jsonl` path does not return a `Json` session.
7. Package README and public XML docs describe the shipped surface, not a host UI.
