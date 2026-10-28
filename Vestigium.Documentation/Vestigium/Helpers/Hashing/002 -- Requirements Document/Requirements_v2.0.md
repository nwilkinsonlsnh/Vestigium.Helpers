# Vestigium.Helpers.Hashing — Requirements Specification

**Document ID:** VEST-HLP-HASH-SRS-200  
**Version:** 2.0  
**Status:** Accepted. Supersedes SRS v1.3 for new work. v1.3 remains historical in `Archived/PR01/Requirements_v1.0.md`.  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.Hashing` **1.4.0**  
**TFM:** `net10.0`  
**Companions:** [`Design_v2.0.md`](Design_v2.0.md), [`DevelopersGuide_v2.0.md`](DevelopersGuide_v2.0.md)

If implementation and this file disagree, this file wins. SRS v1.3 §2 locks remain in force unless the delta table says otherwise.

This library is not Encryption. It does not invent a hash. It does not ship a Demo project.

## 0. Lossless delta from SRS v1.3

| Topic | v1.3 | v2.0 |
|---|---|---|
| VerifyPassword on bad PHC | `FormatException` | **Fail-closed `false`** + EVENTID 13020. m/t/p over cap still `CryptographicException`. |
| HmacKey | Dispose + ZeroMemory | Finalizer calls Dispose. `ToString()` is length only, never hex/Base64. |
| EVENTIDs | Five rows 13000–13020 | Named 13000–13070. HashString never logs digest hex. HashFile may. HMAC logs `keyBytes=` never key material. |
| HelperCompat | Separate file | Deleted. `HashingLog.RequireNotBlank` (13070). |
| Demo | SRS §8 gallery | **Not shipped.** |
| Tests | Session file Compile Remove | `HashingContractTests`, `HashingLoggingTests`, `HashingPR01Tests` live. Session/branch stay removed. |
| Version | 1.3.0 advertised | Library **1.4.0**. nuget.org push is a separate publish step. |
| Logging package | Vestigium.Logging via props | Unchanged: **1.7.1** via `Directory.Build.props`. Library never `Initialize`. |

## 1. Decisions locked

1. Default digest is SHA-256. Default print is **lowercase hex**.
2. Default HMAC is HMAC-SHA256. SHA-384 / SHA-512 / HMAC-SHA3 are opt-in. HMAC-SHA3 is OS-gated.
3. HMAC key minimum **16** bytes. Generate default **32**. Enum 16 / 32 / 64 / 128. `FromString` is UTF-8. `FromBase64` decodes. Do not mix them.
4. `HmacKey.ToString()` is `HmacKey(N bytes)` or `HmacKey(disposed)`. Export is `ToHexLower()` / `ToBase64()` only.
5. `HmacKey.Dispose` zeros bytes. A finalizer is last-chance ZeroMemory. Explicit Dispose stays.
6. MD5 and SHA-1 are named interop (`HashMd5`, `HashSha1`, enum 10/11). Never default. `IsInterop` is true only for those two.
7. Checksums are `Checksum*`, never `Hash()`. CRC-32 IEEE, CRC-64/ECMA-182, xxHash seed 0.
8. KMAC is NIST SP 800-185 (`Kmac*`), not HMAC. SHAKE is FIPS 202 XOF (`Shake*`), never `Hash()`.
9. `HashPassword` is Argon2id PHC 19 MiB / t=2 / p=1. Encryption KDF stays 64 MiB / t=3 / p=1 and still produces a content key.
10. `VerifyPassword` returns `false` on empty or unparseable PHC and logs Failed. Parameter bombs throw `CryptographicException`.
11. File APIs stream with a **64 KiB** buffer. Never `ReadAllBytes` / `ReadAllText` on a payload.
12. Compare with `CryptographicOperations.FixedTimeEquals`.
13. Library never calls `VestigiumLogger.Initialize`. APPID `Hashing`. Host registers `HashingCatalog`.
14. Never log input strings, HMAC keys, passwords, salts, or PHC — even at Debug. File digest hex is the audit record. String/bytes hash Success does **not** include digest hex.
15. `HashingLog.Safe` redacts `$argon2`, PEM, `password=`, `hmac-key=`, `salt=`. Do not treat 64-char hex as a secret.
16. No Demo project this revision.
17. Do not fill Encryption trailer slots from this project. No Encryption reference.
18. Package version for this contract is **1.4.0**. FileIo may `PackageReference` that version only after nuget.org returns 200.

## 2. Goals

G1 String, bytes, and file digests with SHA-256 default.  
G2 HMAC with a single `HmacKey` type.  
G3 Argon2id PHC password verifiers, fail-closed verify.  
G4 Fast checksums for catalogues, not authentication.  
G5 KMAC and SHAKE when the OS supports them.  
G6 ALCOA+ JSONL through `HashingLog`.  
G7 Named EVENTIDs 13000–13070.  
G8 Contract tests without a Demo host.

## 3. Algorithms (unchanged from v1.3 except verify)

Unkeyed: SHA-256 default, SHA-384, SHA-512, SHA3-256/384/512 (OS-gated), MD5/SHA-1 interop.

Keyed HMAC: SHA-256 default, SHA-384 (48), SHA-512 (64), SHA3-256/384/512 OS-gated.

KMAC128 default tag 32, KMAC256 default tag 64. SHAKE128 default 32, SHAKE256 default 64, output 1–1024.

Checksum: CRC-32, CRC-64, XXH32, XXH64, XXH3.

Password: Argon2id PHC string. Caps on verify: memory, iterations, parallelism. Over cap throws.

## 4. JSONL EVENTIDs

13000 ProbeEnter, 13005 ProbeComplete, 13010 OperationEnter, 13015 OperationComplete, 13020 OperationFailed, 13025 HashComplete (no digest), 13030 HashFileComplete (digest hex ok), 13035 HmacComplete, 13040 KmacComplete, 13045 ShakeComplete, 13050 ChecksumComplete, 13055 PasswordHashed, 13060 PasswordVerify, 13065 ConvertComplete, 13070 Rejected.

Twins: `HashingEvents` + `HashingCatalog.Rows` + `EventCatalog/hashing.json`.

HMAC detail includes `keyBytes=N`. Never `ToHexLower()` of the key. Failed never attaches `Exception.ToString()`.

## 5. Public surface

`HashingHelper`: Identity, Probe, AlgorithmName / DigestLength / IsSupported / IsInterop, HashString / HashBytes / HashData / TryHash / HashFile / HashFileAsync, HashMd5 / HashSha1, VerifyString / VerifyBytes / VerifyFile, HMAC / KMAC / SHAKE families, HashPassword / VerifyPassword, Checksum* family.

`HmacKey`: Generate, FromString, FromBase64, FromBytes, Length, ToHexLower, ToBase64, Dispose, ToString.

`HashingConvert`: Format / Parse, Hex↔Base64 / Base64Url.

`HashingCatalog.AppId` + `Register`. `HashingEvents` 13000–13070.

## 6. Tests

TEMP / in-memory only. Live fixtures: `HashingContractTests`, `HashingLoggingTests`, `HashingPR01Tests`.

Must include: Identity; SHA-256 empty + FIPS `abc`; CRC-32 of `123456789` = `cbf43926`; HMAC RFC 4231 case 1 SHA-256; file >64 KiB matches `SHA256.HashData`; HashPassword ≠ HashString; VerifyPassword true / wrong / garbage PHC `false`; cap bomb throws; MD5/SHA-1 named interop only; Hex↔Base64 of `abc`; HashString JSONL 13025 without digest; HashFile JSONL 13030 may include digest; HMAC JSONL `keyBytes=` without key hex; `HmacKey.ToString` is not hex; SHA-3/KMAC/SHAKE skip if `!IsSupported`; Probe without a host does not throw.

`HashingSessionTests` and `HashingBranchTests` stay `<Compile Remove>`.

## 7. Demo

Not required. Not present in `Vestigium.Helpers.slnx`.

## 8. Non-goals

AES. SHA-256-as-password. Blake3 this wave. A separate Hmac csproj. `ReadAllBytes` on a capture. Logging keys or PHC. A Demo project. Filling Encryption trailer slots from this assembly.

## 9. Siblings and publish

`Vestigium.Logging` 1.7.1 package (Directory.Build.props). No Encryption / FileIo / Analytics reference from Hashing.

FileIo unique-content and `CompareFiles` consume Hashing. FileIo may switch to `<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.4.0" />` only after https://www.nuget.org/packages/Vestigium.Helpers.Hashing/1.4.0 is 200.

## 10. Acceptance

This file + Design v2.0 + Developers Guide v2.0 on main. §1 locks match code. §6 tests green. Version 1.4.0 on the csproj. No nuget push from the documentation change itself.
