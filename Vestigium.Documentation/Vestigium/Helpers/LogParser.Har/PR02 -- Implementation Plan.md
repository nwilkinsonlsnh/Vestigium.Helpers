# Vestigium.Helpers.LogParser.Har — Implementation Plan

**Document ID:** VEST-HLP-LOGPARSER-HAR-PLAN-PR02
**Version:** 1.0
**Status:** Open.
**Date:** 6 October 2026
**Governs:** [PR01 -- Requirements.md](PR01%20--%20Requirements.md)

**Goal:** `HarReader` over the two captures, mapped onto `LogReadResult`. No DNS.

**Not:** A text fallback. Url owns a `.har` that is not JSON. DnsIQ owns the probe.

## Current state

Stub. No reference to `LogParser`. No Logging pin.

## Decisions

| Decision | Why |
|---|---|
| Static `HarReader` | Same shape as `NetworkHelper`. No façade interface. |
| Package `Vestigium.Helpers.LogParser` 1.0.0 | On the feed. No project reference. |
| Trimmed fixtures | The SSO original is 2.5 MB. Keep URLs, redirects, `Location`, status 0. Drop bodies. |

## Surface

```
HarReader.Read(Stream) → LogReadResult          Format = Har
HarReader.ReadFile(string path) → LogReadResult
```

## Slices

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | HAR-01 | Reference LogParser. Delete `Class1.cs`. `HarReader` skeleton. | Done |
| 2 | HAR-02 | Field extract per R01-01. | Done |
| 3 | HAR-03 | Trimmed corpus tests. Oversize and missing-entries tests. | Open |
| 4 | HAR-04 | `HarLog` + catalog. Pin Logging 1.7.1. | Open |
| 5 | HAR-05 | Pack 1.0.0 after LogParser 1.0.0 is on the feed. | Open |

### HAR-03 fixtures

- `cannot-reach.har` — three hosts, port 8443, timed-out auth host present.
- `sso.har` — ten hosts, no server IP as a host.
- `data-uri.har` — one real host plus a `data:` request.
- `missing-entries.har` — throws.

### HAR-04

Log parse start, parse fail, oversize. Do not log every host. That is a grid, not a log.

## Files

```
src/Vestigium.Helpers.LogParser.Har/
  HarReader.cs
  HarLog.cs
  HarCatalog.cs
  EventCatalog/har.json
tests/Vestigium.Helpers.LogParser.Har.Tests/
  Fixtures/
```

## Watch

| Role | Watch |
|---|---|
| Alvin | No scrape fallback in this project. No TCP. |
| Theodore | Status 0 still emits. Missing `entries` throws. Tests off the wire. |
| Simon | Server IP is not a probe name. CDN and SharePoint stay because they are in the file. |

## Next action

HAR-03. Trimmed corpus fixtures for the two captures. Extract is in. Server IP is not a host.
