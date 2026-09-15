# Vestigium.Helpers.Xml — Requirements Specification

**Document ID:** VEST-HLP-XML-SRS-000  
**Version:** 1.0  
**Status:** Draft — ready for acceptance  
**Date:** 15 September 2026  
**Package:** `Vestigium.Helpers.Xml`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Supersedes:** Skeleton dated 7 September 2026 (same document ID; no requirement dropped)

If implementation and this file disagree, this file wins.

This library is not Vestigium.Logging and is not FileIo and is not Json. Logging writes the suite audit trail (JSON Lines under `%ProgramData%\Vestigium\Logs\{APPID}\`). FileIo moves trees. Json loads payload `.json` / `.jsonl`. Xml loads, searches, creates, updates, and saves **payload XML documents** (settings, OMA DDF, UPnP SCPD, Windows RE, diagnostic dumps) so an operator can ask a file a question and see what changed before it hits disk.

`XmlHelper` today ships Identity + Probe only. Do not grow the public API until this document is Accepted.

---

## 0. How to read this document

It records:

- one façade (`XmlHelper`) for Identity, Probe, parse, and file/stream open
- one session (`XmlSession`) per opened document with working vs committed state
- search by local name, attribute, text, and XPath 1.0
- Snapshot → Set/Insert/Delete → Diff → Commit or Revert → Save or Cancel
- RFC 7303 media types and encoding determination
- HelperLog only; Category `Helpers`; APPID `Xml`; subcategories in §10
- platform parser only (`XmlReader` + `XDocument`); no custom XML 1.0 grammar
- no FileIo verbs (copy, mirror, UniqueName, recon)
- no second logger (do not write `%ProgramData%\Vestigium\Logs\`)
- gold fixtures shipped as MSBuild Content + EmbeddedResource and seeded into `%USERPROFILE%\Documents\Xml`

---

## 1. Purpose

Give every Vestigium host one way to read an XML payload, find a node, change it in memory, review the change list, and either commit+save or revert.

```csharp
var doc = XmlHelper.Open(path);                 // RFC 7303 safe load
doc.Snapshot();
doc.SetText("//u:action/u:name", "MagicOff");
doc.SetAttribute("/WindowsRE/InstallState", "state", "1");
var changes = doc.Diff();                       // path + op; never log values
if (accept)
{
    doc.Commit();
    doc.Save();                                 // atomic replace of the opened path
}
else
    doc.Cancel();                               // drop working; disk unchanged
```

This is not Logging. Logging still owns the audit JSONL. This is not FileIo. FileIo still owns trees. This is not Json. Json still owns RFC 8259 payloads. Hashing may digest bytes Xml already wrote. Xml does not grow `Hash*`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Engine | `System.Xml` / `System.Xml.Linq` (`XmlReader` + `XDocument`). No custom parser. No third-party XML package. |
| 2 | TFM | `net10.0`. Not Windows-only. Demo gallery remains `net10.0-windows`. |
| 3 | Product | Payload XML documents and queries. Not the Vestigium audit logger. Not a DDF/UPnP domain SDK. |
| 4 | Media types | RFC 7303. Prefer `application/xml`. `text/xml` is an alias of `application/xml`. Recognize `+xml`. `application/xml-dtd` is for DTD subsets only. |
| 5 | Encoding | RFC 7303 §3 order: BOM, then caller MIME charset, then XML declaration, then UTF-8. BOM wins conflicts. Write UTF-8, **no BOM**, unless `XmlWriteOptions.EmitBom` is explicit. UTF-32 rejected on write. |
| 6 | Shape | Static `XmlHelper` (Identity, Probe, Parse, Open/Create/Write) **and** `XmlSession` for an opened document. |
| 7 | Query | Local name + optional namespace URI; attribute name/value; element text; XPath 1.0 with a namespace manager. No XPath 2.0 / XQuery / XSLT in v1. |
| 8 | Edit lifecycle | Read → Snapshot (baseline = committed) → Set/Insert/Delete on working → Diff → Commit or Revert → Save or Cancel. Save writes the **committed** tree unless `SaveWorking` is explicit. Cancel drops working and does not write. |
| 9 | Diff | In-memory change list (path, op, node local-name). HelperLog records path + op count, never old/new bodies. Not XML C14N. |
| 10 | Multi-document | A stream of consecutive XML documents (the `diagwrn.xml` shape) is first-class via `OpenMulti` / `XmlDocumentStream`. Single-document `Open` of that file is Failed. v1 does not rewrite a multi-document stream. |
| 11 | Collision | `XmlCollision.Fail` default on `SaveAs` / `WriteFile` when dest exists. `Overwrite` is explicit. UniqueName is **not** in this library. |
| 12 | Atomic write | `AtomicWrite = true` default. Write a sibling temp in the same directory, flush, then replace dest. |
| 13 | Default export folder | If the host omits a directory: `%DESKTOP%\Vestigium\Exports\Xml\`. Tests inject a temp root and never touch the real Desktop. Probe stays `%TEMP%` only. |
| 14 | Fixture work area | Gold files live in the test project as Content + EmbeddedResource. `XmlContentSeeder` copies them to `%USERPROFILE%\Documents\Xml` (override `VESTIGIUM_XML_TESTAREA`). Mutating tests use `Documents\Xml\_scratch\{id}\`. |
| 15 | Safety | DTD processing prohibited. `XmlResolver` is null. No external entity fetch. Size cap default large enough for `diagwrn.xml` (~1 MiB) but not unbounded. |
| 16 | DOCTYPE | Capture and preserve the prologue text when possible. Do not resolve PUBLIC/SYSTEM identifiers. Trusted local DTD validation is not v1. |
| 17 | Comments / whitespace | Keep comments on load. Indent off unless `XmlWriteOptions.Indent`. Do not claim byte-identical round-trip (attribute order and `<a/>` vs `<a></a>` follow the platform DOM). |
| 18 | Logging door | `HelperLog` only. Category `Helpers`. APPID `Xml` (`HelperLog.AppIds.Xml`). Library never calls `VestigiumLogger.Initialize`. |
| 19 | Log contents | Paths, node counts, byte lengths, XPath spellings, op counts. **Never** element text values, attribute values, Base64 blobs, or `Exception` objects. |
| 20 | Siblings | May call Hashing for a digest of **written UTF-8 bytes**. Must not grow `Hash*`. Must not reference Csv, ClosedXml, FileIo, Json, Charts, Analytics (v1). Must not write under `Logs\`. |
| 21 | Schema / XSD / XSLT | **Not v1.** Roadmap. |
| 22 | Current code gate | `XmlHelper` stays Identity + Probe until Status is Accepted. |

---

## 3. Goals

**G1.** One façade (`XmlHelper`) owns Identity, Probe, Parse, and Open/Create.  
**G2.** One `XmlSession` per document owns working vs committed trees, Snapshot, Diff, Commit, Revert, Save, Cancel.  
**G3.** Search by local name, attribute, text, and XPath 1.0. Default xmlns is bindable.  
**G4.** RFC 7303 encoding and media-type helpers.  
**G5.** Safe load: no DTD resolve, no network, no XXE.  
**G6.** Multi-document read for concatenated dumps.  
**G7.** Collision default Fail; Overwrite explicit; writes atomic.  
**G8.** Logs meet ALCOA+ through HelperLog. Category `Helpers`, APPID `Xml`.  
**G9.** `Probe` is `%TEMP%` only. It must not write the Desktop, must not seed Documents, and must not open a durable session.  
**G10.** Gold fixtures always exist: Content + EmbeddedResource seed `Documents\Xml`.  
**G11.** Hosts can review Diff in-memory and revert before anything hits disk.

---

## 4. Normative references

| Reference | Role |
|---|---|
| RFC 7303 — XML Media Types (July 2014) | Media types, charset, BOM, `+xml`. Obsoletes RFC 3023. |
| W3C XML 1.0 (5th ed.) | Well-formedness. |
| W3C Namespaces in XML 1.0 | Prefix / default namespace. |
| W3C XPath 1.0 | Search language. |
| This suite's HelperLog / HelperGuard contract | Logging and reject path. |

Xml 1.1 is accepted on read when the declaration says so; write stays 1.0 unless the source declaration is 1.1.

---

## 5. Corpus (gold fixtures)

These six files are the first official corpus. They are diverse on purpose. If the helpers work on all six, they will work on most host XML we will see.

| File | Size class | Root / shape | Why it matters |
|---|---|---|---|
| `DevicePreparationDDF.xml` | ~21 KB | `MgmtTree` (OMA DDF) | PUBLIC DOCTYPE to a remote DTD. Nested `Node` trees. MSFT namespace. Autopilot Device Preparation CSP. |
| `DMClient_DDF.xml` | ~79 KB | `MgmtTree` with User + Device `DMClient` paths | Large DDF. Empty `NodeName` slots. `Unenroll` Exec. Deep FirstSync / Recovery / ConfigRefresh. |
| `diagwrn.xml` | ~633 KB | Concatenated ADO rowset documents | Not one well-formed document. `setupcln.dll` / `cleanmgr` dump. Forces MultiDocument + streaming search. |
| `ipcfg.xml` | ~13 KB | UPnP `scpd` | Default xmlns. `actionList` + `serviceStateTable`. Classic Create/Update target. |
| `osinfo.xml` | ~0.8 KB | UPnP `scpd` | Smallest happy path. Trailing `<!-- no-op -->`. `MagicOn` action. |
| `ReAgent.xml` | ~1.1 KB | `WindowsRE` | UTF-8 declaration, possible BOM. Attribute-heavy empty elements. GUIDs and offsets. |

Fixture facts that drive requirements:

- DDF files declare OMA DM DDF 1.2 via PUBLIC + `http://www.openmobilealliance.org/...dtd`. A naïve `XmlDocument` load will hit the network. Tests must prove we do not.
- `diagwrn.xml` is several complete documents back-to-back. `Open` throws. `OpenMulti` yields documents.
- UPnP files use `xmlns="urn:schemas-upnp-org:service-1-0"`. XPath without a bind finds nothing.
- `ReAgent.xml` is attribute-centric; DDF is element-centric. Both are first-class.

---

## 6. Fixture model — Content is gold, Documents\\Xml is the work area

The sample documents are part of the test project, not an external folder we hope exists.

### 6.1 Why both Content and EmbeddedResource

- **Content** + `CopyToOutputDirectory=PreserveNewest`: files land next to the test DLL as `Content\Xml\*.xml`. Easy to open while debugging.
- **EmbeddedResource**: the same bytes live inside the assembly. If the output Content folder is missing (shadow-copy, unusual test host), the seeder still extracts them.
- **`%USERPROFILE%\Documents\Xml`**: the human-visible working set. On this machine that is `C:\Users\nwilkinson-admin\Documents\Xml`.
- Project files are immutable gold. `Documents\Xml` is disposable and is re-seeded every suite start (default `Overwrite`).

### 6.2 Paths

| Role | Path |
|---|---|
| Gold in source | `src/Vestigium.Helpers.Tests/` or `src/Vestigium.Helpers.Xml.Tests/` → `Content\Xml\*.xml` |
| Gold in output | `{AppContext.BaseDirectory}\Content\Xml\*.xml` |
| Gold in assembly | `Vestigium.Helpers.Xml.Tests.Content.Xml.{filename}` |
| Work area | `{MyDocuments}\Xml\` |
| Scratch | `{MyDocuments}\Xml\_scratch\{test-id}\` |

### 6.3 Seed algorithm

1. `dest = Environment.GetFolderPath(MyDocuments) + "\Xml"`. Override with `VESTIGIUM_XML_TESTAREA`.
2. Create `dest` and wipe/recreate `dest\_scratch`.
3. Catalog gold files from `BaseDirectory\Content\Xml`; if empty, enumerate EmbeddedResource names under `.Content.Xml`.
4. Copy each required file to `dest\{filename}`. `Overwrite` default; `VESTIGIUM_XML_SEED_MODE=IfMissing` keeps hand edits while debugging.
5. Fail the suite if a required file is missing or zero bytes.
6. Do not delete extra files the user left in `Documents\Xml`.
7. Read tests use `dest\{file}`. Update/Delete use a scratch copy.
8. Never write back into Content or into the assembly.

### 6.4 MSBuild fragment

```xml
<Content Include="Content\Xml\**\*.xml">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
</Content>
<EmbeddedResource Include="Content\Xml\**\*.xml">
  <LogicalName>Vestigium.Helpers.Xml.Tests.Content.Xml.%(Filename)%(Extension)</LogicalName>
</EmbeddedResource>
```

### 6.5 Test isolation

xUnit tests **must** set `VESTIGIUM_XML_TESTAREA` to a temp folder (or an injected hook) so CI and suite runs never require or pollute the real user Documents folder. The Documents default exists so a developer machine always has inspectable files after a local seed.

---

## 7. Functional requirements

ID prefixes: XML-R Read, XML-S Search, XML-C Create, XML-U Update, XML-D Delete, XML-M media/RFC 7303.

### 7.1 Read

| ID | Requirement | Priority |
|---|---|---|
| XML-R01 | Load from file path, Stream, `ReadOnlySpan<byte>`, and string. Same options object on all overloads. | Must |
| XML-R02 | Detect encoding per RFC 7303 §3. Expose the winner and any conflicting source on the session. | Must |
| XML-R03 | Return an `XmlSession` that keeps Root, Declaration, Encoding, MediaType, Source path. | Must |
| XML-R04 | Well-formed single-document mode: typed failure on not-well-formed input. | Must |
| XML-R05 | Multi-document mode iterates successive roots in one stream. | Must |
| XML-R06 | Do not resolve external DTDs, external parsed entities, or XInclude unless a future Trusted mode is explicit. | Must |
| XML-R07 | Keep comments by default. Normalize insignificant whitespace only when asked. | Should |
| XML-R08 | Guess media type: `application/xml` default; honor caller `+xml`; `text/xml` compares equal as alias. | Should |
| XML-R09 | Reader path for large files so Search can scan without a full `XDocument` when requested. | Should |

### 7.2 Search

| ID | Requirement | Priority |
|---|---|---|
| XML-S01 | Find elements by local name, optional namespace URI, optional ancestor filter. | Must |
| XML-S02 | Find by attribute name and/or value (exact, contains, regex). | Must |
| XML-S03 | Find by element inner text (exact, contains, regex, case option). | Must |
| XML-S04 | XPath 1.0 with a namespace manager. Auto-bind the document's default xmlns to a caller prefix (e.g. `u:`). | Must |
| XML-S05 | Return node handles with path, name, namespace URI, attributes, text. Stable enough to feed Update/Delete. | Must |
| XML-S06 | First / Any / Count so callers do not materialize large sets. | Should |
| XML-S07 | Streaming search over MultiDocument sources. | Should |
| XML-S08 | Typed getters: Bool, Int, Guid, Base64, DateTime on a node or attribute. | Should |

Locked search examples:

- `osinfo.xml`: `action/name == MagicOn`
- `ReAgent.xml`: `WinreBCD/@id`; `InstallState/@state == 1`
- `ipcfg.xml`: all `action/name`; `allowedValue` under `ConnectionStatus`
- `DevicePreparationDDF.xml`: `NodeName == PageEnabled` and sibling `DefaultValue`
- `DMClient_DDF.xml`: `Unenroll` nodes that expose Exec
- `diagwrn.xml`: count `Mod == setupcln.dll` and `Err == 1920`; distinct `Fun`

### 7.3 Create

| ID | Requirement | Priority |
|---|---|---|
| XML-C01 | Create an empty document with XML 1.0 declaration, encoding UTF-8. | Must |
| XML-C02 | Fluent builder: root, elements, attributes, text, comments, namespace declarations. | Must |
| XML-C03 | Create from a gold template (clone `osinfo` / `ReAgent` structure). | Must |
| XML-C04 | Emit declaration and optional BOM per `XmlWriteOptions`. | Must |
| XML-C05 | Content-Type helper: `application/xml; charset=utf-8` by default. | Should |
| XML-C06 | Create a fragment (no declaration) for insertion. | Should |

### 7.4 Update

| ID | Requirement | Priority |
|---|---|---|
| XML-U01 | Set element text by handle or by search (first / all). Working tree only until Commit. | Must |
| XML-U02 | Set, add, or rename an attribute. | Must |
| XML-U03 | Insert a child before, after, or as last child. | Must |
| XML-U04 | Replace a subtree from an `XElement` / fragment string. | Must |
| XML-U05 | Save in place or to a new path. Atomic save for files. | Must |
| XML-U06 | Preserve unmatched comments unless Indent/Normalize is on. | Should |

### 7.5 Delete

| ID | Requirement | Priority |
|---|---|---|
| XML-D01 | Delete a node and its subtree by handle or by search. | Must |
| XML-D02 | Delete an attribute. | Must |
| XML-D03 | Clear text but keep the element. | Should |
| XML-D04 | Refuse to delete the document root; require ReplaceRoot. | Must |
| XML-D05 | Multi-document rewrite of survivors. | Could (not v1) |

---

## 8. RFC 7303 compliance

| ID | Rule | Library behavior |
|---|---|---|
| XML-M01 | `application/xml` preferred for document entities. | Default on write. |
| XML-M02 | `text/xml` is an alias of `application/xml` (7303 vs 3023). | Accept on read. Same charset rules. |
| XML-M03 | `application/xml-dtd` for DTD subsets. | Detect a raw DTD payload vs an instance with DOCTYPE. |
| XML-M04 | `+xml` suffix. | `XmlMediaType.IsXmlFamily("application/vnd.oma.ddf+xml") == true`. |
| XML-M05 | charset optional; UTF-8 recommended. | Write charset=utf-8 when producing a Content-Type string. |
| XML-M06 | BOM overrides charset. | Read records the winner and the losers. |
| XML-M07 | No charset + no BOM + no declaration ⇒ UTF-8. | Default. |
| XML-M08 | UTF-32 not advertised. | Reject on write unless forced (and forced is not a public default). |
| XML-M09 | External parsed entities. | Out of v1 except to refuse unsafe expansion. |

On save, the declaration encoding, the on-disk encoding, and the media-type charset must agree unless the caller opts into a transcode.

---

## 9. Public surface (v1)

Names may move a token. The shapes may not. Identity and Probe already exist and keep these exact strings.

```csharp
public static class XmlHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Xml"
    public static string Probe();

    public static string DefaultExportDirectory();
    public static string NewExportPath(string? stem = null);

    public static XDocument Parse(string xml, XmlReadOptions? options = null);
    public static XDocument Parse(Stream stream, XmlReadOptions? options = null);

    public static XmlSession Create(string? path = null, XmlSessionOptions? options = null);
    public static XmlSession Open(string path, XmlSessionOptions? options = null);
    public static XmlDocumentStream OpenMulti(string path, XmlSessionOptions? options = null);
    public static XmlSession OpenExport(string stem, XmlSessionOptions? options = null);

    public static void WriteFile(string path, XDocument document, XmlWriteOptions? options = null);
}

