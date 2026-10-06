# Vestigium.Helpers.LogParser.Url — Implementation Plan

**Document ID:** VEST-HLP-LOGPARSER-URL-PLAN-PR02
**Version:** 1.0
**Status:** Open.
**Date:** 6 October 2026
**Governs:** [PR01 -- Requirements.md](PR01%20--%20Requirements.md)

**Goal:** `UrlReader` over a dump, an email pasted into `.txt`, and a sheet saved as `.txt`. Bare names included.

**Not:** A Domain package. A HAR parser. An xlsx reader. A DNS probe.

## Current state

Stub. No reference to `LogParser`. No Logging pin. `LogParser.Domain` still exists on disk and is not in this plan.

## Decisions

| Decision | Why |
|---|---|
| Package `Vestigium.Helpers.LogParser` 1.0.0 | On the feed. No project reference. |
| One reader | Bare name and scheme URL are two patterns, one yield. |
| File extension denylist | `report.txt` in a sheet is the false positive that ships. |
| No public-suffix list | No buyer. Last-label letters plus the denylist is the cheap rule. |

## Surface

```
UrlReader.Read(Stream) → LogReadResult          Format = Url
UrlReader.ReadFile(string path) → LogReadResult
```

## Slices

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | URL-01 | Reference LogParser. Delete `Class1.cs`. `UrlReader` skeleton. | Open |
| 2 | URL-02 | Scheme, mailto, bare domain, localhost, IPv4. | Open |
| 3 | URL-03 | Tests: three-host dump, `notes.txt` rejected, `e.g.` rejected, IPv4 skipped-shape. | Open |
| 4 | URL-04 | `UrlLog` + catalog. Pin Logging 1.7.1. | Open |
| 5 | URL-05 | Pack 1.0.0 after LogParser 1.0.0 is on the feed. | Open |

### URL-03

A fixture that is only `https://` links fails the slice. The bare name is the sheet case.

Log scan start and scan fail. Do not log every host.

## Files

```
src/Vestigium.Helpers.LogParser.Url/
  UrlReader.cs
  UrlLog.cs
  UrlCatalog.cs
  EventCatalog/url.json
tests/Vestigium.Helpers.LogParser.Url.Tests/
  Fixtures/email-dump.txt
```

## Watch

| Role | Watch |
|---|---|
| Alvin | No xlsx. No second reader in Domain. No HAR field map. |
| Theodore | `report.txt` is not a host. Bare domain is. Oversize fails closed. |
| Simon | Url is not scheme-only. A valid HAR is not this package's input. |

## Next action

URL-01 after LogParser LP-01 exists.
