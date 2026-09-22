# Vestigium.Helpers.Xml — Developers Guide

**Document ID:** VEST-HLP-XML-DEV-000  
**Version:** 1.0  
**Status:** Draft — companion to SRS v1.0  
**Date:** 15 September 2026  
**TFM:** `net10.0`  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Xml/`. This file replaces the 7 September 2026 skeleton in place. Document ID, Identity string, Probe behavior, and TFM are unchanged.

## What this library is

A helper for **payload XML**: settings, OMA DDF, UPnP SCPD, Windows RE, diagnostic dumps you need to query. Platform parser only (`XmlReader` + `XDocument`). RFC 7303 encoding and media types. XPath 1.0 plus local-name search. An edit session so a developer can see the change list before disk changes.

It is not the audit logger. HelperLog still writes Vestigium JSONL under `%ProgramData%\Vestigium\Logs\Xml\`. Do not implement a log writer here.

It is not FileIo. Do not copy trees, UniqueName, or recon. Xml may read or write one path with an atomic sibling temp.

It is not Json. Do not grow RFC 8259 / JSON Pointer / JSONL here.

It is not a DDF or UPnP SDK. Those documents are fixtures, not product types.

## What already exists on main

```csharp
public static class XmlHelper
{
    public static string Identity => "Vestigium.Helpers.Xml";
    public static string Probe();   // HelperLog Pending then Success, APPID Xml
}
```

`Vestigium.Helpers.Xml.Demo` is the WPF gallery (`net10.0-windows`). It hosts APPID Xml through `HelperWpfHost` and `MainWindow` — same chrome as Json.Demo.

## Host a document (target shape after Phase 3)

```csharp
var doc = XmlHelper.Open(@"D:\Payloads\osinfo.xml");
doc.Snapshot();
doc.SetText("//u:action/u:name", "MagicOff");
var changes = doc.Diff();          // path + op — show it; do not log values
doc.Commit();
doc.Save();                        // atomic replace of the opened path
```

Create + default folder:

```csharp
var doc = XmlHelper.Create(XmlHelper.NewExportPath("probe-settings"));
doc.Insert("/", new XElement("root", new XElement("appId", "PingIQ")));
doc.Commit();
doc.Save();
```

Multi-document dump (`diagwrn.xml` is several ADO rowsets back-to-back):

```csharp
foreach (var part in XmlHelper.OpenMulti(path))
{
    var n = part.Count(XmlSearch.ByAttribute("Fun", "SetupClnEstimateFileSize"));
}
```

`Open` stays a single well-formed document. `OpenMulti` is explicit. `Open(diagwrn.xml)` is Failed.

The class library never calls `VestigiumLogger.Initialize`. The host does (`HelperWpfHost` or `HelperLog.InitializeHost`). Category `Helpers`, APPID `Xml`.

## Snapshot / Diff / Commit / Save

Same lifecycle as Json, different payload.

- **Snapshot** freezes the committed tree as the Diff baseline.
- **SetText / SetAttribute / Insert / Delete** mutate working memory only.
- **Diff** is a change list from baseline → working. Show it. Do not log values.
- **Commit** copies working into committed.
- **Save** writes committed (atomic). Disk does not see uncommitted Sets.
- **SaveWorking** is the escape hatch; log Warning.
- **Revert** restores working from Snapshot (or committed).
- **Cancel** drops working, no write.

Cancel here means session Cancel (drop working), not FileIo job Cancel.

## Search

Local name ignores prefixes. XPath does not.

```csharp
doc.Search(XmlSearch.ByName("NodeName").WhereText("PageEnabled"));
doc.Search(XmlSearch.ByAttribute("state", "1"));
doc.XPath("//u:action/u:name", new XmlNs("u", "urn:schemas-upnp-org:service-1-0"));
```

UPnP files declare `xmlns="urn:schemas-upnp-org:service-1-0"`. If you forget the bind, XPath returns nothing. `ByName("action")` still works because it matches on local name.

Invalid syntax fails through `HelperGuard` / Query Reject. Get of a missing path throws; First / Try helpers return empty. Set creates element parents only when asked through Insert; it does not invent a tree from an XPath that did not match.

Locked examples the tests will pin are in SRS §7.2 (`MagicOn`, `WinreBCD/@id`, `PageEnabled`, `Unenroll`, `setupcln.dll`).

## RFC 7303 encoding

Read order:

1. BOM if present.
2. Else caller MIME charset (`XmlReadOptions.Charset`).
3. Else XML declaration `encoding=`.
4. Else UTF-8.

If BOM and charset disagree, BOM wins and the session records a warning. Write UTF-8 with no BOM unless `EmitBom` is set. Prefer `application/xml; charset=utf-8`. Treat `text/xml` as an alias, not a different charset rule (that is the 3023 → 7303 change).

Do not advertise UTF-32.

## Safe load

Every Open / Parse uses `XmlReadOptions` defaults:

- DTD prohibited
- `XmlResolver` null
- max characters capped
- no XInclude

`DevicePreparationDDF.xml` and `DMClient_DDF.xml` carry a PUBLIC DOCTYPE that points at `http://www.openmobilealliance.org/tech/DTD/DM_DDF-V1_2.dtd`. If a load fetches that URL, the implementation is wrong. Keep the DOCTYPE text if the platform lets us; never resolve it.

