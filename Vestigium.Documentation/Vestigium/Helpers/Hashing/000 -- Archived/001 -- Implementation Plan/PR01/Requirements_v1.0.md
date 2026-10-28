# Vestigium.Helpers.Hashing — Requirements Specification

**Document ID:** VEST-HLP-HASH-SRS-000  
**Version:** 1.3  
**Status:** Accepted. Implemented v1.3 (SHA-2, SHA-3, HMAC-SHA256 / SHA-384 / SHA-512 / HMAC-SHA3, KMAC128/256, SHAKE128/256, Argon2id PHC, CRC-32 / CRC-64 / xxHash, hex↔Base64 converters).  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Hashing`  
**TFM:** `net10.0` (not Windows-only)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

This replaces the 8 September 2026 skeleton note. If implementation and this file disagree, this file wins.

---

## 1. Purpose

String and file **digests**, keyed **HMAC-SHA256 / SHA-384 / SHA-512 / HMAC-SHA3**, **KMAC128 / KMAC256**, **SHAKE128 / SHAKE256** XOF, **password verifiers**, and non-cryptographic **checksums** for Vestigium hosts. Integrity checks, cache keys, “did this capture change,” shared MAC secrets, stored credentials.

This library does **not** encrypt. Encryption Seals. Hashing digests. Neither project grows the other’s APIs. There is **no** `Vestigium.Helpers.Hmac` project — HMAC lives here.

```csharp
var hex = HashingHelper.HashString("abc");                 // SHA-256 lowercase hex
var crc = HashingHelper.ChecksumCrc32("123456789");        // cbf43926
var xxh = HashingHelper.ChecksumXxHash("abc");             // XXH64
var b64 = HashingConvert.HexToBase64(hex);
var file = HashingHelper.HashFile(path);                   // streams; no ReadAllBytes
using var key = HmacKey.Generate();                        // 32 random bytes
var mac = HashingHelper.HmacFile(path, key);               // HMAC-SHA256
var mac384 = HashingHelper.HmacString(msg, key, HmacAlgorithm.Sha384);
var mac3 = HashingHelper.HmacString(msg, key, HmacAlgorithm.Sha3_256);
var kmac = HashingHelper.Kmac128(msg, key);
var shake = HashingHelper.Shake128("abc");
var phc = HashingHelper.HashPassword("correct horse");
HashingHelper.VerifyPassword("correct horse", phc);        // true
```

---

## 2. Locked decisions

| # | Decision | Locked as |
|---|---|---|
| 1 | Default digest | **SHA-256**. Empty input is legal (FIPS vector). |
| 2 | SHA-2 family | SHA-256, SHA-384, SHA-512. |
| 3 | SHA-3 family | SHA3-256 / 384 / 512. Opt-in. Default stays SHA-256. SHA3-256 is not “SHA-256 version 3.” OS-gated (`SHA3_256.IsSupported`). |
| 4 | HMAC | **HMAC-SHA256 default.** HMAC-SHA384 / HMAC-SHA512 / HMAC-SHA3-256 / 384 / 512 opt-in via `HmacAlgorithm`. HMAC-SHA3 is OS-gated. No sibling Hmac project. Encryption structural `mac` and CBC frame HMAC stay Encryption. |
| 4a | KMAC | **v1.3.** NIST SP 800-185 `Kmac128` / `Kmac256`. Not HMAC. Same `HmacKey` (min 16). Default tag 32 / 64 bytes. Optional customization bytes. Output 16–1024. OS-gated (`Kmac128.IsSupported`). Never the default of `HmacString`. |
| 4b | SHAKE | **v1.3.** FIPS 202 SHAKE128 / SHAKE256 XOF. Unkeyed. `Shake*` family, **never** `Hash()`. Default output 32 / 64 bytes. Output 1–1024. OS-gated (`Shake128.IsSupported`). |
| 5 | HMAC keys | UTF-8 string or bytes. **Min 16 bytes.** Generate default **32**. Enum `HmacKeySize`: 16 / 32 / 64 / 128. No silent KDF. `FromBase64` ≠ `FromString`. |
| 6 | MD5 / SHA-1 | Named interop (`HashMd5`, `HashSha1` / enum 10 and 11). **Never default.** |
| 7 | Password | **Argon2id PHC** this milestone. `$argon2id$v=19$m=19456,t=2,p=1$…`. Not Encryption’s content-key KDF (64 MiB / t=3 / p=1). |
| 8 | Password length | 8–128 characters. Unique 16-byte salt every hash. Verify uses **stored** m,t,p. Cap on verify: m ≤ 64 MiB, t ≤ 10, p ≤ 4. |
| 9 | Text default | **Lowercase hex** (FIPS / sha256sum). Caller may request HexUpper, Base64, Base64Url. |
| 10 | Converters | `HashingConvert.HexToBase64` / `Base64ToHex` / URL variants. Password PHC ignores the format enum. |
| 11 | Checksums | **v1.1.** CRC-32 (IEEE / ISO-HDLC, PKZIP, PNG), CRC-64/ECMA-182 (BCL `Crc64`, not XZ), XXH32 / XXH64 / XXH3. `Checksum*` family, **never** `Hash()`. Engine `System.IO.Hashing`, seed 0. Default checksum CRC-32. Catalogue (big-endian) lowercase hex. These do **not** authenticate. |
| 12 | Trailer fill | **Not this milestone.** Encryption already reserves 32-byte `sha256` and `hmacSha256` slots (zeros). A later Encryption revision consumes Hashing output. Hashing does not reference Encryption. |
| 13 | HMAC coverage A/B/C | Later Encryption Seal option (ciphertext frames / header+frames / plaintext). Mode stored in the trailer. Hashing HMAC is always “these bytes + this key.” |
| 14 | Engine | BCL (`SHA256`, `SHA384`, `SHA512`, `SHA3_*`, `HMACSHA256`, `HMACSHA384`, `HMACSHA512`, `HMACSHA3_*`, `Kmac128`, `Kmac256`, `Shake128`, `Shake256`, `IncrementalHash`, `MD5`, `SHA1`). Argon2id: `Konscious.Security.Cryptography.Argon2` (same package Encryption uses). Checksums: `System.IO.Hashing` (`Crc32`, `Crc64`, `XxHash32`, `XxHash64`, `XxHash3`). No home-grown hashes. |
| 15 | Large files | Stream, 64 KiB buffer. Never `File.ReadAllBytes` a capture. |
| 16 | Logging | `HelperLog` APPID `Hashing`. Library never calls `Initialize`. **ALCOA+**: Pending then Success/Failed. Log alg, byte counts, **visible path name**, file digest hex. Never input string, HMAC key, password, salt, PHC — even at Debug. |
| 17 | Strings | UTF-8 no BOM. |
| 18 | Verify | Constant-time (`FixedTimeEquals`). Fail closed: `false`. Caller passes the same format they hashed with. |

---

## 3. Goals

**G1.** One façade (`HashingHelper`) owns Identity, Probe, Hash, HMAC, password, checksum, verify.  
**G2.** Default `HashString` / `HashFile` is SHA-256 lowercase hex.  
**G3.** File APIs stream 64 KiB. Empty files are legal.  
**G4.** HMAC cannot be called without a key.  
**G5.** `HashPassword` never returns SHA-256(password).  
**G6.** Probe is in-memory: SHA-256 of `"abc"` matches FIPS, HMAC generate runs, HMAC-SHA384 RFC 4231 case 1 runs, converters run, CRC-32 of `"123456789"` is `cbf43926`. HMAC-SHA3-256, SHAKE128(`"abc"`), and KMAC128 NIST sample 1 run when the OS supports them. Probe does not HashPassword (cost). Probe does not write Desktop.  
**G7.** Keep `Identity` and `Probe()` so existing smoke tests stay green.  
**G8.** `ChecksumCrc32` / `ChecksumCrc64` / `ChecksumXxHash` (XXH64) never share `Hash()`; checksums are not cryptographic.  
**G9.** Default `HmacString` / `HmacFile` is HMAC-SHA256. HMAC-SHA384 / HMAC-SHA512 / HMAC-SHA3 are opt-in and never the default.  
**G10.** `Kmac*` is not HMAC. `Shake*` is not `Hash()`. Neither family is a default of the other.

---

## 4. Public surface

```csharp
public static class HashingHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Hashing"
    public static string Probe();

    public static string AlgorithmName(HashingAlgorithm algorithm);
    public static int DigestLength(HashingAlgorithm algorithm);
    public static bool IsSupported(HashingAlgorithm algorithm);
    public static bool IsInterop(HashingAlgorithm algorithm);

    public static string HashString(string text, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);
    public static string HashBytes(ReadOnlySpan<byte> data, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);
    public static byte[] HashData(ReadOnlySpan<byte> data, HashingAlgorithm algorithm = Sha256);
    public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, HashingAlgorithm algorithm = Sha256);

    public static string HashFile(string path, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);
    public static string HashFile(Stream stream, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower, string? pathName = null);
    public static Task<string> HashFileAsync(Stream stream, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower, string? pathName = null, CancellationToken ct = default);

    public static string HashMd5(string text, HashingTextFormat format = HexLower);
    public static string HashSha1(string text, HashingTextFormat format = HexLower);

    public static string ChecksumName(ChecksumAlgorithm algorithm);
    public static int ChecksumLength(ChecksumAlgorithm algorithm);
    public static string ChecksumCrc32(string text, HashingTextFormat format = HexLower);
    public static string ChecksumCrc64(string text, HashingTextFormat format = HexLower);
    public static string ChecksumXxHash(string text, HashingTextFormat format = HexLower); // XXH64
    public static string ChecksumString(string text, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower);
    public static string ChecksumBytes(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower);
    public static byte[] ChecksumData(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm = Crc32);
    public static string ChecksumFile(string path, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower);
    public static string ChecksumFile(Stream stream, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower, string? pathName = null);
    public static Task<string> ChecksumFileAsync(Stream stream, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower, string? pathName = null, CancellationToken ct = default);
    public static bool VerifyChecksumString(string text, string expected, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower);
    public static bool VerifyChecksumFile(string path, string expected, ChecksumAlgorithm algorithm = Crc32, HashingTextFormat format = HexLower);

    public static bool VerifyString(string text, string expected, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);
    public static bool VerifyBytes(ReadOnlySpan<byte> data, string expected, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);
    public static bool VerifyFile(string path, string expected, HashingAlgorithm algorithm = Sha256, HashingTextFormat format = HexLower);

    public static string HmacName(HmacAlgorithm algorithm);
    public static int HmacLength(HmacAlgorithm algorithm);
    public static bool IsHmacSupported(HmacAlgorithm algorithm);
    public static string HmacString(string text, HmacKey key, HashingTextFormat format = HexLower);
    public static string HmacString(string text, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HexLower);
    public static string HmacBytes(ReadOnlySpan<byte> data, HmacKey key, HashingTextFormat format = HexLower);
    public static string HmacBytes(ReadOnlySpan<byte> data, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HexLower);
    public static byte[] HmacData(ReadOnlySpan<byte> data, HmacKey key, HmacAlgorithm algorithm = Sha256);
    public static string HmacFile(string path, HmacKey key, HashingTextFormat format = HexLower);
    public static string HmacFile(string path, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HexLower);
    public static string HmacFile(Stream stream, HmacKey key, HashingTextFormat format = HexLower, string? pathName = null);
    public static string HmacFile(Stream stream, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HexLower, string? pathName = null);
    public static bool VerifyHmacString(string text, HmacKey key, string expected, HashingTextFormat format = HexLower);
    public static bool VerifyHmacString(string text, HmacKey key, string expected, HmacAlgorithm algorithm, HashingTextFormat format = HexLower);
    public static bool VerifyHmacFile(string path, HmacKey key, string expected, HashingTextFormat format = HexLower);
    public static bool VerifyHmacFile(string path, HmacKey key, string expected, HmacAlgorithm algorithm, HashingTextFormat format = HexLower);

    public static bool IsKmacSupported { get; }
    public static string KmacName(KmacAlgorithm algorithm);
    public static int KmacDefaultLength(KmacAlgorithm algorithm);
    public static string Kmac128(string text, HmacKey key, HashingTextFormat format = HexLower);
    public static string Kmac256(string text, HmacKey key, HashingTextFormat format = HexLower);
    public static string KmacString(string text, HmacKey key, HashingTextFormat format = HexLower);
    public static string KmacString(string text, HmacKey key, KmacAlgorithm algorithm, HashingTextFormat format = HexLower);
    public static string KmacString(string text, HmacKey key, KmacAlgorithm algorithm, int outputLength, HashingTextFormat format = HexLower, ReadOnlySpan<byte> customization = default);
    public static string KmacBytes(ReadOnlySpan<byte> data, HmacKey key, HashingTextFormat format = HexLower);
    public static byte[] KmacData(ReadOnlySpan<byte> data, HmacKey key, KmacAlgorithm algorithm = Kmac128, int outputLength = 0, ReadOnlySpan<byte> customization = default);
    public static string KmacFile(string path, HmacKey key, KmacAlgorithm algorithm, int outputLength, HashingTextFormat format = HexLower, ReadOnlySpan<byte> customization = default);
    public static bool VerifyKmacString(string text, HmacKey key, string expected, KmacAlgorithm algorithm = Kmac128, HashingTextFormat format = HexLower, int outputLength = 0, ReadOnlySpan<byte> customization = default);

    public static bool IsShakeSupported { get; }
    public static string ShakeName(ShakeAlgorithm algorithm);
    public static int ShakeDefaultLength(ShakeAlgorithm algorithm);
    public static string Shake128(string text, HashingTextFormat format = HexLower);
    public static string Shake256(string text, HashingTextFormat format = HexLower);
    public static string ShakeString(string text, ShakeAlgorithm algorithm, int outputLength, HashingTextFormat format = HexLower);
    public static byte[] ShakeData(ReadOnlySpan<byte> data, ShakeAlgorithm algorithm = Shake128, int outputLength = 0);
    public static string ShakeFile(string path, ShakeAlgorithm algorithm, int outputLength, HashingTextFormat format = HexLower);
    public static bool VerifyShakeString(string text, string expected, ShakeAlgorithm algorithm = Shake128, HashingTextFormat format = HexLower, int outputLength = 0);

    public static string HashPassword(string password);          // PHC
    public static bool VerifyPassword(string password, string stored);
}

