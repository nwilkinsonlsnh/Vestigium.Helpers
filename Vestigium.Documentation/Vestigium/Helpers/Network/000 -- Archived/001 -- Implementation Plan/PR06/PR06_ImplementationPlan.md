# Vestigium.Helpers.Network — PR06 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR06  
**Version:** 1.0  
**Status:** Open. Publish hygiene. No new protocol surface.  
**Date:** 20 September 2026  
**Package:** first nuget.org push of `Vestigium.Helpers.Network` **1.0.0**  
**Depends on:** PR01–PR05 closed.

This is not Option D routes, not Charts, not Demo, not Ubuntu live checks.

---

## 0. Where the feed is (20 Sep 2026, Zazoo profile)

| Package | Latest on nuget.org | Network needs it? |
|---|---|---|
| `Vestigium.Logging` | **1.7.1** | Yes. Already via `Directory.Build.props`. |
| `Vestigium.Helpers.Analytics` | **1.0.1** | Yes. P95 / share estimator. |
| `Vestigium.Helpers.Hashing` | **1.4.0** | Indirect. FileIo consumes it. |
| `Vestigium.Helpers.Charts` | **1.0.1** | **No.** Do not add. |
| `Vestigium.Helpers.FileIo` | **not on that list** | Yes. Share probes. |
| `Vestigium.Helpers.Json` | **not on that list** | Yes. Campaign recipe / JSONL. |
| `Vestigium.Helpers.Network` | not published | This PR. |

Network's csproj still has **project** references:

```xml
<ProjectReference Include="..\Vestigium.Helpers.Json\Vestigium.Helpers.Json.csproj" />
<ProjectReference Include="..\Vestigium.Helpers.Analytics\Vestigium.Helpers.Analytics.csproj" />
<ProjectReference Include="..\Vestigium.Helpers.FileIo\Vestigium.Helpers.FileIo.csproj" />
```

A nupkg built like that either embeds siblings or lists them as project deps. Consumers on nuget.org cannot restore that. **Do not push Network until Json and FileIo are packages and Network points at those versions.**

Logging 1.7.1 is already the repo-wide package. Analytics 1.0.1 is already a package. Hashing 1.4.0 is already a package. Charts is irrelevant.

---

## 1. Work table

| ID | Item | Type | Owner | Status |
|---|---|---|---|---|
| PR06.000 | Confirm FileIo + Json 200 on nuget.org (publish those first if missing) | Gate | You + sibling plans | Open |
| PR06.001 | Network pack metadata + package README (match Hashing / Analytics) | Update | This package | Open |
| PR06.002 | Replace the three Network `ProjectReference`s with pinned `PackageReference`s | Fix | This package | Open. Blocked on 000. |
| PR06.003 | `dotnet pack` Release. Inspect nuspec. No Charts. No project refs. | Check | This package | Open |
| PR06.004 | `dotnet nuget push` Network 1.0.0. Your API key. | Publish | You | Open. Blocked on 003. |
| PR06.005 | Confirm nuget.org 200. Empty-project restore smoke. | Check | You | Open |
| PR06.006 | Docs: Developers Guide consume snippet. Status index. Close. | Update | This package | Open |

Push commands stay on your machine. This agent does not hold the nuget.org key.

---

## 2. Slice notes

### PR06.000 — Sibling packages first

Do this before any Network pack that a stranger could restore.

| Package | Tree version today | Ready? |
|---|---|---|
| Json | `1.0.0` | Pack metadata is thin (no `PackageReadmeFile` / repo URL). Fix that in the Json project, pack, push, wait for 200. |
| FileIo | `1.1.0` | Already references Analytics **1.0.1** and Hashing **1.4.0** as packages in the copy we last treated as current. Confirm that on `main`, pack, push, wait for 200. FileIo README still has a stale “Hashing is a project reference” line — clean that when FileIo ships. |

Gate:

```text
https://www.nuget.org/packages/Vestigium.Helpers.Json/1.0.0
https://www.nuget.org/packages/Vestigium.Helpers.FileIo/1.1.0
```

both return 200. Until then Network stays on project references in the **solution** so local tests keep working.

### PR06.001 — Pack metadata

Match Hashing / Analytics:

- `PackageReadmeFile`, `PackageProjectUrl`, `RepositoryUrl`
- A short `src/Vestigium.Helpers.Network/README.md` (NuGet card). Not the `_Documentation` set.
- Keep `Version` **1.0.0** for the first public drop.
- Keep `IsPackable=true`, `net10.0`, tags already on the csproj.
- Packed OUI stays an embedded resource. Event catalog stays `contentFiles`.

Do **not** add a Charts PackageReference.

### PR06.002 — Flip references

After 000 is green:

```xml
<PackageReference Include="Vestigium.Helpers.Json" Version="1.0.0" />
<PackageReference Include="Vestigium.Helpers.Analytics" Version="1.0.1" />
<PackageReference Include="Vestigium.Helpers.FileIo" Version="1.1.0" />
```

Logging stays `$(VestigiumLoggingVersion)` from Directory.Build.props (1.7.1).

Tests project may keep project references to Network + siblings. Only the **packable** Network csproj must speak packages.

### PR06.003 — Pack inspect

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network
dotnet pack src/Vestigium.Helpers.Network/Vestigium.Helpers.Network.csproj -c Release
```

Open the nupkg. Nuspec must list Json 1.0.0, Analytics 1.0.1, FileIo 1.1.0, Logging 1.7.1. Must not list Charts. Must not list a sibling `.csproj`.

### PR06.004 / PR06.005 — Push + confirm

Same pattern as Hashing 1.4.0. Key stays with you.

```text
dotnet nuget push .…\Vestigium.Helpers.Network.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key <KEY>
```

Then a throwaway console:

```xml
<PackageReference Include="Vestigium.Helpers.Network" Version="1.0.0" />
```

Restore must pull FileIo / Json / Analytics / Logging / Hashing as packages. Must not pull Charts.

### PR06.006 — Close

Guide + status index get the consume snippet. PR06 marked Closed. Live Ubuntu route checks stay parked (PR05 §3).

---

## 3. What this PR does not do

- Bump Network past 1.0.0.
- Publish FileIo / Json from this document (they have their own projects; 000 only waits on them).
- Add Charts.
- Restore Demo.
- Change Option C / default-route refuse.
- Un-waive repo Linux CI.

---

## 4. Commit form

```text
Network PR06: <id short goal>
```

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 20 Sep 2026 | Open. Feed snapshot from Zazoo nuget profile. |
