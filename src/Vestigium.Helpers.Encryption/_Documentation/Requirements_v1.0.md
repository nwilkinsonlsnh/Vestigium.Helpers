# Vestigium.Helpers.Encryption — Requirements Specification

**Document ID:** VEST-HLP-ENC-SRS-000  
**Version:** 1.0  
**Status:** Proposed (lossless; implementation follows acceptance of this document)  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Encryption`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

This replaces the 7 September 2026 skeleton note. If implementation and this file disagree, this file wins. Implementation is a separate change.

---

## 0. How to read this document

It records:

- two AEAD ciphers for **strings and files**: AES-256-GCM (default) and ChaCha20-Poly1305
- one password KDF: Argon2id
- one on-disk / on-wire envelope so a string and a 10 GB file share a format
- framed streaming so large files never sit in RAM
- HelperLog only; library never calls `VestigiumLogger.Initialize`
- hashing **out of this library** — sibling `Vestigium.Helpers.Hashing`
- AES-256-CBC and RSA on the roadmap, still required to stream large files when they land

---

## 1. Purpose

Give every Vestigium host one way to lock a UTF-8 string or a file so an operator can park a secret, a capture, or an export without inventing a cipher.

```csharp
var sealedText = EncryptionHelper.SealString("token", secret);
var plain = EncryptionHelper.OpenString(sealedText, secret);

