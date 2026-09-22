# Hashing — PR06 publish (last)

**Status:** Blocked on nuget.org API key  
**Priority:** P0  
**Depends on:** PR05 close gate  
**Publish:** Yes. This is the only PR that pushes.

Hashing **1.4.0 is pack-ready on `main`**. https://www.nuget.org/packages/Vestigium.Helpers.Hashing is still **404**. FileIo still has a **ProjectReference** on purpose — do not swap until the package page is 200.

## Steps

| Step | Work | Status |
|---|---|---|
| PR06.001 | `dotnet nuget push` Hashing **1.4.0**. Needs your API key. | Blocked |
| PR06.002 | Confirm https://www.nuget.org/packages/Vestigium.Helpers.Hashing/1.4.0 is 200. | Blocked |
| PR06.003 | FileIo.csproj: `<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.4.0" />`. Drop the project reference. | Waiting 06.002 |
| PR06.004 | `dotnet test --filter FileIo` and `--filter Hashing`. Pack FileIo 1.1.0. Nuspec lists Hashing as a **package**. Then push FileIo if you want that feed live. | Waiting |

## Commands (your machine, your key)

```text
dotnet pack src/Vestigium.Helpers.Hashing/Vestigium.Helpers.Hashing.csproj -c Release

dotnet nuget push src/Vestigium.Helpers.Hashing/bin/Release/Vestigium.Helpers.Hashing.1.4.0.nupkg ^
  --api-key %NUGET_API_KEY% ^
  --source https://api.nuget.org/v3/index.json
```

After the package page is 200, say **PR06.003** and FileIo drops the project reference.

Do not push FileIo while Hashing is still a ProjectReference.
