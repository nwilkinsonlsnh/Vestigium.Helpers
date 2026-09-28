# Vestigium.Helpers.PerfMon.Network — PN01 backlog

**Status:** Open.
**Date:** 28 September 2026

| Door | Item | Note |
|---|---|---|
| In | 63-object catalog | Adapter through WinNAT |
| In | Short job | Network Interface rates + errors + queue |
| In | IncludeAdapters | Live instances minus `_Total`, cap 256 |
| In | Logging door | APPID `PerfMon.Network` |
| In | EVENTID 17500–17545 | Block through 17999 |
| Out | Helpers.Network merge | Sibling |
| Out | TCP/IPsec/SMB/WFP short jobs | Catalog first |
| Out | Packet capture | Non-goal |
| Out | NuGet publish | After a host consumes this |
