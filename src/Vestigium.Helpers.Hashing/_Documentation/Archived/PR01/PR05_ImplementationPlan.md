# Hashing — PR05 version and local pack

**Status:** Done  
**Priority:** P2  
**Depends on:** PR01–PR04  
**Publish:** No. Local pack only.

## Why a bump

PR01 changes VerifyPassword (throw → false). PR02 adds EVENTIDs. Shipped as **1.4.0**.

## Steps

| Step | Work | Status |
|---|---|---|
| PR05.001 | Set `<Version>` to **1.4.0**. Update package README. | **Done** |
| PR05.002 | `dotnet pack -c Release`. Inspect nupkg: hashing.json, Argon2, System.IO.Hashing. | Open — run locally |
| PR05.003 | Do **not** `nuget push`. | **Done** |

```text
dotnet pack src/Vestigium.Helpers.Hashing/Vestigium.Helpers.Hashing.csproj -c Release
```

Inspect `bin/Release/Vestigium.Helpers.Hashing.1.4.0.nupkg` for `contentFiles/any/any/EventCatalog/hashing.json` and package dependencies `Konscious.Security.Cryptography.Argon2` / `System.IO.Hashing`.

Commit: `Hashing PR05: version 1.4.0 pack-ready`
