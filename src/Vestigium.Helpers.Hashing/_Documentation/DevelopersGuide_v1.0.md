# Vestigium.Helpers.Hashing — Developers Guide

**Document ID:** VEST-HLP-HASH-DEV-000  
**Version:** 1.0  
**Status:** Skeleton  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation will live in `src/Vestigium.Helpers.Hashing/`.

## Design

**Intent.** Digests of strings and files. Not encryption.

**Status.** Skeleton until its own lossless SRS is accepted. Public surface today is `HashingHelper.Identity` + `Probe()` only.

**Locked for the skeleton.**

- `net10.0`
- `HelperLog` APPID `Hashing`
- Library never calls `Initialize`
- Do not implement SHA here in the same change as this placeholder
- Do not reference `Vestigium.Helpers.Encryption`

## Gallery

```
dotnet run --project src/Vestigium.Helpers.Hashing.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Hashing\`

## Roadmap

v1 after a lossless SRS: SHA-256 default, SHA-512 opt-in, streamed `HashFile`, hex output, tests against known test vectors.

Never: AES, passwords-as-keys, inventing a hash.
