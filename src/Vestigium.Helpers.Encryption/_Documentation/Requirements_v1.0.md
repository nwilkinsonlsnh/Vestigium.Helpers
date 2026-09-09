# Vestigium.Helpers.Encryption — Requirements Specification

**Document ID:** VEST-HLP-ENC-SRS-000  
**Version:** 1.0  
**Status:** Accepted. Implemented v1.0 + v1.1 AES-256-CBC (interop) + v1.2 RSA-OAEP wrap + key ring + token enable/disable/expire with override + ALCOA+ logs.  
**Date:** 8 September 2026  
**Package:** `Vestigium.Helpers.Encryption`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

This replaces the 7 September 2026 skeleton note. If implementation and this file disagree, this file wins.

---

## 0. How to read this document

It records:

- two AEAD ciphers for **strings and files**: **AES-256-GCM** (default) and **ChaCha20-Poly1305**
- one interop cipher: **AES-256-CBC + HMAC-SHA256** (v1.1, not default; never CBC without HMAC)
- one password KDF: **Argon2id**
- one on-disk / on-wire envelope so a UTF-8 string and a multi-gigabyte file share a format
- framed streaming so large files never sit in RAM
- a versioned **`VESTIGIUM TRL` trailer** at EOF: decrypt preamble, lengths, reserved hash/HMAC slots, structural MAC
- validation APIs so a host can say “this is a Vestigium envelope” and print trailer facts without decrypting frames
- HelperLog only; the library never calls `VestigiumLogger.Initialize`
- original file name (`nathan.txt`) encrypted in the trailer; visible disk name is `{stem}.aes` or `{stem}.argon`
- hashing and keyed MAC **out of this library** — siblings `Vestigium.Helpers.Hashing` and (later) `Vestigium.Helpers.Hmac` fill reserved trailer slots; they are not implemented here
- **AES-256-CBC + HMAC-SHA256** ships in v1.1 as framed Encrypt-then-MAC, not the default
- **RSA-OAEP-SHA256 wrap** ships in v1.2. It is **not** a payload algorithm. Payload `alg` stays 1 / 2 / 3. RSA wraps the 32-byte content key only; a wrap **list** (1..8) lives in the trailer after the hidden name and before the structural `mac`. Suite minor **2** (Peek `"1.2"`) when the wrap list is present. GCM/ChaCha without wraps stay `"1.0"`; CBC without wraps stays `"1.1"`. Types live in this project (`EncryptionRsaKey`, `EncryptionKeyRing`). There is no separate KeyRing library. See §4.6, §4.7, §5.3, §6.2, and §12

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

RSA wrap (v1.2) is an extra dimension on the same envelope:

```csharp
using var ops = EncryptionRsaKey.Generate();           // operator pair
using var ring = EncryptionKeyRing.Create("Ops ring");
var (contact, slip) = ring.Issue("Company X AppX", "AppX wrap", "CompanyX", application: "ApplicationX");
var sealedToX = EncryptionHelper.SealString("token", [contact.Key], alsoWrapTo: ops);
var back = EncryptionHelper.OpenString(sealedToX, slip);   // Company X
EncryptionHelper.OpenString(sealedToX, ops);               // also wrap to me
```

