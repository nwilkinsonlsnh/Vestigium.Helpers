# Vestigium.Helpers.Hashing — Developers Guide

**Document ID:** VEST-HLP-HASH-DEV-200  
**Version:** 2.0  
**Status:** Accepted  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.Hashing` **1.4.0**  
**Contract:** [`Requirements_v2.0.md`](Requirements_v2.0.md)

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.Hashing/`. There is no Demo project.

## Host init

The library never calls `Initialize`.

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = HashingCatalog.AppId; // "Hashing"
    HashingCatalog.Register(cfg);
});
```

JSONL: `%ProgramData%\Vestigium\Logs\Hashing\`

Probe and hash APIs no-op the log when the host has not initialized. They must not throw for that reason.

## Digest

```csharp
var hex = HashingHelper.HashString("abc");          // SHA-256 lowercase hex
var file = HashingHelper.HashFile(path);            // streams, 64 KiB
Assert.True(HashingHelper.VerifyFile(path, file));
```

Default algorithm is SHA-256. Pass `HashingAlgorithm.Sha384` / `Sha512` / `Sha3_*` when you mean it. Call `IsSupported` before SHA-3.

MD5 / SHA-1 are `HashMd5` / `HashSha1` only.

## HMAC key

```csharp
using var key = HmacKey.Generate();                 // 32 random bytes
var b64 = key.ToBase64();
HashingHelper.HmacString(msg, HmacKey.FromBase64(b64));     // RIGHT
HashingHelper.HmacString(msg, HmacKey.FromString(b64));     // WRONG: UTF-8 of the letters
HashingHelper.HmacString(msg, HmacKey.FromString("ops-shared-mac-key")); // typed secret ≥ 16 UTF-8 bytes
key.ToString();                                     // "HmacKey(32 bytes)" — not hex
```

Same `HmacKey` object for HMAC-SHA-2, HMAC-SHA-3, and KMAC.

## Password

```csharp
var phc = HashingHelper.HashPassword("gallery-demo-only");
Assert.True(HashingHelper.VerifyPassword("gallery-demo-only", phc));
Assert.False(HashingHelper.VerifyPassword("gallery-demo-only", "not-a-phc")); // no throw
```

PHC is not an Encryption content key. Do not Open an envelope with it.

## Checksum vs hash

```csharp
HashingHelper.ChecksumCrc32("123456789"); // cbf43926
```

Do not authenticate a capture with CRC or xxHash.

## Convert

```csharp
var hex = HashingHelper.HashString("abc");
var b64 = HashingConvert.HexToBase64(hex);
```

Default print stays hex. Base64 is an argument / converter.

## Tests

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Hashing
```

## Package

```xml
<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.4.0" />
```

Use that PackageReference only after nuget.org serves 1.4.0. Until then FileIo keeps the project reference.

## Never

Do not `dotnet run` a Hashing.Demo — it is not in the slnx.  
Do not log `key.ToHexLower()`.  
Do not treat a SHA-256 hex string as a password verifier.
