# Vestigium.Helpers.PerfMon.PageFile — PF01 backlog

**Status:** Open.
**Date:** 28 September 2026

| Door | Item | Note |
|---|---|---|
| In | `Paging File` catalog | `% Usage`, `% Usage Peak` |
| In | Short job | `_Total` usage + peak |
| In | `IncludeFiles` | Live instances minus `_Total`, cap 256 |
| In | Logging door | APPID `PerfMon.PageFile` |
| In | EVENTID 20000–20045 | Block through 20499 |
| Out | Memory `Pages/sec` | Sibling |
| Out | Resize / create pagefile | Non-goal |
| Out | NuGet publish | After a host consumes this |