EncryptionHelper.SealFile(src, dest, secret);   // streams; dest is framed AEAD
EncryptionHelper.OpenFile(dest, restored, secret);
```

`secret` is either a 32-byte key or a passphrase. Passphrases go through Argon2id. Raw keys skip the KDF.

This is **not** TLS, BitLocker, DPAPI, or a key vault. It is authenticated encryption of caller bytes.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Ciphers (v1) | **AES-256-GCM** default. **ChaCha20-Poly1305** opt-in. Both AEAD. |
| 2 | Password KDF | **Argon2id**. Raw 32-byte keys skip it. |
| 3 | String and file | Same envelope. String is one frame. File is N frames. |
| 4 | Large files | Streamed framed AEAD. Never `File.ReadAllBytes` the plaintext or ciphertext. |
| 5 | Engine | BCL (`AesGcm`, `ChaCha20Poly1305`, `RandomNumberGenerator`). Argon2id: BCL if present on net10; otherwise one approved NuGet (`Konscious.Security.Cryptography.Argon2`). No home-grown S-boxes. |
| 6 | Hashing | **Out.** Sibling `Vestigium.Helpers.Hashing`. This project must not grow `Hash*` APIs. |
| 7 | Logging | `HelperLog` only. APPID = host (demo: `Encryption`). Never log plaintext, keys, passphrases, or raw salts as secrets-to-reuse. Log alg, bytes in/out, path **names**, frame count. |
| 8 | Initialize | Library never calls `VestigiumLogger.Initialize`. |
| 9 | Export folder | Encrypted **files** the demo writes go to `%DESKTOP%\\Vestigium\\Exports\\{APPID}\\`. Tests pass a temp path. |
| 10 | CBC / RSA | Roadmap only. When they ship they must still stream large files (CBC framed + HMAC; RSA wraps the content key, never the payload). |

---

## 3. Algorithms (v1)

### 3.1 AES-256-GCM — default

- 256-bit key.
- 96-bit nonce per frame.
- 128-bit tag per frame.
- Associated data = envelope header bytes that do not change per frame (magic, version, alg, kdf, salt, file nonce, frame size) plus the 32-bit frame index.
- .NET type: `System.Security.Cryptography.AesGcm`.

Hardware AES-NI on typical PingIQ boxes. Default for `SealString` / `SealFile` when the caller does not pick an algorithm.

### 3.2 ChaCha20-Poly1305 — opt-in

- 256-bit key.
- 96-bit nonce per frame.
- 128-bit tag per frame.
- Same AAD rule as GCM.
- .NET type: `System.Security.Cryptography.ChaCha20Poly1305`.

Same security contract, different math. Use when the host asks, or when a later profile prefers it on boxes without AES-NI.

### 3.3 Argon2id — passphrase to key

When the caller passes a passphrase:

- Generate a 16-byte random salt, store it in the envelope.
- Argon2id → 32-byte content key.
- Parameters locked for v1 (must be written into the envelope so a future bump can change them):

| Parameter | v1 value |
|---|---|
| Memory | 64 MiB |
| Iterations | 3 |
| Parallelism | 1 |
| Hash length | 32 bytes |
| Salt length | 16 bytes |

A raw key path exists: `EncryptionSecret.FromKey(byte[32])`. That path sets KDF id = 0 and writes no salt.

Do not SHA-256 a password. Do not use PBKDF2 as the default. PBKDF2-SHA256 may appear later as an interop KDF; it is not v1.

### 3.4 Explicitly not v1 ciphers

AES-256-CBC, AES-128, 3DES, RC4, Blowfish, RSA payload encryption, “XOR with password”, unauthenticated CTR/CBC.

---

## 4. Large files (the workaround)

AES-GCM and ChaCha20-Poly1305 are both **record** AEADs. One nonce must not cover an unbounded stream. GCM in particular should stay well under 2^32 blocks (~64 GiB) **per nonce**.

v1 workaround, required for both algorithms:

1. Draw one **file nonce** (12 bytes) at Seal time.
2. Split plaintext into frames of **65 536 bytes** (last frame may be shorter).
3. For frame i (0-based): nonce = fileNonce with the last 4 bytes overwritten by i as big-endian (fileNonce is generated as 8 random bytes + 4 zero bytes).
4. Encrypt that frame with AEAD; AAD includes the header and i.
5. Write `ciphertext || tag` (tag is 16 bytes).
6. Never buffer more than one frame of plaintext and one frame of ciphertext.
7. A file whose plaintext is 0 bytes is legal: header only, frame count 0.
8. Decrypt streams the same way. Tag mismatch on any frame throws `CryptographicException` and **stops**. Partial output files must be deleted by the helper on failure.

String APIs use the same code path with one frame (or zero if the string is empty).

Do not switch algorithm just because the file is large. Framing is the large-file story for AES-GCM, ChaCha20-Poly1305, and — when they ship — AES-CBC and RSA-wrapped payloads.

---

## 5. Envelope (`VEST1`)

Binary, little-endian integers except the frame-index-in-nonce (big-endian).

```
magic[5]     "VEST1"
version      u8     = 1
alg          u8     = 1 AES-256-GCM, 2 ChaCha20-Poly1305
kdf          u8     = 0 raw key, 1 Argon2id
kdfMemMiB    u8     = 64 when kdf=1, else 0
kdfIter      u8     = 3  when kdf=1, else 0
kdfPar       u8     = 1  when kdf=1, else 0
salt         16 bytes when kdf=1, else omitted
fileNonce    12 bytes
frameSize    u32    = 65536
frameCount   u64    number of frames that follow
frames       frameCount × (ciphertext || 16-byte tag)
```

`frameCount` is known up front for files (size / 65536). For streams of unknown length, v1 still requires a seekable source so the helper can write `frameCount` before frames. A later milestone may add a streaming trailer; not v1.

String output is this blob, Base64 (`Convert.ToBase64String`, standard alphabet, no line breaks).

Unknown `version` / `alg` / `kdf` on Open throws `NotSupportedException` naming the field.

---

## 6. Public surface (v1)

Names may move a token. The shapes may not.

```csharp
public static class EncryptionHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Encryption"
    public static string Probe();

    public static string DefaultExportDirectory(string appId);
    public static string NewExportPath(string appId, string? stem = null);

    public static string SealString(string plaintext, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm);
    public static string OpenString(string sealedBase64, EncryptionSecret secret);

    public static string SealFile(string sourcePath, string destinationPath, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm);
    public static string OpenFile(string sourcePath, string destinationPath, EncryptionSecret secret);

    public static void SealFile(Stream source, Stream destination, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, long plaintextLength);
    public static void OpenFile(Stream source, Stream destination, EncryptionSecret secret);
}

public enum EncryptionAlgorithm { Aes256Gcm = 1, ChaCha20Poly1305 = 2 }