public sealed class XmlSession : IDisposable
{
    public string SessionId { get; }
    public string? Path { get; }
    public Encoding Encoding { get; }
    public XmlMediaType MediaType { get; }
    public bool HasUncommittedWork { get; }
    public bool HasUnsavedCommit { get; }

    public void Snapshot();
    public IReadOnlyList<XmlWorkNode> Search(XmlSearch query);
    public XmlWorkNode? First(XmlSearch query);
    public int Count(XmlSearch query);

    public void SetText(string pathOrXPath, string value);
    public void SetAttribute(string pathOrXPath, string attributeName, string value);
    public void Insert(string parentPath, XElement child);
    public void Delete(string pathOrXPath);

    public IReadOnlyList<XmlChange> Diff();
    public void Commit();
    public void Revert();
    public void Cancel();
    public string Save();
    public string SaveWorking();
    public string SaveAs(string path, XmlCollision collision = XmlCollision.Fail);
}

public enum XmlCollision { Fail = 0, Overwrite = 1 }

public sealed class XmlReadOptions
{
    public bool ProhibitDtd { get; init; } = true;
    public bool MultiDocument { get; init; } = false;
    public string? Charset { get; init; }
    public long MaxCharacters { get; init; } = 4_000_000;
}

public sealed class XmlWriteOptions
{
    public bool Indent { get; init; } = false;
    public bool EmitBom { get; init; } = false;
    public XmlCollision Collision { get; init; } = XmlCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
}
```

`DefaultExportDirectory` is `%DESKTOP%\Vestigium\Exports\Xml\` unless a test injects `XmlTestHooks.ExportRoot`. `NewExportPath` uses stem + `.xml` under that folder.

Optional v1: `XmlHelper.DigestWritten(path)` calls Hashing on the file bytes after Save. Not required to ship Phase 1.

`XmlSearch` is a small builder (`ByName`, `ByAttribute`, `ByText`, `XPath`, namespace binds). Exact fluent grammar may move; behavior in §7.2 may not.

---

## 10. Logging

Category = `Helpers`. APPID = `Xml`.

Register these subcategories (reuse existing names where they already exist):

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing suite |
| Session | Open, Create, Dispose, SessionId |
| Document | Parse, path, byte length, encoding winner, media type |
| Query | Search path spelling (not the value) |
| Snapshot | Baseline taken |
| Diff | Operation count only |
| Commit | Commit / Revert / Cancel |
| Save | Path, bytes, collision, atomic |
| Multi | OpenMulti document count |
| Safety | DTD rejected, resolver asked (must be zero) |

Sparse audit: session start, snapshot, commit/revert/cancel, save, Failed. Not a line per quiet First() of a settings node.

Never: element text, attribute values, Base64, PEM, long hex, `Exception` argument to HelperLog.

`HelperGuard` is the contract layer for empty, null, and range rejects: log Failed, then throw.

---

## 11. Security

- `DtdProcessing.Prohibit` (or equivalent) by default.
- `XmlResolver = null`. No filesystem or HTTP entity resolution.
- A test with the OMA http DTD URL must not open a network connection (deny resolver throws if asked).
- A file with an external SYSTEM entity pointing at a sentinel file must not read that file.
- Billion-laughs style expansion is not processed because DTDs are prohibited.
- Paths from callers are used as given. Tests must not write outside the injected work area / scratch.
- Never serialize fixture secrets or payload bodies into exceptions.

A future `XmlLoadSafety.TrustedLocalDtd` may validate against a **bundled** OMA DTD. It is not in v1.

---

## 12. Demo

`Vestigium.Helpers.Xml.Demo` stays on `HelperWpfHost` + APPID `Xml` and the shared `SkeletonWindow` until this SRS is Accepted. After Phase 4 it becomes a shipped gallery:

- Overview, Open (osinfo / ReAgent / ipcfg), Search, Edit (Set/Diff/Commit/Save/Cancel), Multi (`diagwrn` counts), Export folder, JSONL audit pane
- Demo files seeded from Content into the injected work area or `%TEMP%\Vestigium.Helpers.Xml.Demo`
- Probe remains `%TEMP%` only

---

## 13. Tests

xUnit, serial logger collection, temp `LogDirectory`, injected export root, injected `VESTIGIUM_XML_TESTAREA`.

- Identity is `Vestigium.Helpers.Xml`.
- Probe writes Pending then Success; writes only under `%TEMP%`; does not seed Documents.
- After `EnsureSeeded()` against an empty work area, all six gold files are present and non-empty.
- Gold Content files are never written by scratch copies.
- Loading `DevicePreparationDDF.xml` does not perform a network request.
- `Open(diagwrn.xml)` throws; `OpenMulti` returns more than one document and can search `Fun` / `Msg`.
- Encoding detector: UTF-8 no BOM, UTF-8 BOM, UTF-16 LE BOM.
- Declaration vs BOM conflict: BOM wins, warning recorded.
- `text/xml` and `application/xml` compare equal as aliases.
- `osinfo`, `ReAgent`, and `ipcfg` survive Snapshot → Set → Diff → Commit → Save → reload with the intended change present.
- Cancel after Set leaves disk untouched.
- `SaveAs` existing dest with default collision throws; Overwrite replaces.
- Atomic write: dest is either the previous file or the complete new file.
- Tests never touch the real Desktop or live ProgramData. Documents\Xml is only used when a developer runs the seeder locally without an override.
- No payload body in captured HelperLog lines.

---

## 14. Non-goals (v1)

- Vestigium.Logging writer / `%ProgramData%\Vestigium\Logs\`
- FileIo copy, move, delete, mirror, UniqueName, recon, buckets
- Json payload API, Csv, ClosedXml, Excel
- A from-scratch XML 1.0 / 1.1 parser
- XSD / Schematron / DTD validation
- XPath 2.0, XQuery, XSLT
- Multi-document rewrite
- Byte-identical round-trip / C14N 1.0
- JSON↔XML conversion
- HTTP client that fetches XML
- Office Open XML packages (docx / xlsx / pptx)
- Admin rights, scheduler

---

## 15. Roadmap

| Version | Item |
|---|---|
| **v1.0** | This document. Identity + Probe already on `main`. Implementation follows acceptance. |
| **v1.1** | `DigestWritten` via Hashing; Compare two files (semantic node compare + change list) |
| **v1.2** | Streaming multi-document filter + rewrite; TrustedLocalDtd against a bundled OMA DTD |
| **later** | XSD validation, XPath 2.0, C14N |

Never here: being the suite logger; being FileIo; being Json; being a DDF/UPnP SDK.

---

## 16. Delivery phases

**Phase 0 — this document.** Accept SRS + Guide. Do not grow `XmlHelper` past Identity + Probe until Accepted.

**Phase 1 — Read + media types + safety.** `XmlReadOptions.Safe`, encoding detector, `XmlMediaType`, MultiDocument, seeder, security tests green.

**Phase 2 — Search.** ByName / ByAttribute / ByText / XPath. Locked examples in §7.2.

**Phase 3 — Create / Update / Delete.** Session lifecycle, atomic save, scratch-copy tests on osinfo, ReAgent, ipcfg, DevicePreparationDDF.

**Phase 4 — Demo + polish.** Gallery tabs, XML docs, package metadata.

**Phase 5 — Harden.** Sparse log, Probe `%TEMP%` only, Guide matches the code.

---

## 17. Acceptance

This SRS becomes **Accepted** when:

1. This file and the Developers Guide live under `src/Vestigium.Helpers.Xml/_Documentation/` (they already do; this revision replaces the skeleton in place).
2. Document IDs stay `VEST-HLP-XML-SRS-000` and `VEST-HLP-XML-DEV-000`.
3. Identity remains `Vestigium.Helpers.Xml`. Probe remains Pending then Success through `HelperLog.AppIds.Xml`.
4. A follow-up implementation PR can be reviewed against this text without inventing UniqueName, a second logger, a custom parser, or a network DTD fetch.
5. Content fixtures + `XmlContentSeeder` are the only way tests obtain the six files.

Do not grow `XmlHelper` past Identity + Probe until Status on this file is changed to Accepted.

---

## 18. Risks

- `XDocument` will not preserve every byte (attribute order, empty-element style, insignificant whitespace). Do not chase byte-identical diffs.
- Capturing DOCTYPE without enabling DTD processing is awkward on some .NET versions. We may store the prologue separately.
- `diagwrn.xml` may grow; streaming search is the scalable path.
- `Documents\Xml` is profile-specific. CI must set `VESTIGIUM_XML_TESTAREA`. Content + EmbeddedResource still guarantee the bytes exist.
- Overwrite seed replaces hand edits in `Documents\Xml` at the next local seed. Use `IfMissing` or `_scratch` for experiments.
- OMA DTD URLs look authoritative; fetching them in a customer environment is both slow and a data leak.

---

## 19. Glossary

| Term | Meaning here |
|---|---|
| Gold / Content | Immutable XML files in the test project, shipped as Content and EmbeddedResource. |
| Work area | `%USERPROFILE%\Documents\Xml`, seeded from Content. Override `VESTIGIUM_XML_TESTAREA`. |
| Scratch | `Documents\Xml\_scratch\{id}`, the only place Update/Delete may write. |
| Seeder | `XmlContentSeeder.EnsureSeeded()` copies gold bytes to the work area. |
| DDF | OMA Device Description Framework XML (the two `MgmtTree` files). |
| SCPD | UPnP Service Control Protocol Description (`ipcfg`, `osinfo`). |
| MultiDocument | A byte stream that contains more than one XML document in sequence. |
| Safe load | No DTD, no resolver, size cap, RFC 7303 encoding rules. |
| `+xml` | RFC 6839 / 7303 suffix marking an XML-based media type. |
