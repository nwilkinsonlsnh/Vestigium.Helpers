# Vestigium.Helpers.Encryption — Developers Guide

**Document ID:** VEST-HLP-ENC-DEV-000  
**Version:** 1.0  
**Status:** Implemented v1.0 + v1.1 CBC + v1.2 RSA-OAEP wrap + key ring + token enable/disable/expire + ALCOA+ logs.  
**Date:** 8 September 2026

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Encryption/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. AES-256-GCM default, ChaCha20-Poly1305 opt-in, AES-256-CBC+HMAC v1.1 interop, Argon2id for passphrases, RSA-OAEP-SHA256 wrap of the 32-byte content key (v1.2), `VESTIGIUM HDR` / `VESTIGIUM TRL` envelope. Hashing (`Vestigium.Helpers.Hashing`) is the sibling for digests, HMAC-SHA256, and Argon2id PHC; this project only reserves their trailer slots. There is no `Vestigium.Helpers.Hmac` project and no separate KeyRing library.

This file is the design companion: how the pieces sit, how large files stay off the heap, and how RSA wrap lands on the **same** envelope family.

## Design

**Intent.** One envelope, two AEADs, one CBC interop path, streamed frames, optional RSA wrap list. A password string, a 10 GB capture, and a file sealed to Company X share a code path.

**Locked decisions.** See SRS §2. The ones that must not drift:

- No custom primitives. BCL AEAD types and BCL RSA-OAEP SHA-256. One approved Argon2 package only if net10 has no Argon2id type.
- Library never calls `Initialize`. `HelperLog` APPID `Encryption`.
- Never log plaintext, keys, passphrases, PKCS8, company names, issuedTo, or full thumbprints — even at Debug. Override actor + reason only after `LooksLikeSecret` rejection.
- Large files are framed (64 KiB). Do not load the file.
- Every blob ends in a `VESTIGIUM TRL` footer. v1.0 body is 465 bytes plus 17-byte length+magic (482 at EOF) when there is **no** wrap list. Wrap list grows the body; `bodyLength` is the seek contract. VerifyMac uses `body.Length - 32`.
- Hashing is `Vestigium.Helpers.Hashing` (SHA-2/SHA-3, HMAC-SHA256, Argon2id PHC). Do not fill the 32-byte trailer slots from Encryption v1.2. There is no Hmac sibling project.
- AES-256-CBC is Encrypt-then-MAC and still framed (v1.1, not default).
- RSA wraps the 32-byte content key. It never encrypts the payload. Payload `alg` stays 1 / 2 / 3. Suite minor 2 when wraps are present. Flag bit 5 (`32`), not bit 2.
- Isolation is per public key (SHA-256 of SPKI). Trailer stores thumbprints only.
- One private key is one modulus. Many pairs per `issuedTo` if a company needs AppX and AppY.
- Issue-and-forget is the default. `escrow: true` is explicit.
- Token edition lives on the ring: Enable / Disable / Expire / Retire / Compromise. Disabled and Expired need `EncryptionKeyOverride`. Compromised and Retired fail closed.

**Status.** Implemented. Public surface is Identity, Probe, Seal/Open string and file, Peek/Validate/RevealOriginalFileName, `.aes` / `.argon` names, optional 3- or 7-pass + zero secure delete of the unencrypted source, AES-256-CBC+HMAC (alg 3, suite 1.1), RSA-OAEP wrap list (suite 1.2), `EncryptionRsaKey`, `EncryptionKeyRing` (status, expiry, override), `EncryptionKeyOverride`, `EncryptionTokenException`. Hashing is a shipped sibling (digests, HMAC-SHA256, Argon2id PHC) and does not fill trailer slots in this revision.

## Why these algorithms

| Algorithm | Role now | Role later |
|---|---|---|
| AES-256-GCM | Default AEAD. AES-NI on the boxes we ship to. | Stays default. |
| ChaCha20-Poly1305 | Same contract, different math. Opt-in. | Soft default on hosts without AES-NI if a profile asks. |
| Argon2id | Only passphrase → key path. | Parameters may bump; id and params live in the header. |
| AES-256-CBC + HMAC | v1.1 interop. Encrypt-then-MAC, framed, not default. | Per-frame IV + HMAC. Hidden name stays GCM. |
| RSA-OAEP-SHA256 | v1.2 wrap of the 32-byte content key. Wrap list 1..8. | Payload still AEAD/CBC frames. Never RSA on the file body. |

