# Vestigium.Helpers.Encryption — Requirements Specification

**Document ID:** VEST-HLP-ENC-SRS-000  
**Version:** 1.0  
**Status:** Accepted. Implemented v1.0.  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Encryption`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

This replaces the 7 September 2026 skeleton note. If implementation and this file disagree, this file wins.

---

## 0. How to read this document

It records:

- two AEAD ciphers for **strings and files**: **AES-256-GCM** (default) and **ChaCha20-Poly1305**
- one password KDF: **Argon2id**
- one on-disk / on-wire envelope so a UTF-8 string and a multi-gigabyte file share a format
- framed streaming so large files never sit in RAM
- a versioned **`VESTIGIUM TRL` trailer** at EOF: decrypt preamble, lengths, reserved hash/HMAC slots, structural MAC
- validation APIs so a host can say “this is a Vestigium envelope” and print trailer facts without decrypting frames
- HelperLog only; the library never calls `VestigiumLogger.Initialize`
- original file name (`nathan.txt`) encrypted in the trailer; visible disk name is `{stem}.aes` or `{stem}.argon`
- hashing and keyed MAC **out of this library** — siblings `Vestigium.Helpers.Hashing` and (later) `Vestigium.Helpers.Hmac` fill reserved trailer slots; they are not implemented here
- **AES-256-CBC** and **RSA** on the roadmap only; when they land they must still stream large files (see §4.3 and §11)

---

## 1. Purpose

Give every Vestigium host one way to lock a UTF-8 string or a file so an operator can park a secret, a capture, or an export without inventing a cipher.

```csharp
var sealedText = EncryptionHelper.SealString("token", secret);
var plain = EncryptionHelper.OpenString(sealedText, secret);

