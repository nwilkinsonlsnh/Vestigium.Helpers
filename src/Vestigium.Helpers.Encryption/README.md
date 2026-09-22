# Vestigium.Helpers.Encryption

Authenticated encryption for strings and files. AES-256-GCM default, ChaCha20-Poly1305 opt-in, AES-256-CBC+HMAC interop, Argon2id passphrases, RSA-OAEP wrap of the content key. Hashing stays a sibling.

This is not TLS, BitLocker, DPAPI, or a key vault. Wrong secret, wrong wrap key, or a bit flip fails closed: `The envelope is corrupt.`

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Encryption` 1.3.0 |
| TFM | `net10.0` |
| APPID | `Encryption` (`EncryptionCatalog.AppId`) |
| EVENTID | Reserved 12000–12499 (used through 12030) |
| Depends on | `Konscious.Security.Cryptography.Argon2` 1.3.1, `Vestigium.Logging` |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Encryption" Version="1.3.0" />
```

```csharp
using Vestigium.Helpers.Encryption;

using var secret = EncryptionSecret.FromPassphrase(passphrase);
var sealedText = EncryptionHelper.SealString(token, secret);
var tokenBack  = EncryptionHelper.OpenString(sealedText, secret);

var dest = EncryptionHelper.SealFile(capturePath, exportDir, secret); // nathan.argon
EncryptionHelper.OpenFile(dest, exportDir, secret);                   // nathan.txt
```

Raw 32-byte key: `EncryptionSecret.FromKey(key32)` writes `.aes` and skips Argon2.

## Surface

| Call | Returns | Notes |
|---|---|---|
| `SealString` / `OpenString` | string | Same envelope as files, Base64. |
| `SealFile` / `OpenFile` | path | 64 KiB frames. Never `ReadAllBytes` the payload. |
| `IsVestigiumFile` / `PeekFile` / `ValidateFile` | bool / info / result | Peek needs no secret. |
| `RevealOriginalFileName` | string | Hidden name lives in the trailer. |
| `EncryptionSecret.FromPassphrase` / `FromKey` | secret | Dispose clears. |
| `EncryptionRsaKey.Generate` | key | Wraps the 32-byte content key only. Default 3072 bits. |
| `EncryptionKeyRing` | ring | Pairs vs contacts. Enable / Disable / Expire / Retire / Compromise. |
| `EncryptionCatalog.Register(cfg)` | void | Host-only, during `VestigiumLogger.Initialize`. |

Visible names: `{stem}.aes` (raw key / GCM / RSA-only wrap) or `{stem}.argon` (passphrase). Open restores the hidden original name.

## Rules that do not move

- No custom primitives. BCL AEAD / RSA-OAEP. One approved Argon2 package.
- Never CBC without HMAC. Never RSA on the file body.
- Fail closed. Do not distinguish wrong password from corrupt from wrong company key.
- Never log plaintext, keys, passphrases, PKCS8, issuedTo, or full thumbprints.
- Large files stream. Do not encrypt in place.
- Hashing / HMAC digests belong in `Vestigium.Helpers.Hashing`. Trailer slots stay zeros here.
- The library never calls `VestigiumLogger.Initialize`.

## Logging

```csharp
VestigiumLogger.Initialize(cfg =>
{
    cfg.AppId = "PingIQ";                       // host APPID, not Encryption
    cfg.LogDirectory = logDir;
    EncryptionCatalog.Register(cfg);
});
```

Writes are no-ops until the host initializes. JSONL lands at `%ProgramData%\Vestigium\Logs\{host-APPID}\`.

Named events live in `EventCatalog/encryption.json`.

## Related

Digests: `Vestigium.Helpers.Hashing`.

Long-form documents live in [Vestigium.Documentation / Helpers / Encryption](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption). Folders, not files — current revision sits inside the folder.

| Area | GitHub |
|---|---|
| 000 -- Archived | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/000%20--%20Archived) |
| 001 -- Implementation Plan | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/001%20--%20Implementation%20Plan) |
| 002 -- Requirements Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/002%20--%20Requirements%20Document) |
| 003 -- Design Document | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/003%20--%20Design%20Document) |
| 004 -- Developers Guide | [folder](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Encryption/004%20--Developers%20Guide) |