Unauthenticated AES-CBC and “RSA the whole file” are the two designs this library exists to prevent.

## Envelope layout (design)

See SRS §6 for the byte map. Design notes:

- Magics are `VESTIGIUM HDR` / `VESTIGIUM TRL` so a hex dump names the suite. Version is four numeric bytes, not `"1.0"` inside the string.
- Algorithm and KDF are single bytes so Open can branch before touching frames.
- Argon2 parameters ride in the header. A future memory bump does not require a new magic.
- Header `frameCount` is a hint when the source length is known. The trailer is authoritative. Header `0` + flag bit 0 means unknown-length Seal.
- AEAD AAD is the header through `frameSize` only, so a zero header count does not change the frame tags.
- String output is the same blob including the trailer, Base64. There is no “string-only” cipher.
- RSA wrap does not change the header layout. Suite minor 2 is the only header signal besides the trailer wrap region.

`suiteMinor` = 2 if wraps, else 1 if CBC, else 0.

## Trailer (design)

See SRS §6.2 for the byte map.

Seek path:

```
seek EOF-17 → read bodyLength (u32) + "VESTIGIUM TRL"
seek EOF-17-bodyLength → parse body (465 bytes with no wraps; longer with a wrap list)
```

`bodyLength` is the seek contract so a later minor version can grow the body. v1.0 still writes 465 when there are no wraps.

Fixed prefix through `nameCt` is 433 bytes. Wrap region (optional) sits between `nameCt` and `mac`. MAC is always the last 32 of the body.

HMAC key is HKDF-SHA256(contentKey, salt=fileNonce, info=`VESTIGIUM-TRL-HMAC`). That is Encryption’s structural `mac`. The 32-byte `sha256` and `hmacSha256` slots stay zeros until Hashing / Hmac exist.

`IsVestigiumFile` / `PeekFile` / `ValidateFile` read this record. Peek needs no secret. Validate with a secret checks `mac` and still does not decrypt frames. Peek of a wrap file may list thumbprints; it never lists company names.

Wrap record: `wrapAlg u8` + `keyBits u16 LE` + 32-byte thumbprint + `wrappedLen u16 LE` + wrapped key. Count 1..8. `FlagHasWrap = 32`.

## RSA wrap (design)

```
contentKey = derive(secret) or random 32 bytes
for each recipient (max 8)
    wrapped = RSA-OAEP-SHA256(recipient.public, contentKey)
    emit wrapAlg=1, keyBits, SHA256(SPKI), wrapped
payload frames as today
trailer mac over prefix + wrap bytes
```

Open:

```
if secret present and trailer mac verifies → use that content key
else match wrap thumbprint to a private (caller key or ring pair)
    ring.FindPrivate(thumbprint, override) → RequireForOpen
        Active → unwrap
        Disabled / Expired without override → EncryptionTokenException + Warning Refuse
        Disabled / Expired with override → Warning Override, then unwrap
        Retired / Compromised → EncryptionTokenException (not overridable)
    unwrap → verify mac
else fail closed: The envelope is corrupt.
```

Do not try every private key. Do not distinguish “wrong company” from “corrupt.”

Thumbprint = SHA-256 of SubjectPublicKeyInfo DER so C# and Web Crypto agree.

## Key ring (design)

JSON `VESTIGIUM-KEYRING` 1.0. Two arrays: `pairs` (may include PKCS8) and `contacts` (SPKI only).

`Issue` copies PKCS8 into the returned slip (`FromPkcs8`) and disposes the generated key when not escrowed, so the ring does not double-dispose.

Field clamps: title 75, subject 50, description 220, issuedTo 75, application 50.

The ring file on disk should itself be a Vestigium envelope (host Seals the JSON). `ToJson` is plaintext so tests can round-trip without a second envelope.

## Large-file pump (design)

```
write header (frameCount known, or 0 if unknown; suiteMinor 2 if wraps)
loop
    read up to 65536 plaintext bytes (stop at EOF)
    nonce = fileNonce[0..7] || BE32(i)
    ciphertext || tag = AEAD(key, nonce, aad=headerPrefix||i, plaintext)
    write ciphertext || tag
write VESTIGIUM TRL (versions, counts, zeroed hash/HMAC slots, optional wrap list, structural mac)
dispose secret
```

