# Vestigium.Helpers.Network — PR05 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR05  
**Version:** 1.0  
**Status:** Open. Hygiene only. No new protocol surface.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Depends on:** PR01–PR04 closed.

PR04 landed Option C (Linux netlink + IPv6 write) and a packed OUI. The code is ahead of a few documents and one persist-key constant. PR05 catches that drift. It does **not** add features, Charts, Demo, HttpIQ, or a Linux CI matrix.

Live Linux / Windows-admin route checks wait for an Ubuntu host later. They are listed in §4 so they are not forgotten. They are **not** PR05 work.

---

## 0. Out of PR05

| Item | Where it lives |
|---|---|
| Ubuntu netlink / `CAP_NET_ADMIN` live check | Later, on a real Linux box |
| Windows admin IPv6 `CreateIpForwardEntry2` packing check | Later, on a Windows admin box |
| Portable `net10.0` test project / `ubuntu-latest` job | Repo CI, not this package |
| Charts / Demo / Gallery | Host |
| Full IEEE OUI dump | Host-supplied file |
| Scheduler | Later package |

---

## 1. Work table

| ID | Item | Type | Priority | Complexity | Status |
|---|---|---|---|---|---|
| PR05.001 | Point IPv4 persist write/delete at `NetworkRouteKeys.PersistentRoutes` | Fix | P1 | Low | Open |
| PR05.002 | Requirements_v1.6: share shipped, Option C route write, no Charts | Update | P1 | Low | Open |
| PR05.003 | Design + Developers Guide match 002 | Update | P1 | Low | Open |
| PR05.004 | `ImplementationPlan_v1.6.md` — PR01–PR04 closed, PR05 hygiene | Update | P2 | Low | Open |
| PR05.005 | Repo root README: drop `Network.Demo` run line | Fix | P2 | Low | Open |
| PR05.006 | Rename lying IPv6-write test methods | Update | P2 | Low | Open |
| PR05.007 | Tests + close | Update | P1 | Low | Open |

---

## 2. Slice notes

### PR05.001 — Persist key

Today:

- `NetworkRouteKeys.PersistentRoutes` is the intended HKLM path (`SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\PersistentRoutes`).
- `NetworkRouteMutation.WritePersistent` / `DeletePersistent` still use a private `PersistentKey` that can contain doubled slashes after the Option C edit.

Do:

- Delete the private `PersistentKey` const.
- Use `NetworkRouteKeys.PersistentRoutes` in both persist methods.
- IPv6 persist stays out of that IPv4 Tcpip key (no new persist format in PR05).

Fixture: `PR05_001_persist_key_has_single_separators` (already sketched as `PR04_006_persistent_key_is_single_slash` — keep one name, point it at the live mutation path).

### PR05.002 / PR05.003 — Binding docs

`Requirements_v1.6.md` still reads as if PR03/PR04 had not landed. Same change in Design + Guide, same commit if possible.

Lock text:

- Share campaigns are **shipped** (`PlanShareProbe` / `CreateShareCampaign` / `OpenShareCampaign`). FileIo does probe I/O.
- Route **print** both OS, both families.
- Route **write** is Option C: Windows IPv4 IP Helper, Windows IPv6 `CreateIpForwardEntry2`, Linux netlink IPv4+IPv6.
- Missing admin / `CAP_NET_ADMIN` → `NetworkRouteDenied`.
- `0.0.0.0/0` and `::/0` → `NetworkRouteDenied` (default route is not offered).
- No Charts reference. Hosts plot numbers.
- No Demo project in this package.
- Decision 11 / 11b / 32 updated. Roadmap row for PR04 = shipped, PR05 = hygiene.

Do not rewrite the whole SRS. Patch the stale sentences.

### PR05.004 — Status index

`ImplementationPlan_v1.6.md` still says “PR03–PR04 planned.” Replace with closed PR01–PR04 and open PR05.

### PR05.005 — Root README

Repo `README.md` still has:

```text
dotnet run --project src/Vestigium.Helpers.Network.Demo
```

Remove that line. Other libraries may keep their Demo lines. Do not add a Network gallery back.

### PR05.006 — Test names

| Current name | Lies because | Rename to |
|---|---|---|
| `PR02_006_ipv6_destination_rejected_on_add` | IPv6 write is legal under Option C | `PR02_006_ipv6_write_is_cap_or_cleanup` |
| `PR04_003_ipv6_write_denied` | Same | `PR04_003_ipv6_write_is_cap_or_cleanup` |

Update any roster arrays (`NetworkPR02CloseTests`, `NetworkPR04CloseTests`) in the same change.

Behavior of the tests stays: try Add, Remove in `finally`, swallow `NetworkRouteDenied`.

### PR05.007 — Close

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR05_
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_006
```

| Fixture | Asserts |
|---|---|
| `PR05_001_mutation_uses_route_keys` | Persist methods resolve `NetworkRouteKeys.PersistentRoutes` (no doubled `\\`) |
| `PR05_006_old_ipv6_reject_names_are_gone` | Roster does not contain the two old method names |

No live default-route write. No Ubuntu job.

---

## 3. Commit form

```text
Network PR05: <id short goal>
```

---

## 4. Parked until Ubuntu / admin Windows (not PR05)

| Check | Pass |
|---|---|
| Linux, no `CAP_NET_ADMIN`, IPv4 + IPv6 Add | `NetworkRouteDenied`, message mentions `CAP_NET_ADMIN`, no `ip` process |
| Linux, with cap, `192.0.2.88/32` Add then Remove | Route appears then disappears. Never `0.0.0.0/0`. |
| Windows admin, IPv6 `2001:db8:1::/64` Add then Remove | `CreateIpForwardEntry2` succeeds or typed deny — no hang, no `netsh`. |

Record results on the plan as a dated note when those boxes exist. Do not block publish of Network on them if Windows `FullyQualifiedName~Network` is already green.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. Persist key + docs + README + test names. Live Linux parked. |
