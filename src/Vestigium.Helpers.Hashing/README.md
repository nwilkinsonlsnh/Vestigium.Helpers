# Vestigium.Helpers.Hashing

String and file hashing. Separate from Encryption. SHA-256 default. Hex lowercase default. `HashFile` streams — no `ReadAllBytes` on a payload.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Hashing` 1.4.1 |
| TFM | `net10.0` |
| APPID | `Hashing` (`HashingCatalog.AppId`) |
| EVENTID | Reserved 13000–13499 (used through 13070) |
| Depends on | `Konscious.Security.Cryptography.Argon2` 1.3.1, `System.IO.Hashing` 10.0.0, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.4.1" />
```

```csharp
using Vestigium.Helpers.Hashing;

var digest = HashingHelper.HashString("abc");          // SHA-256 hex
var file   = HashingHelper.HashFile(path);

using var key = HmacKey.Generate();
var mac = HashingHelper.HmacString(message, key);

var phc = HashingHelper.HashPassword(passphrase);       // Argon2id PHC
var ok  = HashingHelper.VerifyPassword(passphrase, phc); // fail-closed
```

## Surface

| Call | Returns | Notes |
|---|---|---|
| `HashString` / `HashBytes` / `HashFile` | digest text | Default SHA-256. MD5 / SHA-1 are interop only. |
| `HmacString` / `HmacBytes` / `HmacFile` | mac text | Key is `HmacKey`. Dispose clears. |
| `KmacBytes` | mac text | Needs runtime KMAC support. |
| `Shake128` / `Shake256` | hex | Needs runtime SHAKE support. |
| `ChecksumCrc32` / CRC-64 / xxHash | hex | Not a cryptographic digest. |
| `HashPassword` / `VerifyPassword` | PHC / bool | Argon2id. Verify fails closed. |
| `HashingConvert.HexToBase64` | string | Format helpers. |
| `HashingCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

## Rules that do not move

- Not encryption. Content keys and envelopes live in `Vestigium.Helpers.Encryption`.
- No `ReadAllBytes` on a file payload. 64 KiB stream buffer.
- Never log the input, the key, or the passphrase.
- MD5 and SHA-1 are interop labels, not defaults.
- `VerifyPassword` does not distinguish “wrong password” from “malformed PHC.”
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Hashing
    cfg.LogDirectory = logDir;
    HashingCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/hashing.json`.

## Related

Envelopes: `Vestigium.Helpers.Encryption`. File jobs: `Vestigium.Helpers.FileIo`.

Long-form documents live in [Vestigium.Documentation / Helpers / Hashing](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Hashing/004%20--Developers%20Guide) |
