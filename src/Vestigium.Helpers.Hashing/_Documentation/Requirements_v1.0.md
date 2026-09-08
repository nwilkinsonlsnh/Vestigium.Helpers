# Vestigium.Helpers.Hashing — Requirements Specification

**Document ID:** VEST-HLP-HASH-SRS-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Hashing`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

## Purpose

String and file hashing for Vestigium hosts. Integrity checks, cache keys, “did this capture change.” This library does **not** encrypt.

```csharp
var hex = HashingHelper.HashString("payload");          // SHA-256 hex, after SRS
var file = HashingHelper.HashFile(path);                // streams; no ReadAllBytes
```

## Target

- Framework: `net10.0`
- Windows-only: no

## This milestone

Placeholder public type only (`HashingHelper.Identity` + `Probe()`). Do not grow the API until a lossless SRS is accepted.

Expected algorithms when that SRS is written (not locked here):

| Algorithm | Role |
|---|---|
| SHA-256 | Default string and file digest |
| SHA-512 | Opt-in longer digest |
| SHA3-256 | Later, if a host asks |

File hashing must stream. Same large-file rule as Encryption: do not load the file.

## Logging

`HelperLog` APPID `Hashing`. Library never calls `Initialize`. Log algorithm and byte counts, never the input string.

## Sibling

`Vestigium.Helpers.Encryption` owns Seal/Open. Hashing must not reference Encryption. Encryption must not grow hash APIs.
