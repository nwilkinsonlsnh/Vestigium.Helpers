# Vestigium.Helpers.Encryption — Developers Guide

**Document ID:** VEST-HLP-ENC-DEV-000  
**Version:** 1.0  
**Status:** Implemented v1.0  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Encryption/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. AES-256-GCM default, ChaCha20-Poly1305 opt-in, Argon2id for passphrases, `VESTIGIUM HDR` / `VESTIGIUM TRL` envelope. Hashing and Hmac are siblings; this project only reserves their trailer slots.

This file is the design companion: how the pieces sit, how large files stay off the heap, and how CBC / RSA can land later without a second envelope family.

## Design

**Intent.** One envelope, two AEADs, streamed frames. A password string and a 10 GB capture use the same code path.

**Locked decisions.** See SRS §2. The ones that must not drift:

- No custom primitives. BCL AEAD types. One approved Argon2 package only if net10 has no Argon2id type.
- Library never calls `Initialize`. `HelperLog` APPID `Encryption`.
- Never log plaintext, keys, or passphrases.
- Large files are framed (64 KiB). Do not load the file.
- Every blob ends in a `VESTIGIUM TRL` footer. v1.0 body is 465 bytes plus 17-byte length+magic (482 at EOF).
- Hashing is `Vestigium.Helpers.Hashing`. Hmac is a later sibling. Do not fill their 32-byte slots in v1.0.
- Future AES-256-CBC is Encrypt-then-MAC and still framed.
- Future RSA wraps the 32-byte content key. It never encrypts the payload.

**Status.** Implemented. Public surface is Identity, Probe, Seal/Open string and file, Peek/Validate/RevealOriginalFileName, `.aes` / `.argon` names. Hashing remains a sibling. CBC and RSA stay on the roadmap.

## Why these three, not five

| Algorithm | Role now | Role later |
|---|---|---|
| AES-256-GCM | Default AEAD. AES-NI on the boxes we ship to. | Stays default. |
| ChaCha20-Poly1305 | Same contract, different math. Opt-in. | Soft default on hosts without AES-NI if a profile asks. |
| Argon2id | Only passphrase → key path. | Parameters may bump; id and params live in the header. |
| AES-256-CBC | Not v1. | Interop only, with HMAC, framed. |
| RSA-OAEP | Not v1. | Wraps the content key. Payload still AEAD frames. |

Unauthenticated AES-CBC and “RSA the whole file” are the two designs this library exists to prevent.

## Envelope layout (design)

See SRS §6 for the byte map. Design notes:

- Magics are `VESTIGIUM HDR` / `VESTIGIUM TRL` so a hex dump names the suite. Version is four numeric bytes, not `"1.0"` inside the string.
- Algorithm and KDF are single bytes so Open can branch before touching frames.
- Argon2 parameters ride in the header. A future memory bump does not require a new magic.
- Header `frameCount` is a hint when the source length is known. The trailer is authoritative. Header `0` + flag bit 0 means unknown-length Seal.
- AEAD AAD is the header through `frameSize` only, so a zero header count does not change the frame tags.
- String output is the same blob including the trailer, Base64. There is no “string-only” cipher.

## Trailer (design)

See SRS §6.2 for the byte map.

Seek path:

```
seek EOF-17 → read bodyLength (u32) + "VESTIGIUM TRL"
seek EOF-17-bodyLength → parse body (465 bytes in v1.0)
```

`bodyLength` is the seek contract so a later minor version can grow the body. v1.0 still writes 465.

HMAC key is HKDF-SHA256(contentKey, salt=fileNonce, info=`VESTIGIUM-TRL-HMAC`). That is Encryption’s structural `mac`. The 32-byte `sha256` and `hmacSha256` slots stay zeros until Hashing / Hmac exist.

`IsVestigiumFile` / `PeekFile` / `ValidateFile` read this record. Peek needs no secret. Validate with a secret checks `mac` and still does not decrypt frames.

Expected extra types after implementation: `Trailer.cs`, `EncryptionFileInfo.cs`, `EncryptionValidationResult.cs`.

## Large-file pump (design)

```
write header (frameCount known, or 0 if unknown)
loop
    read up to 65536 plaintext bytes (stop at EOF)
    nonce = fileNonce[0..7] || BE32(i)
    ciphertext || tag = AEAD(key, nonce, aad=headerPrefix||i, plaintext)
    write ciphertext || tag
write VESTIGIUM TRL (versions, counts, zeroed hash/HMAC slots, structural mac)
dispose secret
```

Open is the inverse. On any tag failure:

1. Stop.
2. Delete the destination **file** the helper created.
3. Leave caller streams undisposed.
4. Log Failed without the key.

Do not encrypt in place. Do not write frames into the source path.

Expected RAM: header + 64 KiB plaintext + 64 KiB + 16 ciphertext/tag + 482-byte trailer. Not the file.

