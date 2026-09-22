# Vestigium.Helpers.Network — PR01 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR01  
**Version:** 1.0  
**Status:** Open  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Scope:** Security harden + document the code that already shipped. Library-only. No share campaigns. No Linux route writer. No HTTP client.

If this file and the SRS disagree, the SRS wins **except** where PR01 explicitly amends a lock (hooks visibility, path confinement, OUI URL policy). Those amendments land in the SRS / Guide in the same PR.

Predecessor: [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md) (phases 0–11 are already in the tree; this plan stops treating Phase 9 as “next”).

---

## 0. Why this PR exists

The library already has inventory, ICMP, DNS, tables, campaigns, snapshot, subnet, MAC/EUI, bandwidth, live OUI, and P95 billing. The phase plan and Developers Guide still say Phase 8/9. Several surfaces are unsafe if a host passes attacker-controlled strings.

PR01 does not add features. It makes the shipped surface fail closed.

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Owner file |
|---|---|---|---|---|---|
| PR01.001 | OUI live lookup: HTTPS-only, allowlist, no auto-redirect, cap body | Fix | P0 | Medium | `MacEngine.cs`, `MacTypes.cs` |
| PR01.002 | Hide `NetworkTestHooks` from hosts | Fix | P0 | Low | `NetworkTestHooks.cs`, tests csproj |
| PR01.003 | Continuous ICMP requires interval + duration cap | Fix | P0 | Low | `IcmpEchoEngine.cs`, `IcmpEchoTypes.cs` |
| PR01.004 | DNS UDP accept only the queried endpoint | Fix | P0 | Medium | `DnsClient.cs` |
| PR01.005 | Route mutate fail-closed when interface unknown | Fix | P0 | Medium | `NetworkRouteMutation.cs` |
| PR01.006 | Confine campaign recipe/results paths to campaign root | Fix | P0 | Medium | `IcmpEchoCampaign.cs`, `CampaignJsonl.cs` |
| PR01.007 | Refresh SRS / Guide / phase plan to shipped reality | Update | P1 | Low | `_Documentation/*` |
| PR01.008 | Cap OUI registry file size and row count | Fix | P1 | Low | `OuiRegistry.cs` |
| PR01.009 | Redact absolute paths in HelperGuard / Reject lines | Fix | P2 | Low | `HelperGuard.cs`, `NetworkLog.cs` |
| PR01.010 | Persistent-route delete must not swallow access-denied | Fix | P2 | Low | `NetworkRouteMutation.cs` |
| PR01.011 | Tests + close | Update | P1 | Medium | `src/Vestigium.Helpers.Tests` |

---

## 2. Build notes

### PR01.001 — OUI SSRF

Current: `HttpClient.GetAsync` on host-supplied `RegistryUrl`, default redirects, third-party default `https://api.macvendors.com/{oui}`.

Lock:

- Scheme must be `https`. Reject `http`, `file`, `ftp`, and anything else after `HelperGuard`.
- Default host remain `api.macvendors.com`. A custom `RegistryUrl` is allowed only when `OuiLookupOptions.AllowCustomRegistry = true` **and** the host is in `AllowedRegistryHosts` (host-supplied list, default empty → custom URL rejected).
- `HttpClient` must use `HttpClientHandler { AllowAutoRedirect = false }` unless a test `Handler` is injected. If a 3xx arrives, typed fail / `Source = None`. Do not chase the hop.
- Read at most 4 KiB. Existing 200-char vendor trim stays.
- Block loopback, link-local, RFC1918, CGNAT, metadata (`169.254.169.254`, `fd00:ec2::254`) even on HTTPS.
- `ParseMac` / inventory / Probe still must not call this.

### PR01.002 — Test hooks

Current: public settable `CampaignRoot`, `UtcNow`, `ProcRoot`.

Lock:

- Class becomes `internal`. Tests use `InternalsVisibleTo` already used elsewhere in the suite, or a dedicated `Vestigium.Helpers.Network.Tests` friend.
- Add `Reset()` in test fixtures’ dispose.
- Guide sentence: hosts must not set hooks. After this PR they cannot.

### PR01.003 — ICMP flood

Lock:

- `Interval` default stays. New floor: `Interval >= 200ms` unless `IcmpEchoOptions.AllowBurst = true`.
- `Count == 0` (continuous) requires `MaxDuration` (default 60 s if omitted; reject `> 24h`).
- `AllowBurst` is explicit. Hosts that want lab burst set it. Demo does not.

