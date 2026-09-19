# Vestigium.Helpers.Network — PR04 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR04  
**Version:** 1.0  
**Status:** Open. Starts after PR03 close.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Scope:** Platform gate and optional Linux/IPv6 route write. HTTP stays out.

PR04 exists so PR01–PR03 can close without dragging CI topology or a netlink writer into the security and feature slices.

---

## 0. Decision required before code

Pick **one** route-write goal before writing C:

| Option | Meaning |
|---|---|
| **A. Stop** | PR02 lock stands forever for v1. PR04 is CI-only. |
| **B. Linux IPv4 mutate** | Netlink / RTM_NEWROUTE. No `ip`. Typed `NetworkRouteDenied` without `CAP_NET_ADMIN`. |
| **C. B + IPv6 mutate** | Same door, both families. |

Default recommendation: **A**, plus the test TFM split. B/C are only opened if PingIQ / a Linux host actually needs mutate from this DLL.

HTTP reachability is **not** an option here. That is HttpIQ. This library does not grow an HTTP client beyond the already-constrained OUI GET.

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Default |
|---|---|---|---|---|---|
| PR04.001 | Split portable Network tests off `net10.0-windows` | Update | P3 | High | In |
| PR04.002 | GitHub workflow: `ubuntu-latest` job with `--filter FullyQualifiedName~Network` | Update | P3 | Medium | In, after 001 |
| PR04.003 | Lift the 10 Sep 2026 Linux CI waiver in the Guide | Update | P3 | Low | In, after 002 green |
| PR04.004 | Optional packed OUI file snapshot (Phase 12 leftover) | New | P3 | Medium | Optional |
| PR04.005 | Linux IPv4 route mutate (netlink) | New | P3 | High | **Out unless Option B** |
| PR04.006 | IPv6 route mutate both OS | New | P3 | High | **Out unless Option C** |
| PR04.007 | Tests + close | Update | P3 | Medium | In |

---

## 2. Build notes

### PR04.001 / PR04.002 / PR04.003 — CI

Today `Vestigium.Helpers.Tests` is `net10.0-windows` because Charts lives there. That is why Linux Network tests are waived.

Do **not** move Charts. Add a portable test project:

```text
src/Vestigium.Helpers.Tests.Portable/Vestigium.Helpers.Tests.Portable.csproj
TFM: net10.0
```

Move or duplicate the Network / FileIo-portable / Json / Encryption fixtures that have no WPF types. Windows job still runs the existing project (Charts + WinReg + Network Windows-only).

Linux job:

```text
dotnet test src/Vestigium.Helpers.Tests.Portable --filter FullyQualifiedName~Network
```

No public Internet. Campaign tests still inject a temp root. ICMP tests that need a live stack stay `[Trait("Live","Icmp")]` and are not a CI gate.

### PR04.004 — Offline OUI file

Already have `LoadOuiRegistry` / `LookupOuiFile`. Optional work is a packed snapshot checked in under `_Data/` or loaded from ProgramData, `Source = File`. Not required to close PR04.

### PR04.005 / PR04.006 — Route write on Linux / IPv6

Only if Option B/C is chosen in writing on this document (status line + date).

Rules if opened:

- No `ip`, `route`, `netsh`.
- No `sudo`, no `setcap` from the library.
- Missing cap → `NetworkRouteDenied`.
- Same public `AddRoute` / `ChangeRoute` / `RemoveRoute`.
- Tests use a user namespace or skip when cap is absent. Never mutate a developer’s real default route in CI.

---

## 3. Tests that close the default (Option A) plan

| Fixture | Covers |
|---|---|
| Portable project builds on `net10.0` | TFM |
| Existing `PR01_` / `PR02_` / `PR03_` Network fixtures run under Portable | no Windows TFM leak |
| Linux job green on ubuntu-latest | waiver lifted |

```text
dotnet test src/Vestigium.Helpers.Tests.Portable --filter FullyQualifiedName~Network
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network
```

---

## 4. Permanently out of this library

| Item | Where it lives |
|---|---|
| HTTP reachability / HttpIQ jobs | `Vestigium` host, not Helpers.Network |
| Packet capture | Never |
| Cousin CLI spawn | Never |
| Cron / schtasks / systemd install | Never |
| `net use` / mount / stored share passwords | Never |
| Classful mask inference | Never (subnet addendum S3) |

---

## 5. Commit form

```text
Network PR04: <id short goal>
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. CI split default; Linux/IPv6 mutate optional. |