EncryptionHelper.SealFile("nathan.txt", exportDir, secret); // → nathan.aes or nathan.argon
EncryptionHelper.OpenFile(sealed, exportDir, secret);       // → nathan.txt
```

`secret` is either a 32-byte raw key or a passphrase. Passphrases go through Argon2id. Raw keys skip the KDF.

This is **not** TLS, BitLocker, DPAPI, a key vault, or full-disk encryption. It is authenticated encryption of caller bytes.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Ciphers (v1) | **AES-256-GCM** default. **ChaCha20-Poly1305** opt-in. Both AEAD. |
| 2 | Password KDF | **Argon2id**. Raw 32-byte keys skip it. |
| 3 | String and file | Same envelope. String is one frame (or zero). File is N frames. |
| 4 | Large files | Streamed framed AEAD. Never `File.ReadAllBytes` the plaintext or ciphertext. |
| 5 | Engine | BCL (`AesGcm`, `ChaCha20Poly1305`, `RandomNumberGenerator`). Argon2id: BCL if present on net10; otherwise one approved NuGet (`Konscious.Security.Cryptography.Argon2`). No home-grown S-boxes. |
| 6 | Hashing / HMAC libs | **Out of this project.** `Vestigium.Helpers.Hashing` (skeleton) and future `Vestigium.Helpers.Hmac` own those APIs. Trailer reserves 32+32 zero bytes for them. |
| 7 | Logging | `HelperLog` only. APPID = host (demo: `Encryption`). Never log plaintext, keys, passphrases, salts-as-secrets, Base64 ciphertext, or the hidden original name. Log alg, bytes in/out, **visible** path, frame count. |
| 8 | Initialize | Library never calls `VestigiumLogger.Initialize`. |
| 9 | Export folder | Encrypted **files** the demo writes go to `%DESKTOP%\Vestigium\Exports\{APPID}\`. Tests pass a temp path. |
| 10 | CBC / RSA | Roadmap only. When they ship they must still stream large files (CBC framed + HMAC; RSA wraps the content key, never the payload). |
| 11 | File names | Visible suffix is `.aes` (AES-256-GCM / raw key) or `.argon` (Argon2id passphrase). `nathan.txt` → `nathan.aes` or `nathan.argon`. Original name is encrypted in the trailer. Open restores `nathan.txt`. |
| 12 | Trailer | Every sealed blob ends in a `VESTIGIUM TRL` footer. Open may start from EOF. Header `frameCount = 0` means “trailer is authoritative.” |
| 13 | Suite identity | ASCII magics `VESTIGIUM HDR` and `VESTIGIUM TRL`. Suite and trailer versions are **numeric fields**, not text inside the magic. |
| 14 | Validation | `IsVestigiumFile` / `PeekFile` / `ValidateFile` inspect header+trailer without decrypting frames. Structural MAC check needs a secret; Peek does not. |
| 15 | Reserved integrity slots | Trailer always contains 32-byte `sha256` and 32-byte `hmacSha256` fields. v1.0 writes zeros. Later Hashing / Hmac libraries fill them. Do not compute them in Encryption v1.0. |

---

## 3. Goals

**G1.** One façade (`EncryptionHelper`) owns identity, paths, Seal, and Open.  
**G2.** Seal and Open a UTF-8 string as Base64 of a Vestigium envelope (header + frames + trailer).  
**G3.** Seal and Open a file of unbounded size by streaming 64 KiB frames.  
**G4.** Default algorithm is AES-256-GCM. Caller may pick ChaCha20-Poly1305 on Seal. Open reads the algorithm from the envelope.  
**G5.** Passphrases become a 32-byte content key through Argon2id. Raw keys skip the KDF.  
**G6.** Log Pending / Success / Failed through `HelperLog` only.  
**G7.** Keep `EncryptionHelper.Identity` and `EncryptionHelper.Probe()` so existing smoke tests stay green.  
**G8.** `Probe` is in-memory only. It must not write the Desktop and must not log the fixture text.  
**G9.** Wrong secret, bit flip, or truncated envelope fails closed with `CryptographicException`. Do not distinguish “wrong password” from “corrupt file” in the message.  
**G10.** Every file (and every Base64 string blob) ends with the `VESTIGIUM TRL` trailer so Open can recover algorithm, KDF, salt, nonce, frame count, and plaintext length from EOF.  
**G11.** A host can ask “is this a Vestigium file?” and print suite version, trailer version, algorithm, and sizes without a secret.  
**G12.** Seal of `nathan.txt` writes `nathan.aes` (raw key / AES-256-GCM) or `nathan.argon` (passphrase / Argon2id). Original name is hidden in the trailer. Open restores `nathan.txt`.

---

## 4. Algorithms (v1)

### 4.1 AES-256-GCM — default

- 256-bit key.
- 96-bit nonce per frame.
- 128-bit tag per frame.
- Associated data = stable header prefix (magic, version, alg, kdf, salt, file nonce, frame size — **not** `frameCount`) plus the 32-bit frame index.
- .NET type: `System.Security.Cryptography.AesGcm`.

Hardware AES-NI on typical PingIQ boxes. Default for `SealString` / `SealFile` when the caller does not pick an algorithm.

“AES-256” in this library **means AES-256-GCM** in v1. Unauthenticated AES-256-CBC is not an alias and is not shipped here.

### 4.2 ChaCha20-Poly1305 — opt-in

- 256-bit key.
- 96-bit nonce per frame.
- 128-bit tag per frame.
- Same AAD rule as GCM.
- .NET type: `System.Security.Cryptography.ChaCha20Poly1305`.

Same security contract, different math. Use when the host asks, or when a later profile prefers it on boxes without AES-NI.

“ChaCha20” in this library **means ChaCha20-Poly1305**. Raw ChaCha20 without Poly1305 is forbidden.

### 4.3 Argon2id — passphrase to key

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

### 4.4 Explicitly not v1 ciphers

AES-256-CBC, AES-128, 3DES, RC4, Blowfish, RSA payload encryption, “XOR with password”, unauthenticated CTR/CBC, homemade stream ciphers.

---

## 5. Large files (the workaround)

AES-GCM and ChaCha20-Poly1305 are both **record** AEADs. One nonce must not cover an unbounded stream. GCM in particular should stay well under \(2^{32}\) blocks (~64 GiB) **per nonce**.

Loading a 4 GB capture into a `byte[]`, encrypting it as one record, and writing it back is therefore **not** an implementation of this library. That is the thing the framing exists to avoid.

### 5.1 v1 framing (required for both algorithms)

1. Draw one **file nonce** (12 bytes) at Seal time.
2. Split plaintext into frames of **65 536 bytes** (last frame may be shorter).
3. For frame \(i\) (0-based):
   - nonce = fileNonce with the last 4 bytes overwritten by \(i\) as big-endian (fileNonce must be generated so those four bytes start at 0; implementation draws 8 random bytes + 4 zero bytes).
   - encrypt that frame with AEAD; AAD is the stable header prefix (magic through `frameSize`, **not** `frameCount`) plus the 32-bit frame index.
   - write `ciphertext || tag` (tag is 16 bytes).
4. Never buffer more than one frame of plaintext and one frame of ciphertext plus the header.
5. A file whose plaintext is 0 bytes is legal: header only, frame count 0.
6. Decrypt streams the same way. Tag mismatch on any frame throws `CryptographicException` and **stops**. Partial output files the helper created must be deleted on failure.

String APIs use the same code path with one frame (or zero if the string is empty).

Do not switch algorithm just because the file is large. Framing is the large-file story for AES-GCM, ChaCha20-Poly1305, and — when they ship — AES-256-CBC and RSA-wrapped payloads.

### 5.2 Limits that follow from framing

| Limit | v1 rule |
|---|---|
| Max frames | \(2^{32}\) (nonce counter is 32-bit). At 64 KiB that is 256 TiB of plaintext — far above host captures. Do not raise frame size to dodge this. |
| Source | Seekable **or** unknown-length. Known length writes `frameCount` in the header and the trailer. Unknown length writes header `frameCount = 0` and the real count in the trailer. |
| Unknown-length streams | **v1**, via the trailer. Destination must still be seekable so the trailer can be appended. |
| In-place encrypt | Forbidden. Write a destination. The host may replace the source afterwards. |
| RAM | O(frame size), not O(file size). Trailer is 482 bytes in v1.0 (includes hidden original name slot). |

### 5.3 Roadmap algorithms must keep the same large-file rule

This is the “working around” that CBC and RSA will need. It is specified now so a later implementer cannot “just call `RSA.Encrypt` on the file.”

**AES-256-CBC (v1.1)**

- Still 64 KiB frames.
- Fresh 16-byte IV **per frame**. Never one IV for a multi-GB file.
- Encrypt-then-MAC: HMAC-SHA256 over header + frame index + IV + ciphertext. Reject CBC without HMAC.
- Stream exactly as GCM: one frame in memory.
- PKCS#7 padding applies **per frame**, not to the whole file.

**RSA (v1.2)**

- RSA never sees the payload.
- Draw / derive the 32-byte content key as today.
- RSA-OAEP (SHA-256) wraps **only that content key**.
- Payload frames stay AES-256-GCM or ChaCha20-Poly1305.
- Minimum modulus 2048-bit; prefer 3072.
- Encrypting 4 GB with RSA directly is forbidden in this library, including later versions.

---

## 6. Envelope (Vestigium 1.0)

Binary, little-endian integers except the frame-index-in-nonce (big-endian).

A sealed blob is:

```
[VESTIGIUM HDR]
[64 KiB AEAD frames]
[trailer body]
[u32 body length]
[VESTIGIUM TRL]
```

### 6.0 Magics and versions (brainstorm, then lock)

Putting `1.0` inside the ASCII magic (`VESTIGIUM 1.0 HDR`) looks nice in a hex dump and then breaks the first time the suite is 1.1: every scanner that greps the string fails, and we would mint a new magic instead of a new version field. ZIP, PNG, and age all keep a **stable family string** and a **numeric version**.

Locked ASCII (UTF-8 / US-ASCII, no NUL padding):

| Where | Bytes | ASCII |
|---|---|---|
| Start of file | 13 | `VESTIGIUM HDR` |
| End of file | 13 | `VESTIGIUM TRL` |

Locked numbers immediately after the header magic:

| Field | Size | v1.0 value | Meaning |
|---|---|---|---|
| `suiteMajor` | u8 | 1 | Vestigium envelope family |
| `suiteMinor` | u8 | 0 | Additive suite bump |
| `headerMajor` | u8 | 1 | This header layout |
| `headerMinor` | u8 | 0 | Additive header fields |

Trailer body starts with the same four version bytes (`suiteMajor/Minor`, `trailerMajor/Minor`). v1.0 writes `1,0,1,0` in both places. They must match.

Compat rules (so we do not paint ourselves into a corner):

- **Same major, higher minor:** a v1.0 reader may parse the prefix it knows and ignore trailing body bytes. `bodyLength` in the footer is the source of truth for how far to seek.
- **Higher major:** `NotSupportedException` naming `suiteMajor` or `trailerMajor`. Do not guess.
- **Lower major:** only if we ever ship a v2 reader; v1.0 does not read a future v2.

`IsVestigiumFile` is true when the last 13 bytes are `VESTIGIUM TRL` **or** the first 13 bytes are `VESTIGIUM HDR`. Either end is enough to say “this is a suite file.”

### 6.0.1 Header bytes

```
magic          13     "VESTIGIUM HDR"
suiteMajor     u8     = 1
suiteMinor     u8     = 0
headerMajor    u8     = 1
headerMinor    u8     = 0
alg            u8     = 1 AES-256-GCM, 2 ChaCha20-Poly1305
kdf            u8     = 0 raw key, 1 Argon2id
kdfMemMiB      u8     = 64 when kdf=1, else 0
kdfIter        u8     = 3  when kdf=1, else 0
kdfPar         u8     = 1  when kdf=1, else 0
salt           16 bytes when kdf=1, else omitted
fileNonce      12 bytes
frameSize      u32    = 65536
frameCount     u64    hint; 0 means “read the trailer”
frames         frameCount × (ciphertext || 16-byte tag)   // or unknown count, then trailer
```

`frameCount` in the header is the known count when the source length is known. For an unknown-length source, write `0` and set trailer flag bit 0. Empty known files still write a trailer with `frameCount = 0` and `plaintextLen = 0`.

String output is this whole blob **including the trailer**, Base64 (`Convert.ToBase64String`, standard alphabet, no line breaks). Strings have **no file extension**.

Unknown `suiteMajor` / `headerMajor` / `alg` / `kdf` on Open throws `NotSupportedException` naming the field.

---

## 6.1 File names (short suffixes + hidden original)

The trailer already names the cipher, the KDF, and the versions. The **visible** file name is only a short hint plus the original stem. The **real** name, including `.txt`, lives in the trailer and is encrypted.

Worked example:

```
nathan.txt     Seal (AES-256-GCM, raw key)     →  nathan.aes
nathan.txt     Seal (Argon2id passphrase)      →  nathan.argon
nathan.aes     Open (with secret)              →  nathan.txt
```

`Path.GetFileName("C:\\inbox\\nathan.txt")` is what is stored (`nathan.txt`). Directories are never stored.

### Visible suffix

Exactly two write suffixes. Nothing longer.

| Condition | Suffix | Disk name for `nathan.txt` |
|---|---|---|
| Raw 32-byte key (`FromKey`) | `.aes` | `nathan.aes` |
| Passphrase (`FromPassphrase` → Argon2id) | `.argon` | `nathan.argon` |

AES-256-GCM is still the default **cipher** in both rows. `.aes` vs `.argon` tells the operator how the key was made, not which AEAD ran. ChaCha20-Poly1305 uses the same two suffixes (raw key → `.aes`, passphrase → `.argon`). Peek/Validate name the actual `alg` byte.

Open aliases (accepted, never written by the helper): `.vestigium`, `.vest`, `.vest1`, `.aes.gcm`, `.cha.poly`, `.aes.cbc`, no suffix, `.bin`.

`FileExtension(secret)` returns `".argon"` if the secret is a passphrase, otherwise `".aes"`.

`SealedFileName("nathan.txt", secret)` returns `"nathan.aes"` or `"nathan.argon"`.

`NewExportPath` for a named source:

```
%DESKTOP%\Vestigium\Exports\{APPID}\nathan.aes
```

When there is no original name (string Seal, or a stream with `originalFileName: null`), `NewExportPath(appId)` still uses the stamp form:

```
%DESKTOP%\Vestigium\Exports\{APPID}\vestigium-{APPID}-{yyyyMMdd-HHmmss}.aes
```

### How Seal chooses the disk path

1. `originalName = Path.GetFileName(sourcePath)` — `nathan.txt`. Reject if empty, longer than 255 UTF-8 bytes, or if it contains `/`, `\`, NUL, or `..`.
2. Encrypt that string into the trailer (§6.2 hidden name). Flag bit 1 set.
3. `stem = Path.GetFileNameWithoutExtension(originalName)` — `nathan`.
4. Visible file = `stem + FileExtension(secret)` — `nathan.aes` or `nathan.argon`.
5. If `destinationPath` is a **directory** (exists as a directory, or ends in a directory separator), write `{destinationPath}/{visible}`.
6. If `destinationPath` is a **file**, write that path as-is. The trailer still hides `nathan.txt`. The helper does not rewrite a caller-chosen file name.

### How Open restores the original name

1. Derive the content key, verify the structural `mac`, decrypt the hidden name.
2. If `destinationPath` is a directory, write `{destinationPath}/{originalName}` → `nathan.txt`.
3. If `destinationPath` is a file, write that path as-is. The hidden name is still available via `RevealOriginalFileName`.
4. Do not trust the visible suffix. A file renamed to `other.bin` still restores `nathan.txt` when the destination is a directory.

Peek **without** a secret never returns the original name. It may set `HasHiddenOriginalName = true`.

### Why the original name is encrypted

`passwords.txt` as a Desktop name is a leak. `passwords.aes` is less of one. A hex dump of the trailer must not show `passwords.txt` either, so the name is an AEAD field under the content key, not plaintext in the tail.

### What we do not store

- Full paths (`C:\Users\...\nathan.txt`)
- Alternate streams, ADS, Unix mode bits
- A second copy of the original name in the header

---

## 6.2 Trailer (`VESTIGIUM TRL`)

```
[VESTIGIUM HDR]
[64 KiB AEAD frames]
[trailer body]
[u32 body length]
[VESTIGIUM TRL]          // 13 bytes, last bytes of the file
```

Find it: seek to `Length - 17`, read `bodyLength` (u32 LE) then 13-byte magic. Magic must be ASCII `VESTIGIUM TRL`. Seek to `Length - 17 - bodyLength` and parse the body.

v1.0 `bodyLength` is **465**. A later minor version may grow the body; readers use `bodyLength`, not a constant, when seeking.

### Trailer body (465 bytes in v1.0, little-endian)

```
suiteMajor      u8     = 1
suiteMinor      u8     = 0
trailerMajor    u8     = 1
trailerMinor    u8     = 0
alg             u8     = 1 AES-256-GCM, 2 ChaCha20-Poly1305
kdf             u8     = 0 raw key, 1 Argon2id
kdfMemMiB       u8
kdfIter         u8
kdfPar          u8
flags           u16
salt            16     Argon2id salt, or 16 zeros when kdf=0
fileNonce       12
frameSize       u32    = 65536
frameCount      u64    authoritative
plaintextLen    u64    authoritative
createdUtc      i64    Unix seconds UTC, or 0
sha256          32     RESERVED — zeros in v1.0; later Hashing
hmacSha256      32     RESERVED — zeros in v1.0; later Hmac
reserved        16     zeros in v1.0
nameNonce       12     AEAD nonce for the hidden original name
nameLen         u16    UTF-8 byte count of the original name (0..255); 0 = none
nameCt          272    256-byte padded name + 16-byte AEAD tag (zeros if nameLen=0)
mac             32     structural HMAC-SHA256 (Encryption-owned, v1.0)
```

465 + 4 + 13 = **482** bytes at EOF in v1.0. The name slot is **fixed** so seeking stays a single `bodyLength` read even when there is no original name (string Seal).

`flags` bits:

| Bit | Meaning when set | When |
|---|---|---|
| 0 | Header `frameCount` is not authoritative | v1.0 |
| 1 | Hidden original name is present (`nameLen` > 0) | v1.0 |
| 2 | Detached public-key signature follows `mac` | v1.2+ |
| 3 | `sha256` slot is filled (Hashing library) | later |
| 4 | `hmacSha256` slot is filled (Hmac library) | later |

v1.0 writers set bit 0 when the header count is unknown, and bit 1 when an original file name was Sealed. Bits 3–4 stay 0 and the two 32-byte hash/HMAC slots stay zeros. Encryption v1.0 must not compute a file hash or a Helpers.Hmac value “to be helpful.”

### Hidden original name (v1.0)

This is how `nathan.txt` survives a round-trip while the disk file is `nathan.aes`.

1. Take `Path.GetFileName(sourcePath)` only. UTF-8, 1..255 bytes. No `/`, `\`, NUL, or `..`.
2. Write `nameLen`, then pad the UTF-8 bytes with zeros to 256 bytes.
3. Draw `nameNonce` (12 random bytes). Do **not** reuse `fileNonce` or a frame index.
4. AEAD-encrypt the 256-byte padded buffer with the **content key**, same `alg` as the payload.
   - AAD = UTF-8 `VESTIGIUM-ORIG-NAME` || `suiteMajor` || `suiteMinor`
   - Output = 256 ciphertext + 16-byte tag → `nameCt` (272 bytes)
5. Set flag bit 1.

String Seal and unnamed streams: `nameLen = 0`, `nameNonce` and `nameCt` all zeros, bit 1 clear. The 272-byte slot is still written so `bodyLength` stays 465.

Decrypt (Open / `RevealOriginalFileName` only, after the structural `mac` checks):

1. If bit 1 is clear or `nameLen` is 0, there is no original name.
2. AEAD-open `nameCt` with `nameNonce` and the same AAD. Tag mismatch → `CryptographicException` (do not distinguish “wrong password”).
3. Take the first `nameLen` bytes as UTF-8. Re-validate no `/`, `\`, NUL, `..`.

Peek without a secret must not copy `nameCt` into logs or `EncryptionFileInfo.OriginalFileName`. It may set `HasHiddenOriginalName`.

### Reserved slots vs the structural `mac`

Three different integrity ideas share the tail. Keep them apart:

| Slot | Owner | v1.0 | Later |
|---|---|---|---|
| `mac` (last 32 of body) | Encryption | Written. HKDF+HMAC over the trailer prefix. Proves preamble + counts. | Stays. |
| `sha256` | `Vestigium.Helpers.Hashing` | 32 zero bytes. Flag bit 3 clear. | SHA-256 of **plaintext** (streamed while Sealing). Flag bit 3 set. |
| `hmacSha256` | `Vestigium.Helpers.Hmac` | 32 zero bytes. Flag bit 4 clear. | HMAC-SHA256 over ciphertext frames or over the header+frames, using a caller MAC key distinct from the content key. Flag bit 4 set. |

Do not reuse `mac` as the future Helpers.Hmac field. `mac` is how Encryption knows its own trailer was not clipped. Helpers.Hmac is a different key and a different library.

Structural `mac` in v1.0:

```
macKey = HKDF-SHA256(
    ikm    = 32-byte content key,
    salt   = fileNonce,
    info   = UTF-8 "VESTIGIUM-TRL-HMAC",
    len    = 32)