This is **not** TLS, BitLocker, DPAPI, a key vault, or full-disk encryption. It is authenticated encryption of caller bytes. RSA never encrypts the file body.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Ciphers (v1) | **AES-256-GCM** default. **ChaCha20-Poly1305** opt-in. Both AEAD. **AES-256-CBC + HMAC-SHA256** is v1.1 interop, not default. Never CBC without HMAC. |
| 2 | Password KDF | **Argon2id**. Raw 32-byte keys skip it. |
| 3 | String and file | Same envelope. String is one frame (or zero). File is N frames. |
| 4 | Large files | Streamed framed AEAD. Never `File.ReadAllBytes` the plaintext or ciphertext. |
| 5 | Engine | BCL (`AesGcm`, `ChaCha20Poly1305`, `RSA` OAEP SHA-256, `RandomNumberGenerator`). Argon2id: BCL if present on net10; otherwise one approved NuGet (`Konscious.Security.Cryptography.Argon2`). No home-grown S-boxes. |
| 6 | Hashing / HMAC libs | **Out of this project.** `Vestigium.Helpers.Hashing` (skeleton) and future `Vestigium.Helpers.Hmac` own those APIs. Trailer reserves 32+32 zero bytes for them. |
| 7 | Logging | `HelperLog` only. APPID = host (demo: `Encryption`). **ALCOA+**: attributable (APPID + 8-hex key prefix + override actor), contemporaneous (logger timestamp), complete (Pending then Success/Failed; Warning on Disable/Expire/Refuse/Override), never original secret material. Never log plaintext, keys, passphrases, salts-as-secrets, Base64 ciphertext, the hidden original name, PKCS8, SPKI, company / subject / issuedTo. Log alg, bytes in/out, **visible** path, frame count, wrap **count**. Do not attach `exception` even at Debug — Vestigium.Logging `EXCEPTION` is `exception?.ToString()`. |
| 8 | Initialize | Library never calls `VestigiumLogger.Initialize`. |
| 9 | Export folder | Encrypted **files** the demo writes go to `%DESKTOP%\Vestigium\Exports\{APPID}\`. Tests pass a temp path. |
| 10 | CBC / RSA | CBC ships in v1.1 as framed Encrypt-then-MAC (alg 3, suite 1.1). **RSA wrap ships in v1.2**: wrap list, suite 1.2, payload alg unchanged. RSA never encrypts frames. |
| 11 | File names | Visible suffix is `.aes` (AES-256-GCM / raw key / RSA-only wrap) or `.argon` (Argon2id passphrase). `nathan.txt` → `nathan.aes` or `nathan.argon`. Original name is encrypted in the trailer. Open restores `nathan.txt`. RSA wrap does not mint a third suffix. |
| 12 | Trailer | Every sealed blob ends in a `VESTIGIUM TRL` footer. Open may start from EOF. Header `frameCount = 0` means “trailer is authoritative.” Wrap list (when present) grows the body; `bodyLength` in the footer is the source of truth. |
| 13 | Suite identity | ASCII magics `VESTIGIUM HDR` and `VESTIGIUM TRL`. Suite and trailer versions are **numeric fields**, not text inside the magic. |
| 14 | Validation | `IsVestigiumFile` / `PeekFile` / `ValidateFile` inspect header+trailer without decrypting frames. Structural MAC check needs a secret **or** a matching RSA private / ring. Peek does not. Peek may list wrap **thumbprints**. Peek never returns company name or subject. |
| 15 | Reserved integrity slots | Trailer always contains 32-byte `sha256` and 32-byte `hmacSha256` fields. v1.0 writes zeros. Later Hashing / Hmac libraries fill them. Do not compute them in Encryption. |
| 16 | Secure delete | After a successful `SealFile`, the caller may shred the unencrypted source. **3-pass** = 3 random overwrites + 1 zero pass, then delete. **7-pass** = 7 random + 1 zero, then delete. Default is **Keep** (do not touch the plaintext file). Stream `SealFile` does not shred — there is no path. Flash / SSD wear-leveling is best-effort; the passes still run. |
| 17 | Isolation | Isolation is **per public key** (thumbprint of SubjectPublicKeyInfo), not a friendly name. Company X Application X and Company X Application Y are two moduli. A file sealed to AppX cannot be opened by AppY. Trailer stores **thumbprints only**. |
| 18 | Key ring | Two lists in one JSON format `VESTIGIUM-KEYRING` 1.1: **pairs** (private+public, receive / Open) and **contacts** (public only, send / Seal). Many keys per `issuedTo`. Default Issue is issue-and-forget (public contact kept; private returned as a slip and not stored). `escrow: true` is explicit. Types live beside Encryption. Format minor 1 adds status, expiry, and status-changed. |
| 19 | Token edition | Ring records have `Active`, `Disabled`, `Expired`, `Retired`, `Compromised`. Disable / Expire of an Active token is operator control. Seal or Open **via the ring** then requires `EncryptionKeyOverride` (requestedBy ≤ 50, reason ≤ 80, no PEM / long hex / long Base64). A **Warning** is always logged (Disable, Expire, Refuse, Override) with 8-hex thumb prefix + actor + reason. **Compromised and Retired are not overridable** and cannot be Enabled. A company slip (`EncryptionRsaKey`) is independent of ring status. Clock expiry (`ExpiresUtc` in the past) is EffectiveStatus `Expired`. |

---

## 3. Goals

**G1.** One façade (`EncryptionHelper`) owns identity, paths, Seal, and Open.  
**G2.** Seal and Open a UTF-8 string as Base64 of a Vestigium envelope (header + frames + trailer).  
**G3.** Seal and Open a file of unbounded size by streaming 64 KiB frames.  
**G4.** Default algorithm is AES-256-GCM. Caller may pick ChaCha20-Poly1305 or AES-256-CBC+HMAC on Seal. Open reads the algorithm from the envelope.  
**G5.** Passphrases become a 32-byte content key through Argon2id. Raw keys skip the KDF. RSA-only Seal draws a random 32-byte content key (kdf = 0) and wraps it.  
**G6.** Log Pending / Success / Failed through `HelperLog` only. Token Disable / Expire / Refuse / Override log Warning. Never attach exceptions.  
**G7.** Keep `EncryptionHelper.Identity` and `EncryptionHelper.Probe()` so existing smoke tests stay green.  
**G8.** `Probe` is in-memory only. It must not write the Desktop and must not log the fixture text.  
**G9.** Wrong secret, wrong RSA private, bit flip, or truncated envelope fails closed with `CryptographicException`. Do not distinguish “wrong password” from “corrupt file” from “wrong company key” in the message. Message is `The envelope is corrupt.`  
**G10.** Every file (and every Base64 string blob) ends with the `VESTIGIUM TRL` trailer so Open can recover algorithm, KDF, salt, nonce, frame count, and plaintext length from EOF.  
**G11.** A host can ask “is this a Vestigium file?” and print suite version, trailer version, algorithm, wrap count, and sizes without a secret.  
**G12.** Seal of `nathan.txt` writes `nathan.aes` (raw key / AES-256-GCM / RSA-only wrap) or `nathan.argon` (passphrase / Argon2id). Original name is hidden in the trailer. Open restores `nathan.txt`.  
**G13.** Optional secure delete of the unencrypted source after Seal: 3 random + zero, or 7 random + zero. Off by default.  
**G14.** Seal may wrap the content key to 1..8 RSA public keys. Open with the matching private, a key ring, **or** the original secret if one was used.  
**G15.** A host can keep a named key ring: pairs to receive, contacts to send. Isolation survives sending the envelope to the wrong company.  
**G16.** Enable, Disable, Expire, Retire, Compromise tokens on the ring. Disabled and Expired require an override; Compromised and Retired fail closed.

---

## 4. Algorithms (v1)

### 4.1 AES-256-GCM — default

- 256-bit key.
- 96-bit nonce per frame.
- 128-bit tag per frame.
- Associated data = stable header prefix (magic, version, alg, kdf, salt, file nonce, frame size — **not** `frameCount`) plus the 32-bit frame index.
- .NET type: `System.Security.Cryptography.AesGcm`.

Hardware AES-NI on typical PingIQ boxes. Default for `SealString` / `SealFile` when the caller does not pick an algorithm.

“AES-256” in this library **means AES-256-GCM** unless the caller picks `Aes256CbcHmac`. Unauthenticated AES-256-CBC is not an alias and is not shipped.

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

### 4.4 Explicitly not shipped ciphers

Unauthenticated AES-256-CBC, AES-128, 3DES, RC4, Blowfish, RSA payload encryption, “XOR with password”, unauthenticated CTR/CBC, homemade stream ciphers.

RSA-OAEP on the **content key** is shipped (v1.2). RSA on the **file body** is forbidden in this library, including later versions.

### 4.5 AES-256-CBC + HMAC-SHA256 — v1.1 interop

Algorithm id **3**. Suite minor **1** (Peek `"1.1"`) when there is **no** wrap list. Not the default. Exists so a host can read an old vendor dump or emit one.

- Still 64 KiB frames. Same frame pump as GCM.
- Fresh random 16-byte IV **per frame**. Never one IV for a multi-GB file.
- **Encrypt-then-MAC**: AES-256-CBC (PKCS#7 per frame) then HMAC-SHA256 over header prefix + u32be frame index + IV + ciphertext.
- HMAC key = HKDF-SHA256(contentKey, salt=fileNonce, info=`VESTIGIUM-CBC-HMAC`). AES key = content key. Never the same key for both.
- Frame on disk: `IV(16) || PKCS#7 ciphertext || HMAC-SHA256(32)`. A full 64 KiB plaintext frame pads +16.
- Reject CBC without HMAC. Do not ship “just CBC”. A missing or flipped per-frame HMAC fails closed: `The envelope is corrupt.`
- Hidden original name stays AES-256-GCM (`NameAlgorithm()`). The 272-byte nameCt slot cannot hold IV + padded CT + HMAC-32. Name AAD remains `VESTIGIUM-ORIG-NAME` || suite 1.0.
- Visible suffix is still `.aes` / `.argon` (KDF, not AEAD).
- CBC **plus** an RSA wrap list is suite `"1.2"` with payload alg still 3.

### 4.6 RSA-OAEP-SHA256 wrap — v1.2 shipped

RSA is a **wrap dimension**, not a fourth payload algorithm. Do not emit `alg = 4`. Do not invent a second envelope family.

| Item | Locked as |
|---|---|
| Wrap primitive | RSA-OAEP with SHA-256 (`RSAEncryptionPadding.OaepSHA256` / Web Crypto `RSA-OAEP` + `SHA-256`) |
| What is wrapped | The **32-byte content key only** |
| Payload | Unchanged: alg 1 GCM, 2 ChaCha, 3 CBC+HMAC. Same 64 KiB frame pump |
| Suite | Minor **2** when the wrap list is present. Peek `"1.2"`. Trailer version stays `"1.0"` (same field layout plus a grown wrap region; `bodyLength` is the seek contract) |
| Flag | `FlagHasWrap = 32` (bit 5). Bit 2 stays reserved for a future detached signature. Do not reuse bit 2 for wraps |
| Wrap count | 1..8. Zero wraps means v1.0 / v1.1 as today |
| Record | `wrapAlg u8` (1 = RSA-OAEP-SHA256) + `keyBits u16 LE` + thumbprint 32 + `wrappedLen u16 LE` + wrapped key |
| Thumbprint | SHA-256 of the SubjectPublicKeyInfo DER (`ExportSubjectPublicKeyInfo`). Lower-case hex in Peek / JSON |
| Key size | Min **2048**, prefer **3072**, max **4096**. Tests and the gallery may use 2048 with a caption that the library default is 3072 |
| RSA-only Seal | Draw 32 random bytes, kdf = 0, visible suffix `.aes`. No Argon2 |
| Secret + wraps | Allowed. Open with the matching secret **or** a matching RSA private / ring |
| Also wrap to me | Optional extra wrap slot (`alsoWrapTo`). Same list format; no second envelope |
| Open match | Match wrap thumbprint to a private key. Do **not** try every key. Wrong key fails closed: `The envelope is corrupt.` |
| Peek | May list wrap thumbprints and wrap count. Never company name, subject, title, or issuedTo |
| Trailer contents | Thumbprints only. A hex dump of the envelope must not contain `CompanyX` / subject / application as plaintext |

