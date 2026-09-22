# Vestigium.Helpers.FileIo — Implementation Plan v1.2

**Document ID:** VEST-HLP-FILEIO-PLAN-012  
**Version:** 1.2  
**Status:** Closed in-repo. nuget.org FileIo 1.1.0 is held until Hashing is published.  
**Date:** 20 September 2026  
**Package:** `Vestigium.Helpers.FileIo` 1.1.0

PR01–PR06 landed. Next operator step is not more FileIo code.

## Publish sequence

1. Pack and push `Vestigium.Helpers.Hashing` **1.3.0** (csproj is pack-ready: README + URLs + `hashing.json`).
2. On FileIo, replace the Hashing project reference with:
   `<PackageReference Include="Vestigium.Helpers.Hashing" Version="1.3.0" />`
3. `dotnet pack src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj -c Release`
4. Confirm nupkg: Analytics 1.0.1, Logging 1.7.1, Hashing 1.3.0, `contentFiles/.../EventCatalog/fileio.json`.
5. Push FileIo 1.1.0.

Do not `nuget push` FileIo while Hashing is still a `<ProjectReference>`.