### PR01.004 — DNS receive bind

`UdpExchangeAsync`: after `ReceiveAsync`, discard datagrams whose `RemoteEndPoint` is not the server IP + port that was sent to. Transaction ID check stays. Same rule is enough for the TCP path (connected socket).

### PR01.005 — Route interface

`FirstIpv4Index()` must not return `1` as a guess. No up IPv4 NIC → `ArgumentException` after Reject: interface required. Caller may still pass `InterfaceIndex`.

IPv6 mutate stays out of PR01 (PR04).

### PR01.006 — Campaign paths

- Resolve a root: `NetworkTestHooks.CampaignRoot` (tests) else ProgramData / `/var/lib/vestigium/network/campaigns`.
- `ResultsPath` and `RecipePath` must be under that root after `Path.GetFullPath`. `..` escape → Reject + `ArgumentException`.
- `CampaignJsonl.Append` does not `CreateDirectory` above the root.
- Tests inject the temp root only.

### PR01.007 — Docs

In the same PR:

- `ImplementationPlan_v1.0.md`: phases 0–11 shipped; active work is PR01–PR04.
- `DevelopersGuide_v1.0.md`: drop “Phase 8 harden”; list current façade including subnet / MAC / bandwidth; point at these PR plans.
- `Requirements_v1.0.md`: Status stays Draft until PR02 contract lock. Add a one-line pointer to this series.
- `_Documentation/README.md` is the index (this folder).

### PR01.008 — OUI file

- Max file 8 MiB. Max 200_000 rows. Over → Reject.
- Keep comment / CSV / one-line JSON rows. Do not add a JSON parser project.

### PR01.009 — Log redact

Reject lines may keep the parameter name and the filename. Strip directory prefixes (`C:\`, `/var/`, UNC). Packet bytes stay out (already).

### PR01.010 — Persistent delete

`DeletePersistent`: `UnauthorizedAccessException` / `SecurityException` → `NetworkRouteDenied` (same as add). Missing key / missing value stays quiet.

---

## 3. Tests that close this plan

New fixtures in `NetworkPR01Tests.cs` (or split by area if the file grows past ~400 lines).

| Fixture | Covers |
|---|---|
| `PR01_001_custom_registry_http_is_rejected` | `http://` |
| `PR01_001_custom_registry_without_allow_is_rejected` | default deny custom host |
| `PR01_001_loopback_https_is_rejected` | `127.0.0.1` / `::1` |
| `PR01_001_redirect_is_not_followed` | injected handler 302 |
| `PR01_001_parse_mac_does_not_http` | existing lock |
| `PR01_002_hooks_are_not_public` | reflection / compile |
| `PR01_003_continuous_without_duration_gets_default_cap` | Count 0 |
| `PR01_003_zero_interval_rejected_without_burst` | flood floor |
| `PR01_004_udp_foreign_source_discarded` | fake remote endpoint |
| `PR01_005_missing_interface_does_not_use_index_1` | fail closed |
| `PR01_006_results_path_escape_rejected` | `..\..\Windows` |
| `PR01_008_huge_oui_file_rejected` | size cap |
| `PR01_010_persistent_delete_access_denied_is_typed` | Windows-only; skip on Linux |

```text
dotnet build src/Vestigium.Helpers.Network/Vestigium.Helpers.Network.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~PR01_
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Network
```

No public Internet. No live ProgramData. OUI tests use `OuiLookupOptions.Handler`.

---

## 4. Out of PR01

| Item | Goes to |
|---|---|
| Linux route write vs print-only lock | PR02 |
| DNS label / name length | PR02 |
| Campaign JSONL file lock / AppendJsonl | PR02 |
| Linux ICMP DGRAM confirmation | PR02 |
| P95 empty-sample rule | PR02 |
| Share-transfer campaigns | PR03 |
| Demo Subnet / MAC / Bandwidth / Share tabs | PR03 |
| Linux CI TFM split | PR04 |
| IPv6 route mutate | PR04 |
| HTTP reachability | Never this library. HttpIQ. |

---

## 5. Commit form

```text
Network PR01: <id short goal>
```

One commit per ID is allowed. Do not mix PR02 work.

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. Security + docs from the Network review. |