Placement in the trailer body: after `nameCt` (272), before `mac` (32). `bodyLength` in the 17-byte footer is the source of truth. VerifyMac hashes `body.Length - 32`, never the v1.0 constant 433.

Public type: `EncryptionRsaKey`.

```csharp
public sealed class EncryptionRsaKey : IDisposable
{
    public const int MinBits = 2048;
    public const int PreferredBits = 3072;
    public const int MaxBits = 4096;
    public const byte WrapAlgOaepSha256 = 1;

    public static EncryptionRsaKey Generate(int keyBits = PreferredBits);
    public static EncryptionRsaKey FromPublicSpki(ReadOnlySpan<byte> spki);
    public static EncryptionRsaKey FromPkcs8(ReadOnlySpan<byte> pkcs8);
    public byte[] ExportPublicSpki();
    public byte[] ExportPkcs8();          // throws if public-only
    public EncryptionRsaKey PublicOnly();
    public byte[] Wrap(ReadOnlySpan<byte> contentKey32);
    public byte[] Unwrap(ReadOnlySpan<byte> wrapped);
    public int KeyBits { get; }
    public byte[] Thumbprint { get; }     // SHA-256(SPKI)
    public string ThumbprintHex { get; }
    public bool CanUnwrap { get; }
    public void Dispose();                // zeros PKCS8
}
```

One private key is one modulus. It does not have many different public keys. Isolation is “this public key,” not “this company nickname.”

### 4.7 Key ring — v1.2 shipped

Named RSA wrap keys. Lives in this project. JSON format `VESTIGIUM-KEYRING` 1.1 (1.0 still loads; missing status defaults Active). The ring file itself **should** be a Vestigium envelope when written to disk (host concern: Seal the JSON). The library’s `ToJson` / `FromJson` are the plaintext contract.

Two lists:

| List | Role | Material |
|---|---|---|
| **pairs** | Receive / Open | Private + public |
| **contacts** | Send / Seal | Public only |

Same `issuedTo` may hold many keys: CompanyX / ApplicationX and CompanyX / ApplicationY are two records, two moduli.

Record fields (UTF-16 character counts, trimmed):

| Field | Limit | Notes |
|---|---|---|
| `Id` | GUID | Issued at create |
| `Title` | 75 | Required |
| `Subject` | 50 | Required. Subject line, not X.509 |
| `Description` | 220 | Optional |
| `IssuedTo` | 75 | Required. Company / person / service name |
| `IssuedToKind` | enum | Organization, Person, Service, Host |
| `Application` | 50 | Optional. Distinguishes AppX vs AppY for the same issuedTo |
| `Role` | enum | Receive (pair) / Send (contact) |
| `Status` | enum | Active, Disabled, Expired, Retired, Compromised |
| `ExpiresUtc` | DateTimeOffset? | Clock expiry. Active + past expiry → EffectiveStatus Expired |
| `StatusChangedUtc` | DateTimeOffset? | Last Enable / Disable / Expire / Retire / Compromise |
| `KeyBits` | int | 2048–4096 |
| `ThumbprintSha256` | hex | SHA-256 of SPKI |
| `Escrow` | bool | True only when Issue kept the private |

`Issue(title, subject, issuedTo, …, escrow: false)`:

1. Generate a pair.
2. Store the **public** as a contact on the operator ring.
3. Return the **private** as a one-time slip (`EncryptionRsaKey`).
4. Do **not** keep the private on the ring unless `escrow: true`.

`FindPrivate(thumbprint, override?)` looks at pairs that can unwrap. Disabled / Expired pairs throw `EncryptionTokenException` unless an override is supplied. Retired / Compromised throw and **cannot** be overridden. A contact cannot unwrap.

`RequireForSeal` / `RequireForOpen` are the operator APIs. Raw `EncryptionRsaKey` slips ignore ring status (the company still has the key).

`Enable` restores Disabled / Expired to Active. Enable of Retired or Compromised throws. `Disable` / `Expire` of Retired or Compromised throw and do not downgrade. `Retire` and `Compromise` are terminal (Compromise wins).

`EncryptionKeyOverride.Request(requestedBy, reason)` rejects PEM / `PRIVATE KEY` / PKCS8 / 32+ hex / 44+ Base64. Actor ≤ 50, reason ≤ 80.

`ExportPublicSlip` is JSON a company can import as a contact (public SPKI + metadata). It never includes PKCS8.

Over-limit title / subject / issuedTo throws `ArgumentException` naming the field. Blank required fields throw.

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

Do not switch algorithm just because the file is large. Framing is the large-file story for AES-GCM, ChaCha20-Poly1305, and AES-256-CBC+HMAC. RSA-wrapped payloads (v1.2) keep the same pump.

### 5.2 Limits that follow from framing

| Limit | v1 rule |
|---|---|
| Max frames | \(2^{32}\) (nonce counter is 32-bit). At 64 KiB that is 256 TiB of plaintext — far above host captures. Do not raise frame size to dodge this. |
| Source | Seekable **or** unknown-length. Known length writes `frameCount` in the header and the trailer. Unknown length writes header `frameCount = 0` and the real count in the trailer. |
| Unknown-length streams | **v1**, via the trailer. Destination must still be seekable so the trailer can be appended. |
| In-place encrypt | Forbidden. Write a destination. The host may replace the source afterwards. |
| RAM | O(frame size), not O(file size). Trailer is 482 bytes in v1.0 (no wrap list). A wrap list grows the body; still O(1) relative to the file. |

### 5.3 Roadmap algorithms must keep the same large-file rule

This is the “working around” that CBC and RSA needed. It is specified so a later implementer cannot “just call `RSA.Encrypt` on the file.”

**AES-256-CBC (v1.1) — shipped**

- Still 64 KiB frames.
- Fresh 16-byte IV **per frame**. Never one IV for a multi-GB file.
- Encrypt-then-MAC: HMAC-SHA256 over header prefix + frame index + IV + ciphertext. Reject CBC without HMAC.
- Stream exactly as GCM: one frame in memory.
- PKCS#7 padding applies **per frame**, not to the whole file.
- Suite minor 1. Payload frames use alg 3. Hidden original name stays GCM.

**RSA (v1.2) — shipped**