public static class HashingConvert
{
    public static string Format(ReadOnlySpan<byte> data, HashingTextFormat format = HexLower);
    public static byte[] Parse(string text, HashingTextFormat format);
    public static string HexToBase64(string hex);
    public static string HexToBase64Url(string hex);
    public static string Base64ToHex(string base64);             // lowercase hex
    public static string Base64UrlToHex(string base64Url);
}

public sealed class HmacKey : IDisposable
{
    public static HmacKey Generate(HmacKeySize size = Bytes32);
    public static HmacKey FromString(string utf8, HmacKeySize? required = null);
    public static HmacKey FromBase64(string base64, HmacKeySize? required = null);
    public static HmacKey FromBytes(ReadOnlySpan<byte> bytes, HmacKeySize? required = null);
    public int Length { get; }
    public string ToHexLower();
    public string ToBase64();
}
```

---

## 5. Algorithms

| Enum | Output | Default? | Notes |
|---|---|---|---|
| Sha256 | 32 | **yes** | Trailer slot shape |
| Sha384 | 48 | no | |
| Sha512 | 64 | no | |
| Sha3_256 / 384 / 512 | 32 / 48 / 64 | no | Fail with `NotSupportedException` if OS lacks SHA-3 |
| Md5 | 16 | interop | `HashMd5` |
| Sha1 | 20 | interop | `HashSha1` |

HMAC-SHA256 digest is always 32 bytes. HMAC-SHA384 is 48. HMAC-SHA512 is 64. HMAC-SHA3-256 / 384 / 512 match those lengths. Default `HmacString` stays HMAC-SHA256.

```csharp
public enum HmacAlgorithm { Sha256 = 1, Sha384 = 2, Sha512 = 3, Sha3_256 = 4, Sha3_384 = 5, Sha3_512 = 6 }
public enum KmacAlgorithm { Kmac128 = 1, Kmac256 = 2 }
public enum ShakeAlgorithm { Shake128 = 1, Shake256 = 2 }
```

KMAC (v1.3) is NIST SP 800-185. Default tag 32 (KMAC128) / 64 (KMAC256). Same `HmacKey`. Optional customization (S). Not HMAC.

SHAKE (v1.3) is FIPS 202 XOF. Default 32 (SHAKE128) / 64 (SHAKE256) bytes. Variable output 1–1024. Not a digest family of `Hash()`.

Checksums (v1.1):

| Enum | Output | Named method | Notes |
|---|---|---|---|
| Crc32 | 4 | `ChecksumCrc32` | IEEE / ISO-HDLC. Catalogue hex `cbf43926` for `"123456789"`. |
| Crc64 | 8 | `ChecksumCrc64` | ECMA-182, not XZ. Catalogue hex `6c40df5f0b497347`. |
| XxHash32 | 4 | `ChecksumString(..., XxHash32)` | Seed 0. |
| XxHash64 | 8 | `ChecksumXxHash` | Seed 0. The “xxHash” named method. |
| XxHash3 | 8 | `ChecksumString(..., XxHash3)` | 64-bit XXH3, seed 0. |

---

## 6. Password verifiers vs Encryption KDF

| | Encryption Argon2id | Hashing `HashPassword` |
|---|---|---|
| Output | 32-byte content key | PHC string |
| Default cost | 64 MiB / t=3 / p=1 | **19 MiB / t=2 / p=1** (OWASP interactive) |
| Opens `.argon`? | Yes | Never |
| Package | Konscious Argon2 | Same package, different API |

`HashString(password)` is still a legal digest of a string. It is **not** the password API. Do not document SHA-256 as credential storage.

---

## 7. Logging (ALCOA+)

- APPID `Hashing`. Category Helpers.
- File Hash / HMAC / Checksum: log alg, visible file name, byte count, **digest hex** (the audit record).
- String Hash / HMAC / Checksum: log alg and byte count. **Not** the digest (it might be a hashed secret). **Not** the input.
- Password: `"password hashed"` / `"password verify ok|failed"`. Never PHC, never salt, never password.
- HMAC generate: log `keyBytes=32`. Never the key hex/Base64.
- KMAC: same key rule. Log alg, byte count, output length. Never customization-as-secret, never key.
- SHAKE: log alg, byte count, output length. File SHAKE logs digest hex.
- `HashingLog.Safe` redacts `$argon2`, PEM, `password=`, `hmac-key=`, `salt=`.
- Do **not** treat 64-char hex as a secret — that is a SHA-256 digest we may need to log for files.

---

## 8. Demo

`Vestigium.Helpers.Hashing.Demo` is a WPF gallery (`HelperWpfHost.Start` APPID `Hashing`).

Tabs: Overview, SHA-256, SHA-384/512, SHA-3, Checksum, HMAC (SHA-256 / 384 / 512 / SHA-3), KMAC / SHAKE, Password, MD5/SHA-1 (warning), Convert, JSONL.

String + real file. Convert tab is hex ↔ Base64. Password tab uses a throwaway (`gallery-demo-only`) and must not log it.

JSONL: `%ProgramData%\Vestigium\Logs\Hashing\`

---

## 9. Tests

- Identity is `Vestigium.Helpers.Hashing`.
- Probe writes Pending then Success. FIPS `"abc"` SHA-256.
- SHA-256 empty + `"abc"`; SHA-384 / SHA-512 `"abc"` FIPS.
- Default print is lowercase hex. Hex↔Base64 round-trip of the `"abc"` vector.
- SHA3-256 `"abc"` when `IsSupported`; skip if not.
- MD5 / SHA-1 named interop vectors. `IsInterop` true only for those two.
- File > 64 KiB matches `SHA256.HashData` of the same bytes.
- HMAC RFC 4231 case 1 (20-byte `0x0b` key, `"Hi There"`) for SHA-256, SHA-384, and SHA-512.
- HMAC-SHA384 / HMAC-SHA512 / HMAC-SHA3 are not the default of `HmacString`.
- HMAC-SHA3-256 / 384 / 512 of the same RFC 4231 case 1 key and message when `IsHmacSupported`; skip if not.
- File HMAC > 64 KiB matches in-memory `HmacBytes` for SHA-256 / 384 / 512 and HMAC-SHA3 when supported.
- HMAC key < 16 bytes throws. Generate 16/32/64/128.
- `FromBase64` of a generated key ≠ `FromString` of the same Base64 letters.
- SHAKE128(`"abc"`) 32-byte FIPS 202 vector; SHAKE256(`"abc"`) 64-byte vector when supported. SHAKE is not `HashString`.
- KMAC128 NIST SP 800-185 sample 1 (`K=0x40..0x5f`, `X=00010203`, L=256, S empty) when supported. KMAC256 sample 1. Customization `"My Tagged Application"` sample. KMAC is not HMAC.
- File KMAC / SHAKE > 64 KiB matches in-memory bytes when supported.
- Argon2id PHC prefix `m=19456,t=2,p=1`. Verify true/false. Short password throws.
- CRC-32 empty = `00000000`; `"123456789"` = `cbf43926`. CRC-64 `"123456789"` = `6c40df5f0b497347`.
- XXH32 / XXH64 / XXH3 `"abc"` published vectors. `ChecksumXxHash` is XXH64.
- File checksum > 64 KiB matches in-memory `ChecksumBytes` of the same payload.
- `ChecksumCrc32` is not `HashString` of the same input.
- JSONL after HashPassword / HMAC does not contain the password, PHC, or key hex.
- Tests never use the real Desktop or live ProgramData.

---

## 10. Non-goals

| Item | Why |
|---|---|
| Encryption / Seal / Open | Sibling Encryption. |
| Filling trailer slots | Encryption v1.3. Hashing only *produces* 32-byte digests. |
| HMAC-SHA3 / KMAC / SHAKE | Shipped v1.3. |
| BLAKE3 | Not BCL. |
| Password pepper | Hosts would store it next to the PHC. |
| String-as-AES-key | Encryption. |
| `ReadAllBytes` on a capture | Suite-wide. |
| `ObjectPool<SHA256>` | BCL `HashData` / `IncrementalHash` already use hardware. |

---

## 11. Sibling Encryption

`Vestigium.Helpers.Encryption` owns Seal/Open. Hashing must not reference Encryption. Encryption must not grow `Hash*` or `Hmac*` public APIs.

Trailer today:

| Slot | Owner | v1.0 | Later |
|---|---|---|---|
| structural `mac` | Encryption | Written | Stays |
| `sha256` (32) | Hashing-shaped | zeros, flag bit 3 clear | Plaintext SHA-256, bit 3 set. Encryption writes it. |
| `hmacSha256` (32) | Hashing HMAC | zeros, flag bit 4 clear | Caller MAC key + coverage A/B/C stored. Encryption writes it. |

There is **no** `Vestigium.Helpers.Hmac` project.

---

## 12. Roadmap

| Version | Work |
|---|---|
| v1.0 | SHA-2, SHA-3, HMAC-SHA256, Argon2id PHC, converters, gallery |
| v1.1 | `ChecksumCrc32` / `ChecksumCrc64` / `ChecksumXxHash` (XXH32 / XXH64 / XXH3) |
| v1.2 | HMAC-SHA384 / HMAC-SHA512 |
| v1.3 (this) | HMAC-SHA3 / KMAC / SHAKE |
| Encryption v1.3 | `embedPlaintextSha256`; `callerMacKey` + `HmacCoverage` A/B/C; `trailerMinor` bump |

---

## 13. Glossary

| Term | Meaning |
|---|---|
| Digest | Unkeyed hash of caller bytes |
| HMAC | Keyed SHA-2 or SHA-3 MAC of caller bytes (SHA-256 default; SHA-384 / SHA-512 / SHA-3 opt-in) |
| KMAC | NIST SP 800-185 keyed SHA-3 MAC. Not HMAC. |
| SHAKE | FIPS 202 extendable-output function. Unkeyed. |
| PHC | Password Hashing Competition string (`$argon2id$…`) |
| Coverage A/B/C | Later Encryption HMAC slot: ciphertext / header+frames / plaintext |
| Interop | MD5 / SHA-1, never default |