Open is the inverse. On any tag failure:

1. Stop.
2. Delete the destination **file** the helper created.
3. Leave caller streams undisposed.
4. Log Failed without the key.

Do not encrypt in place. Do not write frames into the source path.

Expected RAM: header + 64 KiB plaintext + 64 KiB + 16 ciphertext/tag + trailer (482 bytes, or a few KB with wraps). Not the file.

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

RSA wrap (Company X cannot be opened by Company Y):

```csharp
using var ring = EncryptionKeyRing.Create("Ops");
using var ops = EncryptionRsaKey.Generate(2048); // tests; default Generate is 3072
ring.AddPair("Ops receive", "Ops", "Wilkinson", application: "Gallery");

var (appX, slipX) = ring.Issue("Company X AppX", "AppX wrap", "CompanyX", application: "ApplicationX");
var (appY, slipY) = ring.Issue("Company X AppY", "AppY wrap", "CompanyX", application: "ApplicationY");

var sealedToX = EncryptionHelper.SealString(token, [appX.Key], alsoWrapTo: ops);
EncryptionHelper.OpenString(sealedToX, slipX);   // ok
EncryptionHelper.OpenString(sealedToX, ops);     // also wrap to me
// EncryptionHelper.OpenString(sealedToX, slipY); // CryptographicException
```

Visible names are short. The original name is hidden in the trailer:

```
nathan.txt  + raw key      →  %DESKTOP%\Vestigium\Exports\{APPID}\nathan.aes
nathan.txt  + passphrase   →  %DESKTOP%\Vestigium\Exports\{APPID}\nathan.argon
nathan.txt  + RSA wrap     →  %DESKTOP%\Vestigium\Exports\{APPID}\nathan.aes
Open into a folder         →  nathan.txt
```

```csharp
var sealedPath = EncryptionHelper.SealFile("nathan.txt", exportDir, secret);
var check = EncryptionHelper.ValidateFile(sealedPath);                 // no secret; no nathan.txt
var info  = EncryptionHelper.PeekFile(sealedPath);                     // suite 1.0 / 1.1 / 1.2, alg, sizes, HasHiddenOriginalName, wrap count
var name  = EncryptionHelper.RevealOriginalFileName(sealedPath, secret); // nathan.txt
EncryptionHelper.OpenFile(sealedPath, exportDir, secret);              // writes nathan.txt
EncryptionHelper.SealFile("nathan.txt", exportDir, secret, shredPlaintext: SecureDeleteMode.ThreePass);
EncryptionHelper.SecureDelete(plaintextPath, SecureDeleteMode.SevenPass);
```

`OpenFile` ignores the visible suffix. `.vest`, `.vestigium`, `.aes.gcm` remain Open aliases. Tests pass a temp path and use `*.aes` / `*.argon`.

`Probe()` is in-memory only.

## Files (expected after implementation)

| File | Role |
|---|---|
| `EncryptionHelper.cs` | Identity, Probe, paths, Seal/Open, IsVestigium/Peek/Validate/RevealOriginalFileName, SecureDelete, RSA overloads |
| `SecureDeleteMode.cs` | Keep / ThreePass (3 random + zero) / SevenPass (7 random + zero) |
| `EncryptionSecret.cs` | Passphrase / raw key; dispose clears |
| `EncryptionAlgorithm.cs` | Aes256Gcm, ChaCha20Poly1305, Aes256CbcHmac (no RSA alg) |
| `EncryptionFileInfo.cs` | Peek DTO (suite version, alg, sizes, wrap count, thumbprints) |
| `EncryptionValidationResult.cs` | Validate DTO |
| `EncryptionRsaKey.cs` | Generate / SPKI / PKCS8 / Wrap / Unwrap / thumbprint |
| `EncryptionKeyRing.cs` | Pairs vs contacts, Issue, Enable/Disable/Expire/Retire/Compromise, JSON `VESTIGIUM-KEYRING` 1.1 |
| `EncryptionKeyOverride.cs` | Override request + `EncryptionTokenException` |
| `EncryptionLog.cs` | ALCOA+ HelperLog adapter; redacts secrets even at Debug |
| `Envelope.cs` | `VESTIGIUM HDR` read/write; `SuiteMinorFor(alg, hasRsaWrap)` |
| `Trailer.cs` | `VESTIGIUM TRL` write / parse / mac / peek / wrap list |
| `FrameCipher.cs` | One AEAD frame, or one CBC+HMAC frame |
| `OriginalNames.cs` | Bare file name rules; hidden `nathan.txt` |
| `Argon2idKdf.cs` | Passphrase → 32-byte key |

