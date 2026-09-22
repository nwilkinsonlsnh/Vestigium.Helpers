# Vestigium.Helpers.Xml

XML document helpers for payload files. RFC 7303 encoding, XPath 1.0 search, snapshot / diff / commit sessions. Platform parser only (`XmlReader` + `XDocument`). Not FileIo. Not Json. Not the audit logger.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Xml` 1.0.0 |
| TFM | `net10.0` |
| APPID | `Xml` (`XmlCatalog.AppId`) |
| EVENTID | Reserved 16500–16999 (used through 16525) |
| Depends on | `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Xml" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Xml;

using var doc = XmlHelper.Open(@"D:\Payloads\osinfo.xml");
doc.Snapshot();
doc.SetText("//u:action/u:name", "MagicOff");
var changes = doc.Diff();   // path + op — show it; do not log values
doc.Commit();
doc.Save();
```

`Open` is one well-formed document. Multi-document dumps use `OpenMulti`.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `Parse` / `Open` / `Create` | document / session | |
| `OpenMulti` | document stream | Explicit. `Open` of a multi file fails. |
| `WriteFile` | void | Collision default Fail. |
| `session.Snapshot` / `Diff` / `Commit` / `Save` | session verbs | Disk sees committed only. |
| `Search` / `XPath` | hits | Local name ignores prefixes. XPath does not. |
| `XmlCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- DTD prohibited. Resolver null. No network fetch of a PUBLIC DTD.
- RFC 7303 encoding order. UTF-8 no BOM default. `text/xml` is an alias.
- Never log element text, attribute values, Base64, PEM, or long hex.
- Diff logs `ops=` only.
- Collision default Fail. Atomic write default true. Indent default false.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Xml
    cfg.LogDirectory = logDir;
    XmlCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/xml.json`.

## Related

Trees: `Vestigium.Helpers.FileIo`. JSON documents: `Vestigium.Helpers.Json`.

Long-form documents live in [Vestigium.Documentation / Helpers / Xml](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Xml/004%20--Developers%20Guide) |
