# Vestigium.Helpers.LogParser.Url — Requirements

**Document ID:** VEST-HLP-LOGPARSER-URL-REQ-PR01
**Version:** 1.0
**Status:** Lock for PR01.
**Date:** 6 October 2026
**Package:** `Vestigium.Helpers.LogParser.Url` 1.0.0 (not packed)
**TFM:** `net10.0`
**Depends on:** `Vestigium.Helpers.LogParser`, `Vestigium.Logging` 1.7.1
**Project on disk:** `src/Vestigium.Helpers.LogParser.Url` (stub at `c80a440`)

**One sentence:** Pull every host-bearing token out of a UTF-8 text dump so DNS can be asked about the names.

**This version is not** scheme-only. The project is named Url. The contract includes bare hostnames and `name@host`. A sheet column of `login.microsoftonline.com` is in. `LogParser.Domain` is not the home for that column. That project is out.

Not a Word reader. Not an xlsx reader. Not an HTML parser. Not a public-suffix list. The owner saves the sheet as `.txt`.

## Decisions

| # | Decision | Locked as |
|---|---|---|
| 1 | Door | `UrlReader.Read` / `ReadFile` → `LogReadResult` with `Format = Url`. |
| 2 | Yield | Scheme URLs, mailto, bare domains, `localhost`, IPv4 literals. |
| 3 | Name trap | A test that only feeds `https://` URLs does not pass this requirement. |
| 4 | HAR | This package does not parse HAR JSON. DnsIQ sends a non-JSON `.har` here as text. |
| 5 | Logging | Internal `UrlLog`. APPID `LogParser.Url`. No `Initialize`. |

## Must change

### R01-01 Extract

| Hit | Notes |
|---|---|
| `http://` or `https://` URL | Host and port. Drop `data:`, `blob:`, `about:`, `chrome:`. |
| `mailto:` or `name@host` | Host is the part after `@`. |
| Bare domain | Two or more labels. Each label 1–63, `[a-z0-9-]`, no leading or trailing hyphen. Last label is letters, length 2–24. |
| Bare IPv4 | `IsAddress=true`. |
| `localhost` | No dot. Still a host. |

Source flag is `Url` for every hit. Hit count = times seen. Ports only from URLs that carried one.

Reject:

- Last label is a file extension: `txt`, `csv`, `tsv`, `xlsx`, `xls`, `json`, `har`, `log`, `xml`, `pdf`, `png`, `jpg`, `jpeg`, `gif`, `dll`, `exe`, `config`, `md`. `report.txt` is a filename.
- A digit-only label, unless the whole token is an IPv4.
- Single-letter labels (`e.g.`, `U.S.`).

UTF-8. BOM allowed. Over 64 MB → reject before scan.

### R01-02 Package

- Reference `LogParser`. Do not copy `LogHost`.
- Delete `Class1.cs`.
- No sockets. No DNS. No WPF. No ClosedXml.
- Internal log. Own event range.

## Must not

- HAR field map
- `LogParser.Domain`
- Native `.xlsx` / `.docx`
- `Initialize`

## Acceptance

1. One `.txt` with `https://q2prod.idbs-cloud.com:8443/`, `user@q2valprod.services.idbs-cloud.com`, and bare `login.microsoftonline.com` yields those three hosts. Port 8443 is on the first.
2. `notes.txt` in that file is not a host.
3. `e.g.` is not a host.
4. An IPv4 literal is `IsAddress`.
5. Tests do not open a socket.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 6 Oct 2026 | Initial lock. Domain folded in. Not scheme-only. |
