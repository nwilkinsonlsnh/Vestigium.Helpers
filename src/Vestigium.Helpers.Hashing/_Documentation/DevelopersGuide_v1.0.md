# Vestigium.Helpers.Hashing — Developers Guide

**Document ID:** VEST-HLP-HASH-DEV-000  
**Version:** 1.3  
**Status:** Implemented v1.3  
**Date:** 9 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Hashing/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. SHA-256 default (lowercase hex), SHA-384/512 and SHA-3 opt-in, HMAC-SHA256 default with HMAC-SHA384 / HMAC-SHA512 / HMAC-SHA3 opt-in, KMAC128/256, SHAKE128/256 XOF, Argon2id PHC password verifiers, CRC-32 / CRC-64 / xxHash checksums, hex↔Base64 converters. Not encryption. HMAC is in this project; there is no `Vestigium.Helpers.Hmac` sibling. Checksums are `Checksum*`, never `Hash()`. SHAKE is `Shake*`, never `Hash()`. KMAC is `Kmac*`, never `HmacString`.

## Design

**Intent.** Digests of strings and files so a host can say “this capture did not change” without pulling Encryption. Keyed HMAC for a shared MAC secret. Password verifiers for stored credentials. Fast non-cryptographic checksums for catalogues and cache keys. Same 64 KiB stream rule as Encryption.

**Locked.** See SRS §2. The ones that must not drift:

- Default print is **lowercase hex**. Base64 is a converter / format argument, not the default.
- HMAC key min 16, generate default 32, enum 16/32/64/128. `FromString` is UTF-8. `FromBase64` decodes. Do not mix them.
- Default HMAC is **HMAC-SHA256**. `HmacAlgorithm.Sha384` / `Sha512` / `Sha3_*` are opt-in. Output 32 / 48 / 64 bytes. HMAC-SHA3 is OS-gated.
- KMAC is NIST SP 800-185, not HMAC. SHAKE is FIPS 202 XOF, not `Hash()`.
- `HashPassword` is Argon2id PHC (19 MiB / t=2 / p=1). Encryption KDF stays 64 MiB / t=3 / p=1 and still produces a content key.
- File APIs stream. Never `ReadAllBytes`.
- Library never calls `Initialize`. APPID `Hashing`.
- Never log input, HMAC keys, passwords, salts, PHC. File digest hex is the audit record.
- Do not fill Encryption trailer slots from this project. Encryption v1.3 writes them when the host passes `EncryptionSealOptions`. Hashing still has no Encryption reference.
- Checksums: CRC-32 IEEE, CRC-64/ECMA-182, xxHash seed 0. `ChecksumCrc32` / `ChecksumCrc64` / `ChecksumXxHash`. Not authentication.

**Status.** Implemented v1.3. Public surface is Identity, Probe, Hash/Verify string-bytes-file, HMAC-SHA256/384/512/SHA3, KMAC, SHAKE, HashPassword/VerifyPassword, Checksum*, HashingConvert, HmacKey.

## Files

| File | Role |
|---|---|
| `HashingHelper.cs` | Façade: Hash, HMAC, KMAC, SHAKE, password, checksum, verify, Probe |
| `HashingHelper.Kmac.cs` | KMAC128 / KMAC256 |
| `HashingHelper.Shake.cs` | SHAKE128 / SHAKE256 |
| `HashingAlgorithm.cs` | Sha256 default, SHA-2, SHA-3, Md5/Sha1 interop |
| `ChecksumAlgorithm.cs` | Crc32 default checksum, Crc64, XxHash32 / 64 / 3 |
| `KmacAlgorithm.cs` / `ShakeAlgorithm.cs` | KMAC128/256; SHAKE128/256 |
| `HashingTextFormat.cs` | HexLower default, HexUpper, Base64, Base64Url |
| `HashingConvert.cs` | Hex ↔ Base64 / Base64Url |
| `HmacKey.cs` / `HmacKeySize.cs` / `HmacAlgorithm.cs` | Generate / FromString / FromBase64 / FromBytes; SHA-256 / 384 / 512 / SHA-3 |
| `PasswordHash.cs` | Argon2id PHC encode/verify |
| `HashingLog.cs` | ALCOA+ Safe() |

## HMAC key footgun

```csharp
using var key = HmacKey.Generate();          // 32 random bytes
var b64 = key.ToBase64();
HmacString(msg, HmacKey.FromBase64(b64));    // RIGHT
HmacString(msg, HmacKey.FromString(b64));    // WRONG: UTF-8 of the letters
HmacString(msg, HmacKey.FromString("ops-shared-mac-key")); // RIGHT: typed secret ≥ 16 UTF-8 bytes
HmacString(msg, key, HmacAlgorithm.Sha384);  // 48-byte MAC
HmacString(msg, key, HmacAlgorithm.Sha512);  // 64-byte MAC
HmacString(msg, key, HmacAlgorithm.Sha3_256); // HMAC-SHA3-256 when OS supports it
Kmac128(msg, key);                           // NIST KMAC, not HMAC
Shake128("abc");                             // FIPS 202 XOF, 32 bytes
```

HMAC is not AES. Lengths 16/32/64/128 are not “HMAC-128 vs HMAC-256.” 64 is the SHA-256 block; 128 is hashed down inside HMAC. HMAC-SHA384 / HMAC-SHA512 / HMAC-SHA3 use the same key object. KMAC uses that same key object too.

A host that wants the Encryption trailer `hmacSha256` slot filled generates or loads an `HmacKey` here, hands the raw bytes to `EncryptionSealOptions.CallerMacKey`, and keeps that key in the same vault it would use for any other HMAC. Encryption never stores it in the envelope. The key is portable; it is not tied to a PC.

## Password vs Encryption KDF

Same Konscious package. Different product. PHC cannot Open `nathan.argon`. A 32-byte Encryption content key is not a login verifier.

## SHA-3

Call `HashingHelper.IsSupported(HashingAlgorithm.Sha3_256)` before a SHA-3 tab. Linux (OpenSSL 3) is fine. Older Windows may throw `NotSupportedException` — fail closed with that message, do not crash Probe.

## Checksums

```csharp
HashingHelper.ChecksumCrc32("123456789");   // cbf43926
HashingHelper.ChecksumCrc64("123456789");   // 6c40df5f0b497347
HashingHelper.ChecksumXxHash("abc");        // XXH64, seed 0
HashingHelper.ChecksumString(text, ChecksumAlgorithm.XxHash3);
```

CRC-32 prints catalogue hex (IEEE / ISO-HDLC). CRC-64 is ECMA-182 — not the XZ polynomial. xxHash seed is 0, matching `System.IO.Hashing`. These are not HMAC and not SHA-256; do not authenticate captures with them.

## Gallery

```
dotnet run --project src/Vestigium.Helpers.Hashing.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Hashing\`

Tabs: Overview, SHA-256, SHA-384/512, SHA-3, Checksum, HMAC (SHA-256 / 384 / 512 / SHA-3), KMAC / SHAKE, Password, MD5/SHA-1 (warning), Convert, JSONL.

## Roadmap

| Version | Work |
|---|---|
| v1.0 | Engine + gallery + converters + Argon2id PHC |
| v1.1 | CRC-32 / CRC-64 / xxHash as `Checksum*` |
| v1.2 | HMAC-SHA384 / HMAC-SHA512 |
| v1.3 (this) | HMAC-SHA3 / KMAC / SHAKE |
| Encryption v1.3 | Fill reserved trailer slots; coverage A/B/C — shipped |

Never: AES, SHA-256-as-password, inventing a hash, `ReadAllBytes` on a capture, a separate Hmac csproj.