- RSA never sees the payload.
- Draw / derive the 32-byte content key as today (or randomly for RSA-only Seal).
- RSA-OAEP (SHA-256) wraps **only that content key**, once per recipient, into a wrap list (max 8).
- Payload frames stay AES-256-GCM, ChaCha20-Poly1305, or AES-256-CBC+HMAC.
- Minimum modulus 2048-bit; prefer 3072; max 4096.
- Encrypting 4 GB with RSA directly is forbidden in this library, including later versions.
- Large-file workaround: hybrid encryption. RSA cost is constant (N OAEP wraps, N ≤ 8). Payload cost stays linear in file size via AEAD frames.

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
| `suiteMinor` | u8 | 0 | Additive suite bump (1 = CBC, 2 = RSA wrap list) |
| `headerMajor` | u8 | 1 | This header layout |
| `headerMinor` | u8 | 0 | Additive header fields |

Trailer body starts with the same four version bytes (`suiteMajor/Minor`, `trailerMajor/Minor`). v1.0 writes `1,0,1,0` in both places. They must match. Wrap files write suite minor 2 in **both** header and trailer.

Compat rules (so we do not paint ourselves into a corner):

- **Same major, higher minor:** a v1.0 reader may parse the prefix it knows and ignore trailing body bytes. `bodyLength` in the footer is the source of truth for how far to seek. A v1.0 reader that **assumes** MAC starts at offset 433 will mis-read a wrap trailer; Open of wrap files is a v1.2 reader. This codebase owns both.
- **Higher major:** `NotSupportedException` naming `suiteMajor` or `trailerMajor`. Do not guess.
- **Lower major:** only if we ever ship a v2 reader; v1.0 does not read a future v2.

`IsVestigiumFile` is true when the last 13 bytes are `VESTIGIUM TRL` **or** the first 13 bytes are `VESTIGIUM HDR`. Either end is enough to say “this is a suite file.”

### 6.0.1 Header bytes

```
magic          13     "VESTIGIUM HDR"
suiteMajor     u8     = 1
suiteMinor     u8     = 0, 1, or 2
headerMajor    u8     = 1
headerMinor    u8     = 0
alg            u8     = 1 AES-256-GCM, 2 ChaCha20-Poly1305, 3 AES-256-CBC + HMAC-SHA256
kdf            u8     = 0 raw key, 1 Argon2id
kdfMemMiB      u8     = 64 when kdf=1, else 0
kdfIter        u8     = 3  when kdf=1, else 0
kdfPar         u8     = 1  when kdf=1, else 0
salt           16 bytes when kdf=1, else omitted
fileNonce      12 bytes
frameSize      u32    = 65536
frameCount     u64    hint; 0 means “read the trailer”
frames         AEAD: frameCount × (ciphertext || 16-byte tag)
               CBC:  frameCount × (IV(16) || PKCS#7 ciphertext || HMAC-SHA256(32))
```

`suiteMinor` rule:

| Condition | suiteMinor | Peek |
|---|---|---|
| Wrap list present | 2 | `"1.2"` |
| Else alg 3 (CBC) | 1 | `"1.1"` |
| Else alg 1 or 2 | 0 | `"1.0"` |

GCM and ChaCha envelopes **without** wraps stay 1.0. CBC **without** wraps stays 1.1. Any wrap list is 1.2 even if the payload is CBC.

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
nathan.txt     Seal (RSA wrap, random key)     →  nathan.aes
nathan.aes     Open (with secret or matching RSA) →  nathan.txt
```

`Path.GetFileName("C:\\inbox\\nathan.txt")` is what is stored (`nathan.txt`). Directories are never stored.

### Visible suffix

Exactly two write suffixes. Nothing longer. RSA wrap does not add `.rsa`.

| Condition | Suffix | Disk name for `nathan.txt` |
|---|---|---|
| Raw 32-byte key (`FromKey`) or RSA-only wrap | `.aes` | `nathan.aes` |
| Passphrase (`FromPassphrase` → Argon2id) | `.argon` | `nathan.argon` |

AES-256-GCM is still the default **cipher** in both rows. `.aes` vs `.argon` tells the operator how the key was made, not which AEAD ran. ChaCha20-Poly1305, AES-256-CBC+HMAC, and RSA wrap use the same two suffixes (raw key / RSA-only → `.aes`, passphrase → `.argon`). Peek/Validate name the actual `alg` byte and wrap count.

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

1. Derive or unwrap the content key, verify the structural `mac`, decrypt the hidden name.
2. If `destinationPath` is a directory, write `{destinationPath}/{originalName}` → `nathan.txt`.
3. If `destinationPath` is a file, write that path as-is. The hidden name is still available via `RevealOriginalFileName`.
4. Do not trust the visible suffix. A file renamed to `other.bin` still restores `nathan.txt` when the destination is a directory.

Peek **without** a secret never returns the original name. It may set `HasHiddenOriginalName = true`. It may list wrap thumbprints.

### Why the original name is encrypted

`passwords.txt` as a Desktop name is a leak. `passwords.aes` is less of one. A hex dump of the trailer must not show `passwords.txt` either, so the name is an AEAD field under the content key, not plaintext in the tail.

### What we do not store

- Full paths (`C:\Users\...\nathan.txt`)
- Alternate streams, ADS, Unix mode bits
- A second copy of the original name in the header
- Company name, subject, title, issuedTo, or application in the envelope (key ring only)

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

v1.0 `bodyLength` is **465** when there is no wrap list. A wrap list grows the body; readers use `bodyLength`, not a constant, when seeking. VerifyMac uses `body.Length - 32`.

### Trailer body (465 bytes in v1.0 with no wraps, little-endian)

Fixed prefix through `nameCt` is **433** bytes. Then an optional wrap region. Then `mac` 32.

```
suiteMajor      u8     = 1
suiteMinor      u8     = 0 / 1 / 2
trailerMajor    u8     = 1
trailerMinor    u8     = 0
alg             u8     = 1 AES-256-GCM, 2 ChaCha20-Poly1305, 3 AES-256-CBC + HMAC-SHA256
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
wraps           0..N   optional wrap list (§6.2.1); omitted when wrap count is 0
mac             32     structural HMAC-SHA256 (Encryption-owned)
```

465 + 4 + 13 = **482** bytes at EOF in v1.0 (no wraps). The name slot is **fixed** so seeking stays a single `bodyLength` read even when there is no original name (string Seal). The wrap list is the only variable-length region before `mac`.

`flags` bits:

| Bit | Value | Meaning when set | When |
|---|---|---|---|
| 0 | 1 | Header `frameCount` is not authoritative | v1.0 |
| 1 | 2 | Hidden original name is present (`nameLen` > 0) | v1.0 |
| 2 | 4 | Detached public-key signature follows `mac` | v1.3+ (not wrap) |
| 3 | 8 | `sha256` slot is filled (Hashing library) | later |
| 4 | 16 | `hmacSha256` slot is filled (Hmac library) | later |
| 5 | 32 | RSA wrap list is present (`FlagHasWrap`) | v1.2 |

v1.0 writers set bit 0 when the header count is unknown, and bit 1 when an original file name was Sealed. Bits 3–4 stay 0 and the two 32-byte hash/HMAC slots stay zeros. Encryption must not compute a file hash or a Helpers.Hmac value “to be helpful.” v1.2 writers set bit 5 when the wrap list is non-empty.

### 6.2.1 Wrap list (v1.2)

```
count           u8     1..8
repeat count:
  wrapAlg       u8     1 = RSA-OAEP-SHA256
  keyBits       u16 LE 2048..4096
  thumbprint    32     SHA-256(SPKI)
  wrappedLen    u16 LE
  wrappedKey    wrappedLen bytes
