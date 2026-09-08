# Vestigium.Helpers.Encryption — Developers Guide

**Document ID:** VEST-HLP-ENC-DEV-000  
**Version:** 1.0  
**Status:** Design companion to SRS v1.0 (proposed)  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Encryption/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. AES-256-GCM default, ChaCha20-Poly1305 opt-in, Argon2id for passphrases, framed `VEST1` envelope for strings and large files. No hashing in this project.

## Design

**Intent.** One envelope, two AEADs, streamed frames. A password string and a 10 GB capture use the same code path.

**Locked decisions.** See SRS §2.

- No custom primitives. BCL AEAD types. One approved Argon2 package only if net10 has no Argon2id type.
- Library never calls `Initialize`. `HelperLog` APPID `Encryption`.
- Never log plaintext, keys, or passphrases.
- Large files are framed (64 KiB). Do not load the file.
- Hashing is `Vestigium.Helpers.Hashing`.
- Future AES-256-CBC is Encrypt-then-MAC and still framed.
- Future RSA wraps the 32-byte content key. It never encrypts the payload.

**Status.** Skeleton until the SRS is accepted. Public surface today is `EncryptionHelper.Identity` + `Probe()` only.

## Usage (after implementation)

```csharp
using var secret = EncryptionSecret.FromPassphrase(passphrase);
var sealedText = EncryptionHelper.SealString(token, secret);
var tokenBack = EncryptionHelper.OpenString(sealedText, secret);
var dest = EncryptionHelper.SealFile(capturePath, outPath, secret);
EncryptionHelper.OpenFile(dest, restoredPath, secret);
```

ChaCha: `EncryptionHelper.SealString(token, secret, EncryptionAlgorithm.ChaCha20Poly1305);`

Raw key: `using var secret = EncryptionSecret.FromKey(key32);`

Desktop default for the demo file dump:

```
%DESKTOP%\Vestigium\Exports\{APPID}\vestigium-{APPID}-{yyyyMMdd-HHmmss}.vest1
```

Tests pass a temp path. `Probe()` is in-memory only.

## Large-file notes

- Source must be seekable in v1 so `frameCount` can be written in the header.
- Frame size is 65 536 bytes. Last frame is short.
- File nonce = 8 random bytes + 4 zero bytes. Frame i writes i into those four bytes (big-endian).
- On any tag failure, delete the destination file the helper created.
- Do not encrypt in place.

## Files (expected after implementation)

| File | Role |
|---|---|
| `EncryptionHelper.cs` | Identity, Probe, paths, Seal/Open |
| `EncryptionSecret.cs` | Passphrase / raw key; dispose clears |
| `EncryptionAlgorithm.cs` | Aes256Gcm, ChaCha20Poly1305 |
| `Envelope.cs` | `VEST1` header read/write |
| `FrameCipher.cs` | One AEAD frame |
| `Argon2idKdf.cs` | Passphrase → 32-byte key |

## Demo

```
dotnet run --project src/Vestigium.Helpers.Encryption.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Encryption\`

## Roadmap (design)

| Version | Work |
|---|---|
| v1.0 | GCM + ChaCha + Argon2id + frames |
| v1.1 | AES-256-CBC + HMAC-SHA256, framed, not default |
| v1.2 | RSA-OAEP wraps content key; payload still GCM/ChaCha frames |
| v1.3 | Unknown-length streams |

CBC without HMAC does not ship. RSA on the file body does not ship.

## Sibling

`Vestigium.Helpers.Hashing` — string and file digests. Separate APPID. Separate gallery.