public sealed class EncryptionSecret : IDisposable
{
    public static EncryptionSecret FromPassphrase(string passphrase);
    public static EncryptionSecret FromKey(ReadOnlySpan<byte> key32);
    public void Dispose();
}
```

Rules:

- `SealString` / `OpenString` take and return strings. Plaintext is UTF-8. Ciphertext is Base64 of a `VEST1` blob.
- File overloads create the destination directory, stream, and return the destination path.
- Stream overloads do **not** dispose caller streams. `plaintextLength` is required on Seal so `frameCount` can be written.
- `FromKey` throws if the span is not 32 bytes.
- `FromPassphrase` throws if blank.
- `Dispose` clears the derived or copied key material.
- `Open*` reads `alg` and `kdf` from the envelope. The caller does not pass the algorithm on decrypt.
- Wrong passphrase / wrong key / bit flip → `CryptographicException`. Do not distinguish “wrong password” from “corrupt file” in the exception message.
- `Probe` seals and opens a short fixture in memory. It must not write `%DESKTOP%` and must not log the fixture text.

---

## 7. Logging

| Event | Level | Status |
|---|---|---|
| Seal start (alg, kdf, byte/frame counts — not secrets) | Information | Pending |
| Seal complete (path or string, frames, bytes) | Information | Success |
| Open start | Information | Pending |
| Open complete | Information | Success |
| Tag mismatch / crypt failure | Error | Failed |
| Missing source file | Error | Failed |
| Unsupported envelope field | Error | Failed |

Category = `Helpers`. Subcategory = `Encryption`. APPID = host APPID.

Never log: plaintext, passphrase, key bytes, salt that could be reused as a key, Base64 ciphertext.

---

## 8. Demo contract

`Vestigium.Helpers.Encryption.Demo` stays a skeleton gallery until implementation. After implementation:

1. `HelperWpfHost.Start` with APPID `Encryption`.
2. `EncryptionHelper.Probe()`.
3. Seal a short string with a throwaway gallery passphrase; Open it back; print only lengths and alg.
4. Seal a small temp file into `%DESKTOP%\\Vestigium\\Exports\\Encryption\\`.
5. JSONL under `%ProgramData%\\Vestigium\\Logs\\Encryption\\`.

---

## 9. Tests

xUnit, serial logger collection, temp directories only. After implementation:

- Identity is `Vestigium.Helpers.Encryption`.
- Probe writes Pending then Success; no Desktop file.
- String round-trip including non-ASCII.
- Wrong passphrase and tampered ciphertext throw `CryptographicException`.
- ChaCha20-Poly1305 string round-trip.
- Raw 32-byte key path round-trip (no salt in envelope).
- File Seal / Open of a 200 KiB buffer (more than one frame) matches the original bytes.
- Empty string and empty file round-trip.
- Missing source throws `FileNotFoundException`.
- Tests never use the real Desktop.

---

## 10. Non-goals (v1)

Hashing APIs, AES-256-CBC, RSA payload encrypt, DPAPI, key vault, compress-then-encrypt, encrypting Vestigium JSONL, replacing FileIo, in-place encrypt of the source file.

---

## 11. Roadmap

### v1.0 — this document

AES-256-GCM default, ChaCha20-Poly1305 opt-in, Argon2id or raw key, `VEST1` framed envelope, string Base64 + streamed files, HelperLog, demo, tests.

### v1.1 — AES-256-CBC (interop)

Algorithm id 3. **Encrypt-then-MAC**: AES-256-CBC then HMAC-SHA256 over header + frame index + IV + ciphertext. Still framed (64 KiB). Per-frame IV. Not the default. Reject CBC without HMAC.

### v1.2 — RSA envelope wrap

Payload stays AES-256-GCM or ChaCha frames. RSA-OAEP (SHA-256) encrypts the **32-byte content key** only. Minimum 2048-bit; prefer 3072. Encrypting the file body with RSA is forbidden in this library, including later versions.

### v1.3

Unknown-length stream Seal (trailer with frame count). Optional frameSize override.

### Explicitly never here

Custom ciphers, hashing, TLS, full-disk encryption, logging JSONL as ciphertext in ProgramData.

---

## 12. Sibling: Vestigium.Helpers.Hashing

Created as a skeleton in the same change as this SRS. Hashing must not encrypt. Encryption must not hash on the public surface.

---

## 13. Acceptance

This SRS is accepted when this file is on `main` under `src/Vestigium.Helpers.Encryption/_Documentation/` and a follow-up implementation PR can be reviewed against this text without inventing a third v1 cipher or hashing APIs. Implementation is a separate change.
