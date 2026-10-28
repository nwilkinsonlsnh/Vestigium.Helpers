# Vestigium.Helpers.Hashing — Design

**Document ID:** VEST-HLP-HASH-DSN-200  
**Version:** 2.0  
**Status:** Accepted  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.Hashing` **1.4.0**  
**Contract:** [`Requirements_v2.0.md`](Requirements_v2.0.md)

If this file and Requirements disagree, Requirements wins.

## 1. Intent

One assembly for unkeyed digests, keyed HMAC/KMAC, SHAKE XOFs, Argon2id password verifiers, and catalogue checksums. Encryption stays a sibling. FileIo calls Hashing for unique-content and compare. Hosts initialize Logging; this library only writes.

## 2. Source shape

| File | Role |
|---|---|
| `HashingHelper.cs` | Identity, Probe, HashString / HashBytes / HashData |
| `HashingHelper.Files.cs` | HashFile / async / Verify* |
| `HashingHelper.Hmac.cs` | HMAC family |
| `HashingHelper.Kmac.cs` | KMAC family |
| `HashingHelper.Shake.cs` | SHAKE family |
| `HashingHelper.Checksum.cs` | CRC / xxHash |
| `HashingHelper.Password.cs` | HashPassword / VerifyPassword |
| `HashingHelper.Private.cs` | Stream pump, algorithm maps |
| `PasswordHash.cs` | PHC parse / encode / verify / caps |
| `HmacKey.cs` | Key object, Dispose, finalizer, safe ToString |
| `HashingLog.cs` | ALCOA+ write + Safe + RequireNotBlank |
| `HashingEvents.cs` / `HashingCatalog.cs` | EVENTID twins |
| `EventCatalog/hashing.json` | Packed sidecar |

Partials exist so a single `HashingHelper.cs` does not hit GitHub push size limits. They are one type.

## 3. Password verify

```
VerifyPassword(password, stored)
  stored empty or !TryParsePhc → Failed 13020, return false
  caps exceeded → Failed, throw CryptographicException
  else PasswordHash.Verify → Success 13060, return bool
```

HashPassword still throws `ArgumentException` on empty or over-long password (lock 9 + existing length gate). That is input validation, not a malformed verifier.

## 4. HmacKey memory

Bytes live in a private `byte[]`. `Span` is internal. `Dispose` zeros, nulls, sets disposed, `GC.SuppressFinalize`. Finalizer calls `Dispose` if the host forgot. `ToString` never walks the bytes as hex.

`FromString` UTF-8 copies then `ZeroMemory` on the temporary encoding buffer.

## 5. Logging route

`HashingLog.Success(verb, detail)` picks EVENTID from the verb prefix:

- Probe → 13005
- HashFile* → 13030
- Hash* except HashPassword → 13025
- Hmac* / VerifyHmac* → 13035
- Kmac* / VerifyKmac* → 13040
- Shake* / VerifyShake* → 13045
- Checksum* / VerifyChecksum* → 13050
- HashPassword → 13055
- VerifyPassword → 13060
- Convert → 13065
- else → 13015

Pending is 13000 (Probe) or 13010. Failed is 13020. Blank path is 13070.

`HelperGuard.NotBlank` is a one-line alias on `RequireNotBlank` so existing path helpers compile. `HelperCompat.cs` is gone.

## 6. Pack graph

Hashing references:

- `Konscious.Security.Cryptography.Argon2` 1.3.1
- `System.IO.Hashing` 10.0.0
- `Vestigium.Logging` 1.7.1 (Directory.Build.props)

No project references. nupkg must contain `README.md` and `contentFiles/any/any/EventCatalog/hashing.json`.

FileIo still ProjectReferences Hashing until 1.4.0 is 200 on nuget.org.

## 7. What never appears in JSONL

Input plaintext. HMAC key hex or Base64. Password. Salt. PHC string. `Exception.ToString()`. `HmacKey.ToString()` is safe to interpolate; do not interpolate `ToHexLower()`.

File digest hex on 13030 is allowed. HMAC file Success may include the MAC hex (that is a tag, not the key).

## 8. Tests that lock the design

`HashingPR01Tests` — fail-closed PHC, cap bomb, ToString, HMAC JSONL `keyBytes=`.
`HashingLoggingTests` — 13005 / 13025 / 13030 and catalog row count 15.
`HashingContractTests` — FIPS / RFC / CRC / file / password / interop vectors.

## 9. Out of this design

Demo WPF host. Blake3. A second hashing csproj. Encryption trailer writers.