mac = HMAC-SHA256(macKey, trailer body without the last 32 bytes)
```

Zeros in the reserved slots are part of that prefix today, so filling them later **must** bump `trailerMinor` and change the MAC input rule in that revision (MAC covers the filled slots). v1.0 readers that see a higher `trailerMinor` parse the prefix they know and do not require `sha256`/`hmacSha256` to be zero.

### What the trailer is not

- Not a second cipher and not a place to store the content key.
- Not a substitute for per-frame GCM / Poly1305 tags.
- Not a plaintext filename sidecar. The original name is AEAD-encrypted.
- Not a full path store. Name only (`nathan.txt`).
- Not `Vestigium.Helpers.Hmac` and not `Vestigium.Helpers.Hashing`. Those libraries fill reserved slots when they exist.

### 6.3 Validation (no decrypt of frames)

These methods exist so a gallery or host can say “Vestigium encrypted this” and print the trailer.

| Method | Secret? | Throws? | Does |
|---|---|---|---|
| `IsVestigiumFile(path)` / `IsVestigium(stream)` | No | No | True if head magic is `VESTIGIUM HDR` or tail magic is `VESTIGIUM TRL`. |
| `TryPeekFile(path, out info)` | No | No | False if not a suite file. Fills `EncryptionFileInfo` from the trailer when possible. |
| `PeekFile(path)` / `Peek(stream)` | No | Yes if not suite | Trailer facts. Does not check `mac`. Does not return the original name. |
| `RevealOriginalFileName(path, secret)` | Yes | Yes if not suite / bad mac | Hidden `nathan.txt`. |
| `ValidateFile(path)` | No | No (returns a result) | Magics, versions, `bodyLength`, agreement, `HasHiddenOriginalName`. |
| `ValidateFile(path, secret)` | Yes | No (returns a result) | Plus structural `mac` and `OriginalFileName`. Still does not decrypt payload frames. |

`EncryptionValidationResult`:

| Property | Meaning |
|---|---|
| `IsVestigium` | Family magic present at head or tail |
| `SuiteVersion` | `"1.0"` from the numeric fields |
| `HeaderVersion` / `TrailerVersion` | `"1.0"` |
| `HeaderPresent` / `TrailerPresent` | Each magic found |
| `HeaderTrailerAgree` | alg, kdf, salt, nonce, frameSize, counts match the rules in §6.2 |
| `ReservedHashEmpty` | `sha256` slot is 32 zeros (expected in v1.0) |
| `ReservedHmacEmpty` | `hmacSha256` slot is 32 zeros (expected in v1.0) |
| `StructuralMacValid` | `null` without a secret; `true`/`false` with one |
| `HasHiddenOriginalName` | Flag bit 1. Not the name itself. |
| `OriginalFileName` | `nathan.txt` only when a secret was supplied and mac verified |
| `Info` | The `EncryptionFileInfo` when peek succeeded |
| `Problems` | Short English reasons if a check failed (no secrets) |

Validate does **not** Open payload frames. A host can light a green “Vestigium 1.0 / AES-256-GCM / 12 frames / 720 KB / has hidden name” row from Validate + Peek alone. The string `nathan.txt` appears only after a secret is supplied.

Reserved algorithm ids for the roadmap (do not emit in v1):

| alg | Meaning | Version that may emit it |
|---|---|---|
| 3 | AES-256-CBC + HMAC-SHA256 | v1.1 |
| 4 | RSA-OAEP wrapped content key + AEAD frames | v1.2 |

---

## 7. Public surface (v1)

Names may move a token. The shapes may not.

```csharp
public static class EncryptionHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Encryption"
    public static string Probe();            // Pending + Success; no Desktop file; no secret in the log

    public static string DefaultExportDirectory(string appId);
    public static string FileExtension(EncryptionSecret secret); // ".aes" or ".argon"
    public static string SealedFileName(string originalFileName, EncryptionSecret secret);
    public static string NewExportPath(string appId, string? originalFileName = null, EncryptionSecret? secret = null);
    public static string? RevealOriginalFileName(string path, EncryptionSecret secret);

    public static bool IsVestigiumFile(string path);
    public static bool IsVestigium(Stream source);
    public static bool TryPeekFile(string path, out EncryptionFileInfo info);
    public static EncryptionFileInfo PeekFile(string path);
    public static EncryptionFileInfo Peek(Stream source); // Seek from end
    public static EncryptionValidationResult ValidateFile(string path, EncryptionSecret? secret = null);
    public static EncryptionValidationResult Validate(Stream source, EncryptionSecret? secret = null);

    public static string SealString(string plaintext, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm);
    public static string OpenString(string sealedBase64, EncryptionSecret secret);

    public static string SealFile(string sourcePath, string destinationPath, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm);
    public static string OpenFile(string sourcePath, string destinationPath, EncryptionSecret secret);

    public static void SealFile(Stream source, Stream destination, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, long? plaintextLength = null, string? originalFileName = null);
    public static void OpenFile(Stream source, Stream destination, EncryptionSecret secret);
}

