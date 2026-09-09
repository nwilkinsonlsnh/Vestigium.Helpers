# Vestigium.Helpers.Hashing — Developers Guide

**Document ID:** VEST-HLP-HASH-DEV-000  
**Version:** 1.0  
**Status:** Implemented v1.0  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Hashing/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. SHA-256 default (lowercase hex), SHA-384/512 and SHA-3 opt-in, HMAC-SHA256, Argon2id PHC password verifiers, hex↔Base64 converters. Not encryption. HMAC is in this project; there is no `Vestigium.Helpers.Hmac` sibling.

## Design

**Intent.** Digests of strings and files so a host can say “this capture did not change” without pulling Encryption. Keyed HMAC for a shared MAC secret. Password verifiers for stored credentials. Same 64 KiB stream rule as Encryption.

**Locked.** See SRS §2. The ones that must not drift:

- Default print is **lowercase hex**. Base64 is a converter / format argument, not the default.
- HMAC key min 16, generate default 32, enum 16/32/64/128. `FromString` is UTF-8. `FromBase64` decodes. Do not mix them.
- `HashPassword` is Argon2id PHC (19 MiB / t=2 / p=1). Encryption KDF stays 64 MiB / t=3 / p=1 and still produces a content key.
- File APIs stream. Never `ReadAllBytes`.
- Library never calls `Initialize`. APPID `Hashing`.
- Never log input, HMAC keys, passwords, salts, PHC. File digest hex is the audit record.
- Do not fill Encryption trailer slots from this project.

**Status.** Implemented. Public surface is Identity, Probe, Hash/Verify string-bytes-file, HMAC, HashPassword/VerifyPassword, HashingConvert, HmacKey.

## Files

| File | Role |
|---|---|
| `HashingHelper.cs` | Façade: Hash, HMAC, password, verify, Probe |
| `HashingAlgorithm.cs` | Sha256 default, SHA-2, SHA-3, Md5/Sha1 interop |
| `HashingTextFormat.cs` | HexLower default, HexUpper, Base64, Base64Url |
| `HashingConvert.cs` | Hex ↔ Base64 / Base64Url |
| `HmacKey.cs` / `HmacKeySize.cs` | Generate / FromString / FromBase64 / FromBytes |
| `PasswordHash.cs` | Argon2id PHC encode/verify |
| `HashingLog.cs` | ALCOA+ Safe() |

## HMAC key footgun

```csharp
using var key = HmacKey.Generate();          // 32 random bytes
var b64 = key.ToBase64();
HmacString(msg, HmacKey.FromBase64(b64));    // RIGHT
HmacString(msg, HmacKey.FromString(b64));    // WRONG: UTF-8 of the letters
HmacString(msg, HmacKey.FromString("ops-shared-mac-key")); // RIGHT: typed secret ≥ 16 UTF-8 bytes
```

HMAC is not AES. Lengths 16/32/64/128 are not “HMAC-128 vs HMAC-256.” 64 is the SHA-256 block; 128 is hashed down inside HMAC.

## Password vs Encryption KDF

Same Konscious package. Different product. PHC cannot Open `nathan.argon`. A 32-byte Encryption content key is not a login verifier.

## SHA-3

Call `HashingHelper.IsSupported(HashingAlgorithm.Sha3_256)` before a SHA-3 tab. Linux (OpenSSL 3) is fine. Older Windows may throw `NotSupportedException` — fail closed with that message, do not crash Probe.

## Gallery

```
dotnet run --project src/Vestigium.Helpers.Hashing.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Hashing\`

Tabs: Overview, SHA-256, SHA-384/512, SHA-3, HMAC-SHA256, Password, MD5/SHA-1 (warning), Convert, JSONL.

## Roadmap

| Version | Work |
|---|---|
| v1.0 (this) | Engine + gallery + converters + Argon2id PHC |
| later | CRC32 / CRC64 / xxHash as `Checksum*` |
| later | HMAC-SHA384/512 |
| Encryption v1.3 | Fill reserved trailer slots; coverage A/B/C |

Never: AES, SHA-256-as-password, inventing a hash, `ReadAllBytes` on a capture, a separate Hmac csproj.