## Usage (after implementation)

```csharp
using var secret = EncryptionSecret.FromPassphrase(passphrase);

var sealedText = EncryptionHelper.SealString(token, secret);
var tokenBack = EncryptionHelper.OpenString(sealedText, secret);

var dest = EncryptionHelper.SealFile(capturePath, outPath, secret);
EncryptionHelper.OpenFile(dest, restoredPath, secret);
```

ChaCha:

```csharp
EncryptionHelper.SealString(token, secret, EncryptionAlgorithm.ChaCha20Poly1305);
```

Raw key (no Argon2):

```csharp
using var secret = EncryptionSecret.FromKey(key32);
```

Visible names are short. The original name is hidden in the trailer:

```
nathan.txt  + raw key      →  %DESKTOP%\Vestigium\Exports\{APPID}\nathan.aes
nathan.txt  + passphrase   →  %DESKTOP%\Vestigium\Exports\{APPID}\nathan.argon
Open into a folder         →  nathan.txt
```

```csharp
var sealedPath = EncryptionHelper.SealFile("nathan.txt", exportDir, secret);
var check = EncryptionHelper.ValidateFile(sealedPath);                 // no secret; no nathan.txt
var info  = EncryptionHelper.PeekFile(sealedPath);                     // suite 1.0, alg, sizes, HasHiddenOriginalName
var name  = EncryptionHelper.RevealOriginalFileName(sealedPath, secret); // nathan.txt
EncryptionHelper.OpenFile(sealedPath, exportDir, secret);              // writes nathan.txt
```

`OpenFile` ignores the visible suffix. `.vest`, `.vestigium`, `.aes.gcm` remain Open aliases. Tests pass a temp path and use `*.aes` / `*.argon`.

`Probe()` is in-memory only.

## Files (expected after implementation)

| File | Role |
|---|---|
| `EncryptionHelper.cs` | Identity, Probe, paths, Seal/Open, IsVestigium/Peek/Validate/RevealOriginalFileName |
| `EncryptionSecret.cs` | Passphrase / raw key; dispose clears |
| `EncryptionAlgorithm.cs` | Aes256Gcm, ChaCha20Poly1305 |
| `EncryptionFileInfo.cs` | Peek DTO (suite version, alg, sizes) |
| `EncryptionValidationResult.cs` | Validate DTO |
| `Envelope.cs` | `VESTIGIUM HDR` read/write |
| `Trailer.cs` | `VESTIGIUM TRL` write / parse / mac / peek |
| `FrameCipher.cs` | One AEAD frame |
| `OriginalNames.cs` | Bare file name rules; hidden `nathan.txt` |
| `Argon2idKdf.cs` | Passphrase → 32-byte key |

Do not add a Hashing implementation here. Do not add CBC or RSA types until those SRS revisions.

## Logging

Category `Helpers`, subcategory `Encryption`, APPID = host.

Safe to log: algorithm name, KDF id, plaintext **length**, frame count, destination **path**.

Never log: passphrase, key, salt-as-reusable-secret, plaintext, Base64 blob, hidden original name. Visible path (`nathan.aes`) is fine.

Library never calls `VestigiumLogger.Initialize`. If the host has not started logging, Seal/Open still work; log calls are no-ops.

## Demo

```
dotnet run --project src/Vestigium.Helpers.Encryption.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Encryption\`

The demo probes in memory, then seals a temp `nathan.txt` to the Desktop export folder as `nathan.argon` and opens it back.

## Roadmap (design)

| Version | Work | Large-file workaround |
|---|---|---|
| v1.0 | GCM + ChaCha + Argon2id + VESTIGIUM HDR/TRL | 64 KiB frames; hidden original name; `.aes` / `.argon`; Peek/Validate |
| v1.1 | AES-256-CBC + HMAC-SHA256, framed, not default | Per-frame IV + per-frame HMAC; PKCS#7 per frame |
| v1.2 | RSA-OAEP wraps content key; payload still GCM/ChaCha frames | Hybrid: RSA cost is one wrap; file still streams |
| v1.3 | Public-key trailer sig / frameSize override | Flag bit 2; keep EOF magic so Peek still works |

CBC without HMAC does not ship. RSA on the file body does not ship.

When v1.1 / v1.2 land, they extend `EncryptionAlgorithm` and the header `alg` byte. They do not invent a second magic. They do not get a `SealHugeFile` sibling — `SealFile` stays the file API.

## Sibling

`Vestigium.Helpers.Hashing` — string and file digests. Separate APPID. Separate gallery. Encryption tests may call Hashing (or BCL `SHA256`) to compare file bytes after a round-trip. Encryption must not grow `HashString` / `HashFile`.

`Vestigium.Helpers.Hmac` is planned, not created yet. Trailer already reserves its 32-byte slot.