## Writes

Default dest folder: `%DESKTOP%\Vestigium\Exports\Xml\`. Tests replace that root via `XmlTestHooks.ExportRoot`. Probe never uses it.

`Save` of the opened path always replaces that file (atomic sibling temp, then `File.Move` overwrite). `SaveAs` / `WriteFile` to another existing path **fails** unless `XmlCollision.Overwrite`. There is no UniqueName here — call FileIo in the host if you need a minted name.

Pretty-print is off by default. Indent is opt-in. Do not expect byte-identical round-trips: the DOM may rewrite `<a/>` vs `<a></a>` and attribute order.

## Fixtures — Content seeds Documents\Xml

Gold files live in the test project:

```
Content\Xml\DevicePreparationDDF.xml
Content\Xml\DMClient_DDF.xml
Content\Xml\diagwrn.xml
Content\Xml\ipcfg.xml
Content\Xml\osinfo.xml
Content\Xml\ReAgent.xml
```

They are both `<Content CopyToOutputDirectory="PreserveNewest">` and `<EmbeddedResource>` with

`LogicalName = Vestigium.Helpers.Xml.Tests.Content.Xml.%(Filename)%(Extension)`

`XmlContentSeeder.EnsureSeeded()` copies those bytes to `%USERPROFILE%\Documents\Xml` so a developer machine always has the files. On this profile that folder is `C:\Users\nwilkinson-admin\Documents\Xml`.

Rules:

- Suite start seeds the work area (`Overwrite` default).
- Read tests use `XmlContentSeeder.GetPath("osinfo.xml")`.
- Update/Delete tests use `XmlContentSeeder.CreateScratchCopy("osinfo.xml")` under `Documents\Xml\_scratch\{id}\`.
- Never write the Content folder.
- CI and xUnit **must** set `VESTIGIUM_XML_TESTAREA` to a temp directory so the suite does not touch the real Documents folder.
- `VESTIGIUM_XML_SEED_MODE=IfMissing` is a local debugging convenience only.

## Sparse HelperLog

Audit these: session start (Create / Open / Dispose), Snapshot, Diff (`ops=` only), Commit / Revert / Cancel, Save (path, bytes, collision, atomic), Multi (`OpenMulti documents=`), Document counts on Parse / Open (bytes, encoding), Search path spelling, Safety (DTD rejected), Failed.

Do **not** log a quiet First / Count of a settings node. Never element text, attribute values, Base64, PEM, long hex, or `Exception` objects. `HelperLog.Trap` is for unexpected failures only; expected `ArgumentException` / `XmlException` / `IOException` rethrow without Trap.

## Probe

`Probe` already logs Pending then Success and returns Identity. After Phase 1 it may parse a tiny in-memory demo document. It must not write the Desktop export folder, must not seed Documents\Xml, must not open a durable session, and tests pin its HelperLog directory under `%TEMP%`.

## Gallery

`Vestigium.Helpers.Xml.Demo` hosts APPID Xml. Tabs: Overview, Document (Set/Diff/Commit/Save/Cancel plus the working tree), Search, Multi, Safety, Export, JSONL audit pane. Demo files come from Tests `Documents\Xml` as Content (copied to output `Xml\` and seeded to `%USERPROFILE%\Documents\Xml`). Command failures stay in the status line. The gallery calls the public session surface (`XmlHelper.Open` / `OpenMulti` / `XmlSearch` / `XmlSession.WorkingXml`); it does not reimplement parse.

## Locked (do not reopen in build mode)

- Platform parser only. No custom XML grammar.
- RFC 7303 encoding order. UTF-8 no BOM default. `text/xml` is an alias.
- DTD off. Resolver null. No network.
- Collision default Fail. AtomicWrite default true. Indent default false.
- Multi-document is Read/Search only in v1.
- HelperLog: paths and counts, never bodies. Quiet Get/First.
- Identity stays `Vestigium.Helpers.Xml`. APPID stays `Xml`.
- Identity stays `Vestigium.Helpers.Xml`. Grow the gallery against the v1.0 SRS surface; Status on the SRS is still Draft until you Accept it.

## Sibling fences

Logging = audit JSONL. FileIo = trees. Json = RFC 8259 documents. Hashing = digests. Csv = delimited tables. ClosedXml = workbooks. Xml = XML documents and queries.