public sealed class EncryptionFileInfo
{
    public string SuiteVersion { get; init; }      // "1.0"
    public string HeaderVersion { get; init; }     // "1.0"
    public string TrailerVersion { get; init; }    // "1.0"
    public EncryptionAlgorithm Algorithm { get; init; }
    public bool UsedArgon2id { get; init; }
    public int FrameSize { get; init; }
    public ulong FrameCount { get; init; }
    public ulong PlaintextLength { get; init; }
    public DateTimeOffset? CreatedUtc { get; init; }
    public bool Sha256ReservedFilled { get; init; }    // false in v1.0
    public bool HmacSha256ReservedFilled { get; init; } // false in v1.0
    public bool HasHiddenOriginalName { get; init; }   // flag bit 1; name itself is not here
    public string? OriginalFileName { get; init; }     // only when a secret was supplied
}

public sealed class EncryptionValidationResult
{
    public bool IsVestigium { get; init; }
    public bool HeaderPresent { get; init; }
    public bool TrailerPresent { get; init; }
    public bool HeaderTrailerAgree { get; init; }
    public bool? StructuralMacValid { get; init; }
    public string? OriginalFileName { get; init; }     // set only when a secret was supplied
    public EncryptionFileInfo? Info { get; init; }
    public IReadOnlyList<string> Problems { get; init; }
}

