# Vestigium.Helpers.Hashing — Implementation Plan v1.4

**Document ID:** VEST-HLP-HASH-PLAN-014  
**Version:** 1.4.1  
**Status:** Active  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.Hashing` (still 1.3.0 on disk until PR05)  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS v1.3)

If this file and the SRS disagree, the SRS wins.

**Publish is the last line.** Fix verify, catalog, tests, and docs first. `dotnet nuget push` is PR06 only.

v1.3 shipped the engine. v1.4 does not invent a hash. No AES. No SHA-256-as-password. No Blake3 this wave.

## Baseline

| Item | Actual |
|---|---|
| Version on disk | 1.3.0, pack metadata already on the csproj |
| nuget.org | **404** — leave it that way until PR06 |
| EVENTIDs | Block 13000–13499, only five rows today |
| Tests live | `HashingLoggingTests`, `HashingBranchTests` |
| Tests removed | `HashingSessionTests` is `<Compile Remove>` |
| Demo | Docs claim `Hashing.Demo`. No project in the slnx |

SRS §2 stays locked: SHA-256 default, HMAC-SHA256 default, key min 16 / generate 32, MD5/SHA-1 named interop only, Argon2id PHC 19 MiB/t=2/p=1, checksums never `Hash()`, KMAC ≠ HMAC, SHAKE ≠ `Hash()`, 64 KiB streams, never log input/key/password/salt/PHC, `FixedTimeEquals`.

## Findings

**P0 security.** Lock 18 says verify fail-closed `false`; `VerifyPassword` throws on a bad PHC. `HmacKey` has no finalizer. HMAC JSONL must show `keyBytes=` and never `ToHexLower()`.

**P1 logging.** Five EVENTIDs is too coarse. String hash must not log digest; file hash may.

**P1 tests.** SRS §9 vectors are in the removed session file.

**P1 docs.** Demo in the README, not in the slnx.

**P0 pack — last.** After the fixes, bump the version (PR05), then push (PR06). FileIo may PackageReference only after the package page is 200.

## PR sequence

| PR | Priority | Goal | Publish? |
|---|---|---|---|
| **PR01** | P0 | Fail-closed verify + key hygiene | No |
| **PR02** | P1 | Named EVENTIDs; drop HelperCompat | No |
| **PR03** | P1 | Contract tests | No |
| **PR04** | P2 | Docs match slnx | No |
| **PR05** | P2 | Version + local `dotnet pack` inspection | No push |
| **PR06** | P0 | **nuget.org push.** Then FileIo PackageReference | **Yes** |

One PR at a time. Commit form: `Hashing PR0N: <short goal>`.

## Commands (every close gate except PR06 push)

```text
dotnet build src/Vestigium.Helpers.Hashing/Vestigium.Helpers.Hashing.csproj
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Hashing
```

PR06 adds pack + push + FileIo restore.