Do not add a Hashing implementation here. Do not add a separate KeyRing project.

## Logging

Category `Helpers`, subcategory `Encryption` or `Token`, APPID = host.

This is ALCOA+ without key material:

- **Complete:** Seal/Open/Shred write Pending then Success or Failed. Token Disable/Expire/Refuse/Override write Warning. TokenException is rethrown without Failed.
- **Attributable:** 8-hex thumb prefix + `by=` + `reason=` on Override. Never issuedTo / company / subject.
- **Accurate:** `EncryptionLog.Failed` never takes an `Exception`. Vestigium.Logging would otherwise persist `exception.ToString()`.

Safe to log: algorithm name, KDF id, wrap count, plaintext **length**, frame count, destination **file name**, 8-hex key prefix, sanitized actor and reason.

Never log, including Debug `enter`: passphrase, key, PKCS8, SPKI, salt-as-reusable-secret, plaintext, Base64 blob, hidden original name, company / subject / issuedTo, full thumbprint. Visible path (`nathan.aes`) is fine.

`EncryptionAudit.LooksLikeSecret` rejects PEM, `PRIVATE KEY`, PKCS8, 32+ hex, 44+ Base64. Override construction uses it. `EncryptionLog.Safe` is a last fence.

Library never calls `VestigiumLogger.Initialize`. If the host has not started logging, Seal/Open still work; log calls are no-ops.

## Demo

```
dotnet run --project src/Vestigium.Helpers.Encryption.Demo
```

JSONL: `%ProgramData%\Vestigium\Logs\Encryption\`

The demo is a WPF gallery: AES-256-GCM, ChaCha20-Poly1305, AES-256-CBC+HMAC, Argon2id, and RSA / key ring tabs. Cipher tabs round-trip a string and a file. File Seal can keep the original or shred it (3- or 7-pass random + zero). Argon2id uses a visible throwaway passphrase and writes `.argon`. CBC is not the default. RSA tab Issues CompanyX AppX / AppY, Seals to a contact, optionally also wraps to Ops, and Opens as Ops / AppX / AppY so isolation is visible. The same tab Enable / Disable / Expire tokens and can request an override. Gallery RSA keys are 2048-bit; the library default remains 3072.

## Roadmap (design)

| Version | Work | Large-file workaround |
|---|---|---|
| v1.0 | GCM + ChaCha + Argon2id + VESTIGIUM HDR/TRL | 64 KiB frames; hidden original name; `.aes` / `.argon`; Peek/Validate; optional 3/7-pass shred |
| v1.1 | AES-256-CBC + HMAC-SHA256, framed, not default — **shipped** | Per-frame IV + per-frame HMAC; PKCS#7 per frame; hidden name stays GCM |
| v1.2 | RSA-OAEP wraps content key; wrap list; key ring; token enable/disable/expire — **shipped** | Hybrid: RSA cost is N wraps (N ≤ 8); file still streams |
| v1.3 | Public-key trailer sig / frameSize override | Flag bit 2; keep EOF magic so Peek still works |

CBC without HMAC does not ship. RSA on the file body does not ship.

v1.1 extended `EncryptionAlgorithm` (alg 3). v1.2 does **not** add alg 4 — it grows the trailer. They do not invent a second magic. They do not get a `SealHugeFile` sibling — `SealFile` stays the file API.

## Sibling

`Vestigium.Helpers.Hashing` — string and file digests, HMAC-SHA256, Argon2id PHC. Separate APPID. Separate gallery. Encryption tests may call Hashing (or BCL `SHA256`) to compare file bytes after a round-trip. Encryption must not grow `HashString` / `HashFile`. There is no `Vestigium.Helpers.Hmac` project; HMAC lives in Hashing. Trailer slots stay zeros until an Encryption minor revision.
