# Vestigium.Helpers.LogParser.Har — Requirements

**Document ID:** VEST-HLP-LOGPARSER-HAR-REQ-PR01
**Version:** 1.0
**Status:** Lock for PR01.
**Date:** 6 October 2026
**Package:** `Vestigium.Helpers.LogParser.Har` 1.0.0 (not packed)
**TFM:** `net10.0`
**Depends on:** `Vestigium.Helpers.LogParser`, `Vestigium.Logging` 1.7.1
**Project on disk:** `src/Vestigium.Helpers.LogParser.Har` (stub at `c80a440`)

**One sentence:** Read a Chrome HAR 1.2 capture down to hosts the browser had to reach.

**This version is not** a HAR analyzer. No waterfall, no timings, no cookies, no header dump, no body decode, no request replay. Not a text scrape. A `.har` that is not JSON is not this package's problem. Url takes that file.

## Decisions

| # | Decision | Locked as |
|---|---|---|
| 1 | Door | `HarReader.Read` / `ReadFile` → `LogReadResult` with `Format = Har`. |
| 2 | JSON | `System.Text.Json`. No Newtonsoft. |
| 3 | Failed requests | Status 0, `net::ERR_*`, 302, 404, 400 still yield a host. |
| 4 | Server IP | `serverIPAddress` is not a host. |
| 5 | Logging | Internal `HarLog`. APPID `LogParser.Har`. No `Initialize`. |

## Must change

### R01-01 Extract

Host of an absolute `http` or `https` URL in:

| Field | Source flag |
|---|---|
| `entry.request.url` | `Request` |
| `entry.response.redirectURL` | `Redirect` |
| response header `Location` (absolute URL only) | `Location` |
| `page.title` when it is an absolute http(s) URL | `Page` |

Rules:

- Case-fold. Strip one trailing dot. `Uri` host.
- Deduplicate on host. Union ports. Union sources. Hit count = `request.url` hits. Redirect-only host still appears, hit count 0.
- Drop `data:`, `blob:`, `about:`, `chrome:`, empty, and relative `Location`.
- IP-literal host is `IsAddress=true`.
- `log.version` other than `1.2` → warning, still read `entries` if present.
- Missing `log` or `log.entries` → `InvalidDataException`. Do not return an empty success.
- UTF-8. BOM allowed.
- File over 64 MB → reject before parse.

### R01-02 Corpus

Two captures already in hand. Trimmed fixtures, not the 2.5 MB original, live in the test project.

Cannot-reach (`USITHLWDU19031_24JUL_SiteCannotBeReached.har`):

| Host | Why it stays |
|---|---|
| `q2prod.idbs-cloud.com` | Page title and the one 200. Port 8443. |
| `q2valprod.services.idbs-cloud.com` | Timed out. Status 0. Still a host. |
| `quintiles.sharepoint.com` | Referrer page. |

SSO (`USITHLWDU19031_SSO_SignInError.har`), ten hosts from the field map, not from response bodies:

`aadcdn.msauth.net`, `aadcdn.msftauth.net`, `cdn.auth0.com`, `idbs-q2valprod.us.auth0.com`, `idbs-themes.idbs-cloud.com`, `login.microsoftonline.com`, `q2prod.idbs-cloud.com`, `q2valprod.services.idbs-cloud.com`, `quintiles.sharepoint.com`, `static-resources.idbs-cloud.com`.

`login.windows.net`, `login.microsoftonline.us`, `login.microsoftonline.cn`, and `acdn.auth0.com` are not in `request.url`, `redirectURL`, `Location`, or page title. They are out of this reader. Port 8443 is on `q2prod`. Neither fixture yields `75.2.119.14` or `13.107.136.2` as a probe name.

### R01-03 Package

- Reference `LogParser`. Do not copy `LogHost`.
- Delete `Class1.cs`.
- No sockets. No DNS. No WPF.
- Internal log. Event catalog. Count by 5. Own range.

## Must not

- Text scrape of a failed HAR
- `serverIPAddress` as a host
- `LogParser.Domain`
- Calling `Initialize`

## Acceptance

1. Cannot-reach fixture yields the three hosts, including the timed-out auth host, and port 8443 on `q2prod`.
2. SSO fixture yields the ten. No server IP as a host.
3. A `data:` URL does not become a host.
4. Missing `log.entries` throws.
5. Tests do not open a socket.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 6 Oct 2026 | Initial lock. Field map only. |
