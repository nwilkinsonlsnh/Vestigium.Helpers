# Vestigium.Helpers.Encryption — Design

**Document ID:** VEST-HLP-ENC-DSN-000  
**Version:** 1.0  
**Status:** Locked companion to SRS v1.0 (shipped through v1.2 wrap + ring)  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.0.md` wins on conflict

This page records *why* Encryption is shaped this way. It does not add requirements. Byte maps stay in the SRS.

---

## 1. Intent

One envelope for a UTF-8 string and a multi-gigabyte file. Two AEADs, one CBC interop path, streamed frames, optional RSA wrap of the content key. A password, a capture, and a file sealed to Company X share a code path.

```
caller bytes
    → EncryptionSecret (passphrase / raw key)  and/or  EncryptionRsaKey / KeyRing
         → EncryptionHelper.SealString / SealFile
              header + 64 KiB frames + VESTIGIUM TRL
         → OpenString / OpenFile
Peek / Validate inspect header+trailer without decrypting frames.
```

Hashing is a sibling. This project does not grow `HashString` / `HashFile`.

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| No custom primitives | BCL `AesGcm`, `ChaCha20Poly1305`, RSA-OAEP SHA-256. One approved Argon2 package. |
| GCM default | AES-NI on the boxes hosts ship to. ChaCha is the same contract, different math. |
| Never CBC without HMAC | Unauthenticated CBC is the design this library exists to prevent. |
| Never RSA the file body | RSA wraps the 32-byte content key. Payload `alg` stays 1 / 2 / 3. |
| Same envelope for string and file | String is Base64 of header + frames + trailer. No string-only cipher. |
| 64 KiB frames | A 10 GB capture must not sit on the heap. |
| `VESTIGIUM HDR` / `VESTIGIUM TRL` | Hex dump names the suite. Versions are numeric fields, not text in the magic. |
| Fail closed | One message: `The envelope is corrupt.` Wrong password, bit flip, and wrong company key look the same. |
| Isolation by SPKI thumbprint | Not a friendly name. AppX and AppY are two moduli. |
| Issue-and-forget default | `escrow: true` is explicit. Compromised / Retired are not overridable. |
| Never log secrets | Not even at Debug. No plaintext, keys, PKCS8, issuedTo, full thumbprints. |
| Never `Initialize` | Folder follows the host APPID. |
| Hashing stays out | Trailer `sha256` / `hmacSha256` slots are zeros until an Encryption minor fills them from Hashing. |

---

## 3. Shape

| File | Role |
|---|---|
| `EncryptionHelper.cs` | Identity, Probe, paths, Seal/Open, Peek/Validate/Reveal, SecureDelete |
| `EncryptionSecret.cs` | Passphrase / raw key; dispose clears |
| `EncryptionAlgorithm.cs` | Aes256Gcm, ChaCha20Poly1305, Aes256CbcHmac |
| `Envelope.cs` | `VESTIGIUM HDR` |
| `Trailer.cs` | `VESTIGIUM TRL`, structural mac, wrap list |
| `FrameCipher.cs` | One AEAD frame or one CBC+HMAC frame |
| `Argon2idKdf.cs` | Passphrase → 32-byte key |
| `EncryptionRsaKey.cs` | Generate / SPKI / PKCS8 / Wrap / Unwrap |
| `EncryptionKeyRing.cs` | Pairs vs contacts, token edition, JSON |
| `EncryptionKeyOverride.cs` | Disabled / Expired override |
| `EncryptionFileInfo.cs` / `EncryptionValidationResult.cs` | Peek / Validate DTOs |
| `OriginalNames.cs` | Hidden original name |
| `SecureDeleteMode.cs` | Keep / ThreePass / SevenPass |
| `EncryptionAudit.cs` | `LooksLikeSecret` |
| `EncryptionLog` / `EncryptionCatalog` / `EncryptionEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Envelope sketch

```
VESTIGIUM HDR
frames: nonce || ciphertext || tag   (CBC: IV || PKCS7 || HMAC)
VESTIGIUM TRL
    seek EOF-17 → bodyLength + magic
    seek EOF-17-bodyLength → body (mac is last 32)
```

`suiteMinor` = 2 if wraps, else 1 if CBC, else 0.

RSA wrap list sits after hidden `nameCt` and before `mac`. Open matches wrap thumbprint to one private key. It does not try every key.

Expected RAM: header + 64 KiB plaintext + 64 KiB + tag + trailer. Not the file.

On any tag failure: stop, delete the destination **file the helper created**, leave caller streams undisposed, log Failed without the key.

---

## 5. Exception policy

| Class | When |
|---|---|
| `ArgumentNullException` | Required reference is null. |
| `ArgumentException` / `ArgumentOutOfRangeException` | Bad key length, empty path, illegal override text. |
| `FileNotFoundException` | Missing source on Open / Seal. |
| `CryptographicException` | Tag fail, truncated envelope, wrong secret, wrong wrap key. Message is always `The envelope is corrupt.` |
| `EncryptionTokenException` | Ring token Disabled / Expired without override, or Retired / Compromised. |

`EncryptionLog.Failed` never takes an `Exception`. Vestigium.Logging would persist `exception.ToString()`.

---

## 6. What closed

| SRS | Outcome |
|---|---|
| v1.0 | GCM + ChaCha + Argon2id + HDR/TRL + hidden name + `.aes` / `.argon` + Peek/Validate + optional shred |
| v1.1 | AES-256-CBC+HMAC framed, not default |
| v1.2 | RSA-OAEP wrap list, key ring, token edition + override |

Package version on `main` is 1.3.0. Public-key trailer signature is still out.

---

## 7. Still out

Public-key trailer signature, frameSize override, filling trailer hash/HMAC slots from Hashing, PBKDF2 as default KDF, password-protected workbooks, a second envelope family, `SealHugeFile` sibling, RSA on the payload, CBC without HMAC.

---

## 8. Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Sep 2026 | First standalone Design. Content lifted from Guide v1.0 + shipped helper/ring shape. |
