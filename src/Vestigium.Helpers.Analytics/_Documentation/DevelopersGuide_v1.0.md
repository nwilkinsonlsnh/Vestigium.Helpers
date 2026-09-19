# Vestigium.Helpers.Analytics — Developers Guide

**Document ID:** VEST-HLP-ANALYTICS-DEV-000  
**Version:** 1.6  
**Status:** Design companion to SRS v1.5 + Logging catalog 10500+  
**Date:** 18 September 2026

Open `Vestigium.Helpers.slnx` → `src/Vestigium.Helpers.Analytics/`.

## Logging

This library talks to **Vestigium.Logging** directly. It does not use `HelperLog`.

Host start:

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "Analytics"; // or the product APPID
    AnalyticsCatalog.Register(cfg);
});
```

- EventIds **10500–10615** (block reserved through 10999, count by 5).
- Constants: `AnalyticsEvents`.
- Shard: `EventCatalog/analytics.json`.
- The library never calls `Initialize` and never picks a log folder.
- Writes are skipped when the host has not initialized. Math still runs.
- Stable MESSAGE; varying values go in PROPERTIES.
- Inner percentile / histogram loops stay silent.
