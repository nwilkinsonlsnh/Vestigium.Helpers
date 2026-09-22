# Hashing — PR03 contract tests

**Status:** Done  
**Priority:** P1  
**Depends on:** PR02  
**Publish:** No

`HashingContractTests.cs` is live. `HashingSessionTests` and `HashingBranchTests` stay Compile Remove.

Covered: Identity; SHA-256 empty + FIPS abc; CRC-32 of 123456789 = cbf43926; HMAC RFC 4231 case 1 SHA-256; file >64 KiB matches SHA256.HashData; HashPassword ≠ HashString; VerifyPassword true/false + fail-closed garbage PHC; MD5/SHA-1 named interop only; Hex↔Base64 of abc. SHA-3/KMAC/SHAKE skip if !IsSupported.
