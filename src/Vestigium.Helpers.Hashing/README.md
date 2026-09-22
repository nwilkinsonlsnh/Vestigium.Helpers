# Vestigium.Helpers.Hashing

String and file hashing. Separate from Encryption.

**Version:** 1.4.1  
**License:** MIT

SHA-2, SHA-3, HMAC-SHA2/SHA3, KMAC, SHAKE, Argon2id (PHC), CRC-32/64, xxHash. Hex default. `HashFile` streams — no `ReadAllBytes` on a payload.

1.4.0: fail-closed `VerifyPassword`, named EVENTIDs 13000–13070, no Demo.  
1.4.1: `PackageLicenseExpression` MIT.

A host calls `VestigiumLogger.Initialize` (APPID `Hashing`) and `HashingCatalog.Register`.

```xml
<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.4.1" />
```