```

Unknown `wrapAlg` → `NotSupportedException("wrapAlg")`. `keyBits` below 2048 → `NotSupportedException("keyBits")`. Count 0 in a present region, count > 8, or trailing unread bytes → `The envelope is corrupt.`

A 2048-bit wrap is 256 ciphertext bytes; 3072 is 384; 4096 is 512. Body length is `433 + wrapBytes + 32`.

### Hidden original name (v1.0)

This is how `nathan.txt` survives a round-trip while the disk file is `nathan.aes`.

1. Take `Path.GetFileName(sourcePath)` only. UTF-8, 1..255 bytes. No `/`, `\`, NUL, or `..`.
2. Write `nameLen`, then pad the UTF-8 bytes with zeros to 256 bytes.
3. Draw `nameNonce` (12 random bytes). Do **not** reuse `fileNonce` or a frame index.
4. AEAD-encrypt the 256-byte padded buffer with the **content key**, same `alg` as the payload except CBC which uses GCM for the name slot.
   - AAD = UTF-8 `VESTIGIUM-ORIG-NAME` || `suiteMajor` || `suiteMinor` of the **name** suite (1.0)
   - Output = 256 ciphertext + 16-byte tag → `nameCt` (272 bytes)
5. Set flag bit 1.

String Seal and unnamed streams: `nameLen = 0`, `nameNonce` and `nameCt` all zeros, bit 1 clear. The 272-byte slot is still written so the prefix stays 433.

Decrypt (Open / `RevealOriginalFileName` only, after the structural `mac` checks):

1. If bit 1 is clear or `nameLen` is 0, there is no original name.
2. AEAD-open `nameCt` with `nameNonce` and the same AAD. Tag mismatch → `CryptographicException` (do not distinguish “wrong password”).
3. Take the first `nameLen` bytes as UTF-8. Re-validate no `/`, `\`, NUL, `..`.

Peek without a secret must not copy `nameCt` into logs or `EncryptionFileInfo.OriginalFileName`. It may set `HasHiddenOriginalName`.

### Reserved slots vs the structural `mac`

Three different integrity ideas share the tail. Keep them apart:

| Slot | Owner | v1.0 | Later |
|---|---|---|---|
| `mac` (last 32 of body) | Encryption | Written. HKDF+HMAC over the trailer prefix including wraps. Proves preamble + counts + wrap list. | Stays. |
| `sha256` | `Vestigium.Helpers.Hashing` | 32 zero bytes. Flag bit 3 clear. | SHA-256 of **plaintext** (streamed while Sealing). Flag bit 3 set. |
| `hmacSha256` | `Vestigium.Helpers.Hmac` | 32 zero bytes. Flag bit 4 clear. | HMAC-SHA256 over ciphertext frames or over the header+frames, using a caller MAC key distinct from the content key. Flag bit 4 set. |

Do not reuse `mac` as the future Helpers.Hmac field. `mac` is how Encryption knows its own trailer was not clipped. Helpers.Hmac is a different key and a different library.

Structural `mac`:

```
macKey = HKDF-SHA256(
    ikm    = 32-byte content key,
    salt   = fileNonce,
    info   = UTF-8 "VESTIGIUM-TRL-HMAC",
    len    = 32)

mac = HMAC-SHA256(macKey, trailer body without the last 32 bytes)
```

Zeros in the reserved slots are part of that prefix today, so filling them later **must** bump `trailerMinor` and change the MAC input rule in that revision (MAC covers the filled slots). v1.0 readers that see a higher `trailerMinor` parse the prefix they know and do not require `sha256`/`hmacSha256` to be zero.

Wrap bytes sit **inside** the MAC input (they are before the last 32). Tampering with a wrap record fails the structural MAC.

### What the trailer is not

- Not a second cipher and not a place to store the content key in the clear.
- Not a substitute for per-frame GCM / Poly1305 tags.
- Not a plaintext filename sidecar. The original name is AEAD-encrypted.
- Not a full path store. Name only (`nathan.txt`).
- Not a company directory. Thumbprints only.
- Not `Vestigium.Helpers.Hmac` and not `Vestigium.Helpers.Hashing`. Those libraries fill reserved slots when they exist.

### 6.3 Validation (no decrypt of frames)

These methods exist so a gallery or host can say “Vestigium encrypted this” and print the trailer.

| Method | Secret? | Throws? | Does |
|---|---|---|---|
| `IsVestigiumFile(path)` / `IsVestigium(stream)` | No | No | True if head magic is `VESTIGIUM HDR` or tail magic is `VESTIGIUM TRL`. |
| `TryPeekFile(path, out info)` | No | No | False if not a suite file. Fills `EncryptionFileInfo` from the trailer when possible. |
| `PeekFile(path)` / `Peek(stream)` | No | Yes if not suite | Trailer facts. Does not check `mac`. Does not return the original name. May list wrap thumbprints. |
| `RevealOriginalFileName(path, secret \| rsa \| ring)` | Yes | Yes if not suite / bad mac | Hidden `nathan.txt`. |
| `ValidateFile(path)` | No | No (returns a result) | Magics, versions, `bodyLength`, agreement, `HasHiddenOriginalName`, wrap count. |
| `ValidateFile(path, secret)` | Yes | No (returns a result) | Plus structural `mac` and `OriginalFileName` when the secret derives the content key. Still does not decrypt payload frames. |

`EncryptionValidationResult`:

| Property | Meaning |
|---|---|
| `IsVestigium` | Family magic present at head or tail |
| `SuiteVersion` | `"1.0"` / `"1.1"` / `"1.2"` from the numeric fields |
| `HeaderVersion` / `TrailerVersion` | `"1.0"` for trailer layout; header version follows suite minor |
| `HeaderPresent` / `TrailerPresent` | Each magic found |
| `HeaderTrailerAgree` | alg, kdf, salt, nonce, frameSize, counts match the rules in §6.2 |
| `ReservedHashEmpty` | `sha256` slot is 32 zeros (expected until Hashing fills it) |
| `ReservedHmacEmpty` | `hmacSha256` slot is 32 zeros (expected until Hmac fills it) |
| `StructuralMacValid` | `null` without a secret; `true`/`false` with one |
| `HasHiddenOriginalName` | Flag bit 1. Not the name itself. |
| `OriginalFileName` | `nathan.txt` only when a secret was supplied and mac verified |
| `Info` | The `EncryptionFileInfo` when peek succeeded (includes wrap count / thumbprints) |
| `Problems` | Short English reasons if a check failed (no secrets) |

Validate does **not** Open payload frames. A host can light a green “Vestigium 1.2 / AES-256-GCM / 2 wraps / 12 frames / 720 KB / has hidden name” row from Validate + Peek alone. The string `nathan.txt` appears only after a credential is supplied. Company names never appear.

Reserved algorithm ids:

| alg | Meaning | Version that may emit it |
|---|---|---|
| 3 | AES-256-CBC + HMAC-SHA256 | v1.1 — **shipped** |
| 4 | unused | Do **not** emit. RSA wrap is a trailer list, not a payload alg |

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
    public static string? RevealOriginalFileName(string path, EncryptionRsaKey rsa);
    public static string? RevealOriginalFileName(string path, EncryptionKeyRing ring);

    public static bool IsVestigiumFile(string path);
    public static bool IsVestigium(Stream source);
    public static bool TryPeekFile(string path, out EncryptionFileInfo info);
    public static EncryptionFileInfo PeekFile(string path);
    public static EncryptionFileInfo Peek(Stream source); // Seek from end
    public static EncryptionValidationResult ValidateFile(string path, EncryptionSecret? secret = null);
    public static EncryptionValidationResult Validate(Stream source, EncryptionSecret? secret = null);

    public static string SealString(string plaintext, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null);
    public static string SealString(string plaintext, IReadOnlyList<EncryptionRsaKey> rsaRecipients, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, EncryptionRsaKey? alsoWrapTo = null);
    public static string OpenString(string sealedBase64, EncryptionSecret secret);
    public static string OpenString(string sealedBase64, EncryptionRsaKey rsa);
    public static string OpenString(string sealedBase64, EncryptionKeyRing ring);

    public static string SealFile(string sourcePath, string destinationPath, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, SecureDeleteMode shredPlaintext = SecureDeleteMode.Keep, IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null);
    public static string SealFile(string sourcePath, string destinationPath, IReadOnlyList<EncryptionRsaKey> rsaRecipients, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, SecureDeleteMode shredPlaintext = SecureDeleteMode.Keep, EncryptionRsaKey? alsoWrapTo = null);
    public static string OpenFile(string sourcePath, string destinationPath, EncryptionSecret secret);
    public static string OpenFile(string sourcePath, string destinationPath, EncryptionRsaKey rsa);
    public static string OpenFile(string sourcePath, string destinationPath, EncryptionKeyRing ring);
    public static void SecureDelete(string path, SecureDeleteMode mode);

    public static void SealFile(Stream source, Stream destination, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, long? plaintextLength = null, string? originalFileName = null, IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null);
    public static void OpenFile(Stream source, Stream destination, EncryptionSecret secret);
    public static void OpenFile(Stream source, Stream destination, EncryptionRsaKey rsa);
    public static void OpenFile(Stream source, Stream destination, EncryptionKeyRing ring);
}

public sealed class EncryptionFileInfo
{
    public string SuiteVersion { get; init; }      // "1.0" / "1.1" / "1.2"
    public string HeaderVersion { get; init; }
    public string TrailerVersion { get; init; }    // "1.0"
    public EncryptionAlgorithm Algorithm { get; init; }
    public bool UsedArgon2id { get; init; }
    public int FrameSize { get; init; }
    public ulong FrameCount { get; init; }
    public ulong PlaintextLength { get; init; }
    public DateTimeOffset? CreatedUtc { get; init; }
    public bool Sha256ReservedFilled { get; init; }
    public bool HmacSha256ReservedFilled { get; init; }
    public bool HasHiddenOriginalName { get; init; }
    public string? OriginalFileName { get; init; }
    public bool HasRsaWrap { get; init; }
    public int WrapCount { get; init; }
    public IReadOnlyList<string> WrapThumbprints { get; init; } // lower-case hex, no company names
}

public enum EncryptionAlgorithm
{
    Aes256Gcm = 1,
    ChaCha20Poly1305 = 2,
    Aes256CbcHmac = 3
}

public enum SecureDeleteMode
{
    Keep = 0,
    ThreePass = 3,
    SevenPass = 7
}
```