public enum EncryptionAlgorithm
{
    Aes256Gcm = 1,
    ChaCha20Poly1305 = 2
}

public sealed class EncryptionSecret : IDisposable
{
    public static EncryptionSecret FromPassphrase(string passphrase);
    public static EncryptionSecret FromKey(ReadOnlySpan<byte> key32);
    public bool IsPassphrase { get; }   // true for FromPassphrase; used by FileExtension; never ToString the secret
    public void Dispose();
}
```

Rules:

- `SealString` / `OpenString` take and return strings. Plaintext is UTF-8. Ciphertext is Base64 of the Vestigium envelope (header + frames + trailer).
- File overloads create the destination directory, stream, and return the path actually written.
- `SealFile(sourcePath, destinationPath, …)`: if `destinationPath` is a directory, the helper writes `{stem}.aes` or `{stem}.argon` there and hides `Path.GetFileName(sourcePath)` in the trailer. If it is a file, that path is used as-is and the original name is still hidden.
- `OpenFile(sourcePath, destinationPath, …)`: if `destinationPath` is a directory, the helper restores the hidden original name (`nathan.txt`). If it is a file, that path is used as-is.
- Stream overloads do **not** dispose caller streams. `plaintextLength` is optional. When omitted (or the source is not seekable), header `frameCount = 0`, flag bit 0 is set, and the trailer carries the real count. Destination must allow the trailer to be appended. Pass `originalFileName` to hide a name on a stream Seal.
- `SealedFileName("nathan.txt", secret)` is `"nathan.aes"` or `"nathan.argon"`.
- `RevealOriginalFileName` needs a secret, verifies `mac`, decrypts the name slot, and returns `nathan.txt` or null.
- `IsVestigiumFile` / `TryPeekFile` never throw on a random file; they return false.
- `PeekFile` / `Peek` read the trailer. No secret. They do not verify `mac` and they do not return the original name.
- `ValidateFile` without a secret checks magics, versions, sizes, header/trailer agreement, reserved hash/HMAC slots zeros, and `HasHiddenOriginalName`.
- `ValidateFile` with a secret also checks the structural `mac` and may fill `OriginalFileName`. It still does not decrypt payload frames.
- `FromKey` throws if the span is not 32 bytes.
- `FromPassphrase` throws if blank.
- `Dispose` clears the derived or copied key material.
- `Open*` reads `alg` and `kdf` from the header and confirms them against the trailer. The caller does not pass the algorithm on decrypt.
- Wrong passphrase / wrong key / bit flip → `CryptographicException`. Do not distinguish “wrong password” from “corrupt file” in the exception message.
- `EncryptionSecret` is not serializable and must not override `ToString` with the passphrase or key.

`Probe` seals and opens a short fixture in memory. It must not write `%DESKTOP%` and must not log the fixture text.

---

## 8. Logging

| Event | Level | Status |
|---|---|---|
| Seal start (string or file, alg, kdf, byte/frame counts — not secrets) | Information | Pending |
| Seal complete (path or `string`, frames, bytes) | Information | Success |
| Open start | Information | Pending |
| Open complete | Information | Success |
| Tag mismatch / crypt failure | Error | Failed |
| Missing source file | Error | Failed |
| Unsupported envelope field | Error | Failed |
| Peek (path, alg, frames, plaintext length — not secrets) | Information | Success |

Category = `Helpers`. Subcategory = `Encryption`. APPID = host APPID.

Never log: plaintext, passphrase, key bytes, salt that could be reused as a key, Base64 ciphertext, or the **hidden original file name**. Log the visible path (`nathan.aes`) only.

---

## 9. Demo contract

`Vestigium.Helpers.Encryption.Demo` is the CLI host. After implementation:

1. `HelperWpfHost.Start` is not required for the CLI; the CLI uses `HelperDemoHost.Run` with APPID `Encryption`.
2. `EncryptionHelper.Probe()`.
3. After the engine ships: Seal a short string with a demo passphrase; Open it back; print only lengths and alg to the console.
4. After the engine ships: Seal a small temp file named like `nathan.txt` into `%DESKTOP%\Vestigium\Exports\Encryption\` as `nathan.aes` or `nathan.argon`. Open it back to `nathan.txt`. Print the visible name, not the hidden one, until Open succeeds.
5. JSONL under `%ProgramData%\Vestigium\Logs\Encryption\`.

Do not put a real production passphrase in the gallery source. A throwaway gallery secret is fine if it is obviously fake (`gallery-demo-only`).

---

## 10. Tests

xUnit, serial logger collection, temp directories only.

v1.0 after implementation:

- `Identity` is `Vestigium.Helpers.Encryption`.
- `Probe` writes Pending then Success when the host is initialized; no Desktop file.
- `SealString` / `OpenString` round-trip UTF-8 including non-ASCII (`café`, `Δ`).
- Wrong passphrase throws `CryptographicException`.
- Tampered Base64 (flip a ciphertext byte) throws `CryptographicException`.
- ChaCha20-Poly1305 string round-trip.
- Raw 32-byte key path round-trip (no salt in envelope).
- File Seal / Open of a 200 KiB buffer (more than one frame) matches SHA-256 of the original — hash via `Vestigium.Helpers.Hashing` once that library exists, or `SHA256.HashData` in the test until then.
- Empty string and empty file round-trip.
- `SealFile` of a missing source throws `FileNotFoundException`.
- Envelope magic / version / alg checks.
- Written files start with `VESTIGIUM HDR` and end with `VESTIGIUM TRL`.
- `IsVestigiumFile` is true for those files and false for a random temp file.
- `PeekFile` reports suite `"1.0"`, alg, frame count, and plaintext length without a secret.
- `ValidateFile` without a secret succeeds on a helper-written file; reserved hash/HMAC slots are zeros.
- `ValidateFile` with the correct secret sets `StructuralMacValid = true`.
- Header/trailer `frameCount` mismatch fails validation and Open.
- Truncating the last 17 bytes (length + magic) fails `IsVestigiumFile` and Open.
- Flipping one structural `mac` byte makes `StructuralMacValid = false` and Open throw.
- `FileExtension(FromKey(…))` is `.aes`. `FileExtension(FromPassphrase(…))` is `.argon`.
- `SealedFileName("nathan.txt", rawKeySecret)` is `nathan.aes`.
- `SealedFileName("nathan.txt", passphraseSecret)` is `nathan.argon`.
- Seal `nathan.txt` into a temp directory writes `nathan.aes` (raw key) or `nathan.argon` (passphrase). Peek without a secret does not contain `nathan.txt`. Reveal/Open with the secret returns `nathan.txt`.
- Open of a ChaCha file renamed to `.bin` into a directory still restores the hidden original name (header/trailer win).
- Tests never use the real Desktop.

xUnit, serial logger collection, temp directories only. See `EncryptionSessionTests`.

---

## 11. Non-goals (v1)

| Item | Why |
|---|---|
| Hashing APIs | Sibling Hashing. |
| AES-256-CBC | Roadmap v1.1. Needs HMAC or it is unsafe. |
| RSA / ECC payload encrypt | Roadmap v1.2. RSA wraps a content key only. |
| DPAPI / TPM / Windows Hello | Windows-only; this TFM is `net10.0`. |
| Key vault, rotation UI | Host concern. |
| Compress-then-encrypt switch | Out of v1. |
| Encrypting Vestigium JSONL logs | Logging owns `%ProgramData%`. |
| Replacing FileIo | This library talks envelopes, not general paths. |
| In-place encrypt of the source file | Write a destination, then the host may replace. |
| Password managers / secret stores | Host concern. |
| Full paths in the trailer | Name only (`nathan.txt`), never `C:\\Users\\...`. |

---

## 12. Roadmap

### v1.0 — this document

- AES-256-GCM default
- ChaCha20-Poly1305 opt-in
- Argon2id or raw 32-byte key
- `VESTIGIUM HDR` / `VESTIGIUM TRL` envelope, versions 1.0 / 1.0
- Reserved 32+32 hash/HMAC slots (zeros)
- `IsVestigiumFile` / `Peek` / `Validate`
- String Base64 + streamed files (unknown-length via trailer)
- Short write suffixes `.aes` / `.argon`; original name hidden in the trailer
- HelperLog, demo, tests
- Large-file streaming is mandatory, not optional

### v1.1 — AES-256-CBC (interop)

- Algorithm id 3.
- **Encrypt-then-MAC**: AES-256-CBC then HMAC-SHA256 over header + frame index + IV + ciphertext.
- Still framed (same 64 KiB). Per-frame IV, never a single IV for a multi-GB file.
- Not the default. Exists so a host can read an old vendor dump or emit one.
- Reject CBC without HMAC. Do not ship “just CBC”.
- Large-file workaround: same frame pump as GCM. PKCS#7 per frame.

### v1.2 — RSA envelope wrap

- Algorithm stays AES-256-GCM (or ChaCha) on the payload.
- RSA-OAEP (SHA-256) encrypts the **32-byte content key** only. Payload frames do not change.
- Public key on Seal, private key on Open.
- Minimum 2048-bit; prefer 3072.
- This is how large files stay streamable under RSA. Encrypting 4 GB with RSA directly is forbidden in this library, including later versions.
- Large-file workaround: hybrid encryption. RSA cost is constant (one OAEP wrap). Payload cost stays linear in file size via AEAD frames.

### After Hashing v1 ships

- Fill trailer `sha256` (plaintext, streamed). Set flag bit 3. Bump `trailerMinor`.

### After Hmac v1 ships

- Fill trailer `hmacSha256` with a caller MAC key that is **not** the content key. Set flag bit 4. Bump `trailerMinor`.

### v1.3

- Optional `frameSize` override (64 KiB / 1 MiB).
- Flag bit 2 public-key signature over the trailer body (pairs with v1.2 RSA keys or Ed25519).

### Explicitly never here

- Custom ciphers
- Hashing
- TLS
- Full-disk encryption
- Logging JSONL as ciphertext in ProgramData
- RSA on the file body

---

## 13. Siblings: Hashing and Hmac

`Vestigium.Helpers.Hashing` already exists as a skeleton (Identity + Probe). `Vestigium.Helpers.Hmac` is a planned sibling, not created in this change.

- Hashing must not encrypt. Hmac must not encrypt.
- Encryption must not grow `Hash*` or `Hmac*` public APIs.
- v1.0 trailer already has a 32-byte slot for each. Both are zeros until those libraries exist and an Encryption minor revision fills them.
- Different APPIDs (`Encryption`, `Hashing`, later `Hmac`).
- Hashing / Hmac file APIs must stream. Same “do not `ReadAllBytes` a capture” rule.

---

## 14. Glossary

| Term | Meaning |
|---|---|
| AEAD | Authenticated encryption with associated data (confidentiality + tag) |
| Frame | One AEAD record, ≤ 65 536 bytes of plaintext |
| File nonce | 12-byte nonce family for a Seal; frame index fills the last 4 bytes |
| Envelope | `VESTIGIUM HDR` + frames + `VESTIGIUM TRL` |
| Trailer | EOF record: versions, decrypt preamble, reserved hash/HMAC slots, structural `mac` |
| Content key | 32-byte key that actually runs AES-GCM / ChaCha |
| Argon2id | Password KDF that produces the content key |
| Seal / Open | Encrypt / decrypt in this library’s vocabulary |
| Peek | Read trailer metadata from EOF with no secret (no original name) |
| Validate | Magics + versions + agreement + optional structural `mac`; with secret may reveal original name |
| Hidden name | Encrypted original file name in the trailer (`nathan.txt` behind `nathan.aes`) |
| Hybrid RSA | RSA wraps the content key; AEAD frames carry the file |

---

## 15. Acceptance

This SRS is accepted when:

1. This file is on `main` under `src/Vestigium.Helpers.Encryption/_Documentation/`.
2. Implementation of §7 + §9 demo + §10 tests follows without inventing a third v1 cipher or hashing APIs.
3. Large-file Seals stream; a reviewer can see that `ReadAllBytes` is not the file path.
4. Roadmap CBC and RSA text in §5.3 / §12 is not treated as v1 work.
5. `nathan.txt` → visible `.aes` / `.argon`, hidden original name round-trips on Open into a directory.

Implementation is a separate change from this document.
