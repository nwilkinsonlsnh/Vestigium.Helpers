# Vestigium.Helpers.Network — Phase Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-000  
**Version:** 1.0  
**Status:** Active. Build mode follows this file.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS **v1.2**)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins. If this file and working code disagree, change the code. Do not reopen locked decisions to make a slice easier.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Network/`. Clone `Vestigium.Logging` as `../Vestigium.Logging`.

Current code on `main`: Identity + Probe only. Do not grow the façade until Phase 0 closes.

---

## 0. How build mode uses this file

1. Read SRS §2 / §2.1 (locks + platform matrix) before touching code.
2. Implement **one phase**. Close gate green before the next.
3. Commit form: `Network phase N: <short goal>`.
4. Do not invent APIs that are not in the SRS public surface. Names may move a token; shapes may not.
5. Do not spawn `ping`, `ping.exe`, `traceroute`, `tracert`, `ip`, `ss`, `netstat`, `route`, `arp`, `nbtstat`, `netsh`, `nslookup` on any OS.
6. Do not implement HTTP reachability, cron/schtasks/systemd install, packet capture, or NetBIOS-on-Linux.
7. After each phase run the commands in §10.

---

## 1. Phases

| Phase | Goal | Close gate |
|---|---|---|
| **0 Paper** | SRS + Guide + this plan agreed; taxonomy registered; umbrella HLP-NET row; façade still Identity + Probe | Docs on disk; `Network_subcategories_are_registered`; Probe-only API |
| **1 Inventory** | `GetWorkstation` / `GetAdapters` / `GetAdapter`; CIDR + IPv4 mask; DHCP best-effort; Linux + Windows | Loopback present; mask and prefix agree on IPv4; Linux NetBIOS-over-TCP = Unknown |
| **2 ICMP Echo** | `NetworkJob`, `IcmpEcho` / `Ping` alias, Count default 4, Count=0 cancel, option guards, Linux payload retry | 127.0.0.1 default sends 4 or typed ProtocolForbidden; cancel ≤ 5 s; no process spawn |
| **3 ICMP Trace + DNS** | `IcmpTrace` / `Trace` alias; `LookupAsync` / `LookupManyAsync`; explicit nameserver; ProbeProtocol | localhost lookup; explicit 127.0.0.1:53 times out/refuses without crash; trace to 127.0.0.1 ends or max-hops |
| **4 Stack tables** | `GetConnections`, TCP/UDP stats, `GetRoutes` print, `GetNeighbors` | Lists returned (may be empty); no `ss`/`ip`/`arp` spawn; Linux PID best-effort |
| **5 Campaigns + JSONL** | Json project reference; recipe `.json`; append-only stats `.jsonl`; clock + 15 min grace; injected root | Idempotent window; OpenJsonl sees `echo` + `windowSummary`; missed window writes `windowMissed` |
| **6 Snapshot / route mutate / NetBIOS** | `GetInterfaceSnapshot`; `AddRoute` / `ChangeRoute` / `DeleteRoute`; Windows NetBIOS reads | Snapshot no-throw; mutations typed access-denied; NetBIOS throws on Linux |
| **7 Demo** | Replace SkeletonWindow with gallery pages (Windows WPF) | Adapters, Echo, Trace, DNS, Connections, Routes, Campaign tail; Probe on startup only |
| **8 Harden** | Sparse HelperLog, no packet bytes, Developers Guide matches engine, portable tests on Windows **and** Linux | `FullyQualifiedName~Network` green; no live ProgramData / `/var/lib/vestigium` |

---

## 2. Locked decisions (do not debate)

Copied from SRS v1.2 §2. Build mode treats these as constants.

| # | Lock |
|---|---|
| 1 | `net10.0` library. Hosts subscribe. |
| 1b | Windows + Linux first-class. macOS not a test gate. |
| 2 | Protocols, not shells. |
| 3 | `Ping` is an alias of `IcmpEcho`. JSONL kind `icmpEcho`. |
| 4 | Default Count = **4**. `0` = continuous. |
| 5 | Campaign grace **15 minutes**. No backfill after grace. |
| 6 | No schtasks / cron / systemd unit from this library. |
| 7 | Stats JSONL via Json. Audit JSONL via HelperLog. Never mix. |
| 8 | Do not `OpenJsonl` + full Save to append one echo. |
| 9 | Route print both OS. Route write explicit + typed deny. |
| 10 | NetBIOS Windows-only. |
| 11 | Linux ICMP: DGRAM first. Custom payload reject → empty retry + `PayloadRestricted`. |
| 12 | Campaign default roots: `%ProgramData%\Vestigium\Network\Campaigns\` / `/var/lib/vestigium/network/campaigns/`. Tests inject temp. Never silent `$HOME`. |
| 13 | Probe on-box. No off-box ICMP/DNS in Probe. |
| 14 | HelperLog only. Category `Helpers`. APPID `Network`. Never `Initialize`. |

---

## 3. Phase 0 — Paper and taxonomy

**Goal.** Build mode and the repo agree on the contract. Engine stays Probe.

**Edit**

- SRS Status → **Accepted** (v1.2) when you say Accept. Until then leave Draft; still add this plan.
- Developers Guide points at this plan.
- This file under `src/Vestigium.Helpers.Network/_Documentation/`.
- `HelperLog.Subcategories` + `CreateTaxonomy()` register:

  `Inventory`, `Adapter`, `Icmp`, `Dns`, `Connection`, `Neighbor`, `Netbios`, `Route`, `Snapshot`, `Campaign`, `Job`, `Stats`

  Keep `Probe`, `Identity`, `Guard`.
- Test `Network_subcategories_are_registered`.
- Umbrella `_Documentation/Requirements_v1.0.md` HLP-NET row: SRS v1.2 draft/accepted + this plan.
- csproj Description → workstation inventory and protocol jobs (drop stale “HTTP and socket”).

**Do not**

- Grow `NetworkHelper` past Identity + Probe.
- Add the Json project reference yet (Phase 5).
- Touch FileIo / Encryption / Charts.

**Close gate**

- Three docs in `_Documentation/`.
- `dotnet test --filter Network_subcategories`.
- Commit: `Network phase 0: plan + taxonomy`.

---

## 4. Phase 1 — Inventory

**Goal.** One snapshot a host can bind.

**Build**

- Types: `WorkstationNetwork`, `NetworkAdapter`, `UnicastAddress`, `DhcpInfo`, `NetbiosOverTcp`, `NetworkAdapterQuery`.
- `GetWorkstation`, `GetAdapters`, `GetAdapter`.
- IPv4 rows expose **both** `PrefixLength` and `SubnetMask`, and they agree. Derive; do not invent `/24`.
- Linux: NetBIOS-over-TCP = `Unknown`. DHCP lease timestamps null unless a readable source exists (not required).
- Guard unknown adapter name.

**Do not**

- ICMP, DNS, campaigns.
- Subscribe to `NetworkChange`.

**Close gate**

- At least loopback with an address on Windows and Linux.
- IPv4 prefix/mask agreement test.
- Commit: `Network phase 1: workstation inventory`.

---

## 5. Phase 2 — ICMP Echo job

**Goal.** Protocol reachability. Default 4 echoes.

**Build**

- `NetworkJob<TResult>`, `NetworkProgress`, `IcmpEchoOptions`, `IcmpEchoReply`, `IcmpEchoResult`.
- `NetworkHelper.IcmpEcho` + `Ping` alias.
- Engine: `System.Net.NetworkInformation.Ping` (ICMP). Linux: tolerate payload PNSE → empty retry + `PayloadRestricted`.
- Guards: Count ≥ 0; Timeout 10 ms–60 s; BufferSize 1–65500.
- Count = 0 + Cancel / token completes Cancelled ≤ 5 s.
- Sparse HelperLog: recipe + summary, not each success.

**Do not**

- Spawn ping.
- Write campaign JSONL yet (`StatsPath` may exist as a no-op or throw NotImplemented until Phase 5 — prefer omit until Phase 5).
- Trace or DNS.

**Close gate**

- Default options against `127.0.0.1` send 4 or return typed `ProtocolForbidden` / `TimedOut` (no crash).
- Count = 2 sends 2.
- Commit: `Network phase 2: ICMP Echo job`.

---

## 6. Phase 3 — ICMP Trace and DNS

**Goal.** Path + nameserver checks.

**Build**

- `IcmpTrace` / `Trace` alias. TTL walk. `ProbeProtocol = Icmp | Udp`. Defaults MaxHops 30, ProbesPerHop 3.
- `LookupAsync` / `LookupManyAsync`. Null server = OS resolver. Set server = RFC 1035 UDP/TCP 53.
- Types A/AAAA/CNAME/MX/NS/PTR/SOA/TXT/SRV/ANY.
- No REPL.

**Do not**

- Spawn tracert/nslookup/dig.
- Public Internet in default tests.

**Close gate**

- `LookupAsync("localhost")` returns a loopback address.
- Lookup to `127.0.0.1` with nothing listening → Timeout or Refused, not an unhandled exception.
- Trace to 127.0.0.1 completes (reached or max hops) without spawn.
- Commit: `Network phase 3: ICMP Trace and DNS`.

---

## 7. Phase 4 — Connections, routes print, neighbors

**Goal.** Stack tables both OS.

**Build**

- `GetConnections` + query (protocol, listening, numeric default true).
- TCP/UDP statistics.
- `GetRoutes` print (family filter). Fields: destination, prefix/mask, gateway, interface, metric.
- `GetNeighbors` (ARP + ND).
- Linux: BCL then `/proc` / netlink. Process name best-effort; null if denied.

**Do not**

- `AddRoute` yet (Phase 6).
- Spawn `ss` / `ip` / `arp`.

**Close gate**

- Each method returns a list (empty legal).
- No child processes in tests.
- Commit: `Network phase 4: connections routes neighbors`.

---

## 8. Phase 5 — Campaigns and JSONL

**Goal.** Scheduled ICMP Echo with append-only stats.

**Build**

- ProjectReference `Vestigium.Helpers.Json`.
- `IcmpEchoCampaign`, `IcmpEchoCampaignOptions`, `EchoWindow`.
- Recipe write/read via `JsonHelper` (`.json`).
- Stats append: `JsonHelper.ToJson(record, WriteIndented = false)` + line append. Inject `NetworkTestHooks.CampaignRoot`.
- Line kinds: `campaignStart`, `windowStart`, `echo`, `windowSummary`, `windowMissed`, `campaignEnd`.
- Grace 15 min. Idempotent `campaignId + date + localTime`.
- On start, only today’s due-and-in-grace windows.
- Optional `StatsPath` on one-shot Echo now wired.

**Do not**

- Install cron/schtasks/systemd.
- Rewrite the whole JSONL per echo.
- Write under `Logs\`.
- Silent `$HOME` fallback.

**Close gate**

- Two-window recipe on today: in-grace window runs once; second `RunAsync` does not duplicate.
- `JsonHelper.OpenJsonl` enumerates `echo` + `windowSummary`.
- Past-grace window → `windowMissed`, zero extra echoes.
- Commit: `Network phase 5: echo campaigns and JSONL`.

---

## 9. Phase 6 — Snapshot, route mutate, NetBIOS

**Goal.** Remaining inventory planes.

**Build**

- `GetInterfaceSnapshot` (adapters + DNS + routes + neighbors + WLAN names when present).
- `AddRoute` / `ChangeRoute` / `DeleteRoute` with HelperLog Warning. Access denied → HelperGuard + `UnauthorizedAccessException`. No UAC/`sudo`.
- Windows: local names, cache, `-a`/`-A`, sessions.
- Linux: NetBIOS methods throw `PlatformNotSupportedException` after HelperGuard.

**Do not**

- netsh/ip write beyond route mutate.
- WLAN profile keys.
- nbtstat `-R`.

**Close gate**

- Snapshot returns on both OS.
- Mutation tests opt-in / admin-only; default CI asserts deny-or-skip, not a crash.
- NetBIOS Linux throw test.
- Commit: `Network phase 6: snapshot route NetBIOS`.

---

## 10. Phase 7 — Demo gallery

**Goal.** Windows WPF host that exercises the library.

**Build**

- Replace `SkeletonWindow` in `Vestigium.Helpers.Network.Demo`.
- Pages: Overview/Probe, Adapters, ICMP Echo, Trace, DNS, Connections, Neighbors, Routes, Campaign (edit windows + tail JSONL), Snapshot. Hide or disable NetBIOS off Windows.
- APPID `Network`. Startup = Probe only. Off-box targets operator-initiated.

**Close gate**

- `dotnet run --project src/Vestigium.Helpers.Network.Demo` opens the gallery.
- Commit: `Network phase 7: Network demo gallery`.

---

## 11. Phase 8 — Harden

**Goal.** Contract tests and quiet logs.

**Build**

- Full portable §11 tests.
- HelperLog: no packet bytes, no Exception objects, no echo-per-line when a campaign is bound.
- Developers Guide matches the running types.
- csproj tags/description accurate. Umbrella README Network row updated (inventory + ICMP, not “HTTP / socket helpers”).
- Note for CI: add a Linux job for `--filter FullyQualifiedName~Network` when the pipeline can (Windows-latest already exists).

**Close gate**

- `dotnet test --filter FullyQualifiedName~Network` green on Windows.
- Same filter green on a Linux agent or documented waiver with date.
- Commit: `Network phase 8: harden portable tests`.

---

## 12. Logging cheat sheet

Category = `Helpers`. APPID = `Network`.

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing |
| Inventory / Adapter | Snapshot counts |
| Icmp | Echo/trace recipe + summary |
| Dns | Server, QNAME, type, RCODE, answer count |
| Connection / Neighbor / Route / Snapshot | Counts; route mutate Warning |
| Netbios | Windows queries |
| Campaign | Start, window start/end/miss, end |
| Job | Success / Failed / Cancelled |
| Stats | Path + bytes appended, not echo bodies |

---

## 13. Out of this plan

HTTP/TLS client, Json `AppendJsonl` implementation (belongs in Json; Network consumes it later), cron/schtasks/systemd install, packet capture, pathping, NetBIOS on Linux, NetworkManager writers, macOS CI gate, spawning any cousin CLI.

---

## 14. Commands

```
dotnet build src/Vestigium.Helpers.Network/Vestigium.Helpers.Network.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Network
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter Network_subcategories
dotnet run --project src/Vestigium.Helpers.Network.Demo/Vestigium.Helpers.Network.Demo.csproj
```

---

## 15. Definition of done (v1.2 library)

1. SRS Status Accepted and this plan in `_Documentation/`.
2. Subcategories registered and tested.
3. One `net10.0` DLL used on Windows and Linux.
4. No cousin CLI process.
5. Inventory + ICMP Echo (default 4) + Trace + DNS + tables + campaigns + snapshot.
6. Route write explicit; NetBIOS Windows-only.
7. Campaign JSONL append + Json replay.
8. Probe on-box. Tests inject temp roots.
9. Portable Network filter green.
10. Demo gallery on Windows only.

Phase 0 closes 1–2. Phases 1–8 close 3–10.