`EncryptionSecret`, `EncryptionValidationResult` stay as v1.0. RSA types are §4.6 / §4.7.

Rules:

- `SealString` / `OpenString` take and return strings. Plaintext is UTF-8. Ciphertext is Base64 of the Vestigium envelope (header + frames + trailer).
- File overloads create the destination directory, stream, and return the path actually written.
- `SealFile(sourcePath, destinationPath, …)`: if `destinationPath` is a directory, the helper writes `{stem}.aes` or `{stem}.argon` there and hides `Path.GetFileName(sourcePath)` in the trailer. If it is a file, that path is used as-is and the original name is still hidden.
- `OpenFile(sourcePath, destinationPath, …)`: if `destinationPath` is a directory, the helper restores the hidden original name (`nathan.txt`). If it is a file, that path is used as-is.
- Stream overloads do **not** dispose caller streams. `plaintextLength` is optional. When omitted (or the source is not seekable), header `frameCount = 0`, flag bit 0 is set, and the trailer carries the real count. Destination must allow the trailer to be appended. Pass `originalFileName` to hide a name on a stream Seal.
- `SealedFileName("nathan.txt", secret)` is `"nathan.aes"` or `"nathan.argon"`.
- `RevealOriginalFileName` needs a secret, RSA private, or ring; verifies `mac`; decrypts the name slot; returns `nathan.txt` or null.
- `IsVestigiumFile` / `TryPeekFile` never throw on a random file; they return false.
- `PeekFile` / `Peek` read the trailer. No secret. They do not verify `mac` and they do not return the original name. They may return wrap thumbprints.
- `ValidateFile` without a secret checks magics, versions, sizes, header/trailer agreement, reserved hash/HMAC slots zeros, and `HasHiddenOriginalName`.
- `ValidateFile` with a secret also checks the structural `mac` and may fill `OriginalFileName`. It still does not decrypt payload frames.
- `FromKey` throws if the span is not 32 bytes.
- `FromPassphrase` throws if blank.
- `Dispose` clears the derived or copied key material.
- `Open*` reads `alg` and `kdf` from the header and confirms them against the trailer. The caller does not pass the algorithm on decrypt.
- Wrong passphrase / wrong key / wrong RSA private / bit flip → `CryptographicException` with message `The envelope is corrupt.` Do not distinguish those cases.
- `EncryptionSecret` is not serializable and must not override `ToString` with the passphrase or key.
- `SealFile(..., shredPlaintext)` shreds the **source** only after a successful seal. A failed seal leaves the plaintext file and deletes a dest the helper just created. Default is `Keep`.
- `SecureDelete(path, ThreePass | SevenPass)` overwrites the whole length in 64 KiB chunks (random, then zeros), `Flush(flushToDisk: true)` after each pass, then `File.Delete`. `Keep` is invalid on this method. Missing source → `FileNotFoundException`. Directories are refused. Read-only is cleared, then shredded. Never log file contents.
- RSA-only Seal (`SealString` / `SealFile` with a recipient list and no secret) draws a random 32-byte content key, sets kdf = 0, writes `.aes`.
- `alsoWrapTo` appends one extra wrap slot. Combined list must be 1..8 unique-enough keys (duplicate thumbprints are the caller’s problem; the reader matches the first).
- Open with a ring matches wrap thumbprints through `FindPrivate`. Disabled / Expired tokens throw `EncryptionTokenException` (`The token is disabled.` / `The token is expired.`) unless `EncryptionKeyOverride` is passed. Retired / Compromised throw `The token is not usable.` and cannot be overridden. TokenException is not converted to `The envelope is corrupt.`
- `OpenString` / `OpenFile` / `RevealOriginalFileName` ring overloads take an optional `EncryptionKeyOverride`.

`Probe` seals and opens a short fixture in memory. It must not write `%DESKTOP%` and must not log the fixture text.

---

## 8. Logging

ALCOA+ for this library:

| Letter | How Encryption meets it |
|---|---|
| Attributable | APPID + subcategory (`Encryption` / `Token`) + 8-hex key prefix + override `by=` / `reason=` |
| Legible | Structured `verb: detail` English. No exception dumps. |
| Contemporaneous | Vestigium.Logging timestamp. `StatusChangedUtc` on the record. |
| Original | JSONL append-only. The library never rewrites a line. |
| Accurate | Counts and alg bytes only. Fail closed messages are generic. |
| Complete | Pending then Success or Failed on Seal / Open / Shred. Token Disable / Expire / Refuse / Override is a Warning. TokenException is not logged as Failed. |
| Consistent | Same verbs (`Seal`, `Open`, `Peek`, `Shred`, `Disable`, `Expire`, `Override`, `Refuse`). |
| Enduring | Host JSONL under `%ProgramData%\Vestigium\Logs\{APPID}\`. |
| Available | `HelperLog.RecentJsonLines` after the host initializes. |

| Event | Level | Status |
|---|---|---|
| Seal start (string or file, alg, kdf, wrap count, byte/frame counts — not secrets) | Information | Pending |
| Seal complete (path or `string`, frames, bytes, wrap count) | Information | Success |
| Open start | Information | Pending |
| Open complete | Information | Success |
| Tag mismatch / crypt failure | Error | Failed |
| Missing source file | Error | Failed |
| Unsupported envelope field | Error | Failed |
| Peek (alg, frames, plaintext length, wrap count — not secrets, not company names) | Information | Success |
| Shred start (pass count — not contents) | Information | Pending |
| Shred complete | Information | Success |
| Shred failed | Error | Failed |
| Token Disable / Expire / Retire / Compromise | Warning | Success |
| Token Refuse (disabled / expired / terminal, no usable override) | Warning | Success |
| Token Override (disabled / expired + sanitized actor and reason) | Warning | Success |
| Token Enable | Information | Success |
| Token Issue | Information | Success |

Category = `Helpers`. Subcategory = `Encryption` or `Token`. APPID = host APPID.

Never log, **including Debug enter/exit**: plaintext, passphrase, key bytes, PKCS8, SPKI, salt that could be reused as a key, Base64 ciphertext, the **hidden original file name**, company / subject / issuedTo, full thumbprints (8-hex prefix only), override reasons that look like PEM / long hex / long Base64. Log the visible path (`nathan.aes`) and wrap **count**. Do not pass `Exception` into `HelperLog` from this library — the engine stores `EXCEPTION = exception?.ToString()`, which can contain paths and key material.

`EncryptionLog.Safe` redacts any message that looks like a secret before it is written. `EncryptionKeyOverride.Request` rejects those strings so they never become audit text.

---

## 9. Demo contract

`Vestigium.Helpers.Encryption.Demo` is a WPF gallery. After implementation:

1. `HelperWpfHost.Start` with APPID `Encryption`. Gallery chrome, not the shared skeleton.
2. Tabs: Overview, **AES-256-GCM**, **ChaCha20-Poly1305**, **AES-256-CBC + HMAC**, **Argon2id**, **RSA / key ring**, Validate, JSONL.
3. AES and ChaCha default to a raw 32-byte key (`.aes`). Argon2id uses `EncryptionSecret.FromPassphrase` (`.argon`, 64 MiB / 3 / 1) with a visible throwaway passphrase field (`gallery-demo-only`).
4. Each cipher tab round-trips a UTF-8 string (`SealString` / `OpenString`) and a file (`Choose file…` / `nathan.txt`, then Seal / Open). Open restores the hidden original name.
5. File Seal offers **Keep original**, **3-pass random + zero**, or **7-pass random + zero**. Shred runs only after a successful seal and only on the unencrypted source.
6. RSA / key ring tab: generate an operator pair; Issue CompanyX / ApplicationX and CompanyX / ApplicationY (2048-bit in the gallery with a caption that the library default is 3072); Seal to the selected contact; optional also wrap to Ops; Open as Ops / AppX / AppY to show isolation; Peek wrap thumbprints. Issue is issue-and-forget on the operator ring (public contact kept; private slip held in the gallery so isolation can be demonstrated). **Token edition:** Enable / Disable / Expire the selected ring token. Disabled or expired Seal/Open via the ring requires an override (requestedBy + reason); a warning is logged. Compromised / retired cannot be overridden. JSONL never shows issuedTo or PKCS8.
7. JSONL under `%ProgramData%\Vestigium\Logs\Encryption\`. Desktop exports under `%DESKTOP%\Vestigium\Exports\Encryption\`.

Do not put a real production passphrase or a production RSA private in the gallery source. A throwaway gallery secret is fine if it is obviously fake (`gallery-demo-only`).

The web gallery mirrors the same tabs.

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
- Default `SealFile` leaves the unencrypted source on disk.
- `SealFile(..., SecureDeleteMode.ThreePass)` deletes the source after a successful seal; the envelope still opens to the original bytes and hidden name.
- `SecureDelete(path, SevenPass)` deletes an empty file. Missing path throws `FileNotFoundException`. `Keep` on `SecureDelete` throws `ArgumentOutOfRangeException`.
- Open of a ChaCha file renamed to `.bin` into a directory still restores the hidden original name (header/trailer win).
- `SealString` / `SealFile` with `Aes256CbcHmac` round-trips; Peek reports alg 3 and suite `"1.1"`. Trailer version stays `"1.0"`. Default Seal stays GCM / `"1.0"`.
- CBC PKCS#7 frame sizes 1, 15, 16, 17 bytes round-trip. Empty CBC file is 0 frames + trailer.
- CBC hidden original name: Seal `nathan.txt` writes `nathan.aes`; Peek does not reveal the name; Reveal/Open returns `nathan.txt`. Name slot is AES-256-GCM.
- Flipping a CBC per-frame HMAC or IV throws `CryptographicException` on Open. Trailer structural mac may still validate.
- CBC 200 KiB (4 frames) round-trips.
- Tests never use the real Desktop.

v1.2 RSA wrap (tests use 2048-bit keys):

- RSA-only `SealString` round-trips; Peek reports suite `"1.2"`, wrapCount ≥ 1, payload alg still GCM (1). Trailer version stays `"1.0"`.
- Isolation: seal to CompanyX / ApplicationX; Open with ApplicationY private fails closed (`The envelope is corrupt.`); Open with ApplicationX succeeds.
- Also wrap to me: Open with Ops **and** with AppX both succeed.
- Hidden original name: Seal `nathan.txt` RSA-only writes `nathan.aes`; Peek does not reveal the name; Reveal/Open with the matching private returns `nathan.txt`.
- 200 KiB file RSA wrap round-trips (SHA-256 of plaintext).
- Envelope bytes do not contain the UTF-8 company name, subject, or application.
- Peek wrap thumbprints are 64-char lower-case hex and match the recipient SPKI hash. Peek does not include issuedTo.
- Wrong RSA private throws `CryptographicException` with message `The envelope is corrupt.`
- Secret + wraps: Open with the original secret still works.
- CBC + wrap: Peek suite `"1.2"`, alg 3.
- GCM without wrap stays suite `"1.0"`.
- Key ring JSON round-trip: pairs and contacts survive `ToJson` / `FromJson`. Title 75 / subject 50 / description 220 / issuedTo 75 / application 50 reject over-limit.
- `Issue` without escrow: contact is public-only; `FindPrivate` on that thumbprint is null on the operator ring; the returned slip unwraps.
- `Issue` with escrow: `FindPrivate` returns the pair.
- Disable a receive pair: `OpenString(..., ring)` throws `EncryptionTokenException` (`The token is disabled.`). The same call with `EncryptionKeyOverride.Request` succeeds and logs Warning `Override`. The exported slip still opens without an override.
- Expire (past timestamp) requires an override. Enable restores Active.
- Compromise / Retire: override is refused (`The token is not usable.`). Disable / Enable of Compromised throw and do not change status.
- `EncryptionKeyOverride.Request` rejects PEM, 32+ hex, and 44+ Base64.
- Logs after Disable/Override never contain issuedTo, company names, PKCS8, PEM, the full thumbprint, or Base64 ciphertext. They do contain the 8-hex prefix, `by=`, and `reason=`. Failed lines never include an `EXCEPTION` payload.
- Key ring JSON round-trip preserves Status and ExpiresUtc. Format minor is 1.

xUnit, serial logger collection, temp directories only. See `EncryptionSessionTests`.

---

## 11. Non-goals (v1)

| Item | Why |
|---|---|
| Hashing APIs | Sibling Hashing. |
| AES-256-CBC without HMAC | Forbidden. v1.1 ships CBC **only** as Encrypt-then-MAC. |
| RSA / ECC payload encrypt | Forbidden. RSA wraps a content key only. |
| DPAPI / TPM / Windows Hello | Windows-only; this TFM is `net10.0`. |
| Key vault, rotation UI | Host concern. The ring is a JSON document, not a vault. |
| Compress-then-encrypt switch | Out of v1. |
| Encrypting Vestigium JSONL logs | Logging owns `%ProgramData%`. |
| Replacing FileIo | This library talks envelopes, not general paths. |
| In-place encrypt of the source file | Write a destination, then the host may replace. |
| Password managers / secret stores | Host concern. |
| Full paths in the trailer | Name only (`nathan.txt`), never `C:\\Users\\...`. |
| Company / subject in the trailer | Thumbprints only. |
| A separate `Vestigium.Helpers.KeyRing` project | Types live in Encryption. |
| One private with many different public keys | Rejected. One modulus, one public. Many pairs per company if needed. |

---

## 12. Roadmap

### v1.0 — this document (shipped)

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

### v1.1 — AES-256-CBC (interop) — shipped

- Algorithm id 3.
- **Encrypt-then-MAC**: AES-256-CBC then HMAC-SHA256 over header prefix + frame index + IV + ciphertext.
- Still framed (same 64 KiB). Per-frame IV, never a single IV for a multi-GB file.
- Not the default. Exists so a host can read an old vendor dump or emit one.
- Reject CBC without HMAC. Do not ship “just CBC”.
- Large-file workaround: same frame pump as GCM. PKCS#7 per frame.
- Hidden original name stays AES-256-GCM. Suite minor 1 (Peek `"1.1"`).

### v1.2 — RSA envelope wrap — shipped

- Algorithm stays AES-256-GCM (or ChaCha, or CBC) on the payload. **Not** alg 4.
- RSA-OAEP (SHA-256) encrypts the **32-byte content key** only. Payload frames do not change.
- Wrap **list** 1..8 after `nameCt`, before `mac`. Flag bit 5 (`FlagHasWrap = 32`).
- Suite minor 2 (Peek `"1.2"`) when the wrap list is present.
- Public key on Seal, private key (or ring) on Open. Optional also wrap to me.
- Minimum 2048-bit; prefer 3072; max 4096.
- Key ring: pairs vs contacts, Issue-and-forget vs escrow, field limits, JSON `VESTIGIUM-KEYRING`.
- Isolation per public key. Trailer stores thumbprints only.
- This is how large files stay streamable under RSA. Encrypting 4 GB with RSA directly is forbidden in this library, including later versions.
- Large-file workaround: hybrid encryption. RSA cost is constant (one OAEP wrap per recipient). Payload cost stays linear in file size via AEAD frames.

### After Hashing v1 ships

- Fill trailer `sha256` (plaintext, streamed). Set flag bit 3. Bump `trailerMinor`.

### After Hmac v1 ships

- Fill trailer `hmacSha256` with a caller MAC key that is **not** the content key. Set flag bit 4. Bump `trailerMinor`.

### v1.3

- Optional `frameSize` override (64 KiB / 1 MiB).
- Flag bit 2 public-key signature over the trailer body (pairs with v1.2 RSA keys or Ed25519). Not a wrap.

### Explicitly never here

- Custom ciphers
- Hashing
- TLS
- Full-disk encryption
- Logging JSONL as ciphertext in ProgramData
- RSA on the file body
- Company names in the envelope

---

## 13. Siblings: Hashing and Hmac

`Vestigium.Helpers.Hashing` already exists as a skeleton (Identity + Probe). `Vestigium.Helpers.Hmac` is a planned sibling, not created in this change.

- Hashing must not encrypt. Hmac must not encrypt.
- Encryption must not grow `Hash*` or `Hmac*` public APIs.
- v1.0 trailer already has a 32-byte slot for each. Both are zeros until those libraries exist and an Encryption minor revision fills them.
- Different APPIDs (`Encryption`, `Hashing`, later `Hmac`).
- Hashing / Hmac file APIs must stream. Same “do not `ReadAllBytes` a capture” rule.

There is no `Vestigium.Helpers.KeyRing` sibling. Wrap keys live in Encryption.

---

## 14. Glossary

| Term | Meaning |
|---|---|
| AEAD | Authenticated encryption with associated data (confidentiality + tag) |
| Frame | One AEAD record, ≤ 65 536 bytes of plaintext |
| File nonce | 12-byte nonce family for a Seal; frame index fills the last 4 bytes |
| Envelope | `VESTIGIUM HDR` + frames + `VESTIGIUM TRL` |
| Trailer | EOF record: versions, decrypt preamble, reserved hash/HMAC slots, optional wrap list, structural `mac` |
| Content key | 32-byte key that actually runs AES-GCM / ChaCha / CBC |
| Argon2id | Password KDF that produces the content key |
| Seal / Open | Encrypt / decrypt in this library’s vocabulary |
| Peek | Read trailer metadata from EOF with no secret (no original name; wrap thumbprints ok) |
| Validate | Magics + versions + agreement + optional structural `mac`; with secret may reveal original name |
| Hidden name | Encrypted original file name in the trailer (`nathan.txt` behind `nathan.aes`) |
| Hybrid RSA | RSA wraps the content key; AEAD frames carry the file |
| Wrap list | 1..8 RSA-OAEP records in the trailer. Thumbprints only |
| Pair | Ring row with private + public (receive) |
| Contact | Ring row with public only (send) |
| Issue | Generate a pair, keep public as a contact, return private as a slip. Escrow is explicit |
| Thumbprint | SHA-256 of SubjectPublicKeyInfo DER |
| Isolation | Per public key. AppX cannot Open AppY |

---

## 15. Acceptance

This SRS is accepted when:

1. This file is on `main` under `src/Vestigium.Helpers.Encryption/_Documentation/`.
2. Implementation of §7 + §9 demo + §10 tests follows without inventing hashing APIs. AES-256-CBC+HMAC is the v1.1 cipher (alg 3). RSA wrap is the v1.2 trailer list (not alg 4).
3. Large-file Seals stream; a reviewer can see that `ReadAllBytes` is not the file path.
4. RSA text in §4.6 / §5.3 / §12 is treated as **shipped** work. CBC in those sections is v1.1 and is implemented.
5. `nathan.txt` → visible `.aes` / `.argon`, hidden original name round-trips on Open into a directory.
6. Isolation holds: a file sealed to CompanyX / ApplicationX cannot be opened by ApplicationY. Trailer bytes do not contain the company name.

Implementation of v1.2 is this change set, not a later one.
