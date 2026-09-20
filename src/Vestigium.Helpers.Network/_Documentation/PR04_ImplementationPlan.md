# Vestigium.Helpers.Network — PR04 implementation plan

**Document ID:** VEST-HLP-NETWORK-PLAN-PR04  
**Version:** 1.1  
**Status:** Open. Starts after PR03 close.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network` only  
**Default route goal:** Option A — v1 write stays Windows IPv4. Linux / IPv6 mutate stay denied.

PR01–PR03 closed security, contract lock, and share campaigns. PR04 is the leftover **Network package** work. It is not a gallery, not Charts, not a scheduler, not HttpIQ.

---

## 0. What this package is not

| Item | Why it is out |
|---|---|
| Charts / ScottPlot / WPF plots | Application host wires numbers to Charts. Network returns `BandwidthAmount`, `PercentileBill`, `ShareCampaignResult`. No Charts reference. |
| Demo / Gallery tabs | Host concern. Skipped in PR03.007–009. |
| Portable test project / ubuntu workflow | Solution CI. Tests today are `net10.0-windows` because the umbrella test assembly also references WinReg and (on Windows) Charts. That coupling is not Network's to fix. |
| HTTP reachability | HttpIQ / host. Network's only HTTP is the already-constrained OUI GET. |
| Packet capture, cousin CLIs, cron/schtasks, `net use`, stored share passwords | Never |

If a host wants a chart of share hours or P95, it reads the result object and calls Charts itself.

---

## 1. Decision: route write

Pick **one** before any mutate code. Default is A.

| Option | Meaning | v1 |
|---|---|---|
| **A. Stop** | Windows IPv4 IP Helper + persistent HKLM as locked in PR02. Linux and IPv6 write throw `NetworkRouteDenied`. | **Default** |
| **B. Linux IPv4 mutate** | Netlink / RTM_NEWROUTE. No `ip`. No cap → `NetworkRouteDenied`. | Out unless written here with a date |
| **C. B + IPv6 mutate** | Same door, both families, both OS. | Out unless written here with a date |

Do not start B or C because a test matrix would look nicer. Start them only if a real host needs mutate from this DLL on Linux or IPv6.

---

## 2. Work table (Network package only)

| ID | Item | Type | Priority | Complexity | In by default |
|---|---|---|---|---|---|
| PR04.001 | Docs lock: Network has no Charts reference; hosts chart results | Update | P2 | Low | Yes |
| PR04.002 | Optional packed OUI snapshot for offline `LookupOuiFile` | New | P3 | Medium | Optional |
| PR04.003 | Confirm Linux / IPv6 mutate still throw `NetworkRouteDenied` (Option A) | Update | P2 | Low | Yes |
| PR04.004 | Linux IPv4 netlink mutate | New | P3 | High | **No — Option B only** |
| PR04.005 | IPv6 mutate both OS | New | P3 | High | **No — Option C only** |
| PR04.006 | Tests + close | Update | P2 | Low | Yes |

CI topology (portable `net10.0` test project, `ubuntu-latest` job, lift the 10 Sep 2026 Linux waiver) is a **repo** PR, not a Network feature slice. It may mention Network fixtures. It must not land as a Charts or Network API change.

---

## 3. Slice notes

### PR04.001 — Docs lock

Requirements / Design / Developers Guide one paragraph:

- Network does not reference `Vestigium.Helpers.Charts`.
- Share and bandwidth results are numbers + disclaimer text.
- Plotting is the host.

No code change required if `Vestigium.Helpers.Network.csproj` still has no Charts `ProjectReference` (verify in the close test).

### PR04.002 — Packed OUI (optional)

Already shipped: `LoadOuiRegistry` / `LookupOuiFile`. Optional work is a checked-in snapshot under `_Data/` or ProgramData, `Source = File`. Skip if you do not want a file in the package.

### PR04.003 — Option A confirmation

Re-run the PR02.002 / PR02.006 fixtures:

- Linux `AddRoute` / `ChangeRoute` / `RemoveRoute` → `NetworkRouteDenied`
- IPv6 destination on write → `NetworkRouteDenied`
- Windows IPv4 write path unchanged

No new writer.

### PR04.004 / PR04.005 — only if B or C is chosen in writing

Rules if opened later:

- No `ip`, `route`, `netsh`.
- No `sudo` / `setcap` from the library.
- Missing capability → `NetworkRouteDenied`.
- Same public `AddRoute` / `ChangeRoute` / `RemoveRoute`.
- Tests must not mutate a developer default route in CI.

### PR04.006 — Close

| Fixture | Asserts |
|---|---|
| `PR04_001_network_has_no_charts_reference` | Network csproj / assembly does not reference Charts |
| `PR04_003_linux_mutate_denied` | existing typed deny |
| `PR04_003_ipv6_write_denied` | existing typed deny |

```text
dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~PR04_
```

---

## 4. Permanently out of this library

| Item | Where it lives |
|---|---|
| Charts, plots, WPF controls | `Vestigium.Helpers.Charts` + host |
| Demo gallery | Host, later |
| Portable test TFM / Linux CI job | Repo workflow PR |
| HTTP reachability | Host / HttpIQ |
| Scheduler / cron / schtasks / systemd | Later Scheduler package or host |
| `net use` / mount / passwords | Never |
| Packet capture / cousin CLIs | Never |

---

## 5. Commit form

```text
Network PR04: <id short goal>
```

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 19 Sep 2026 | Open. CI split mixed into Network; Charts named as test-TFM reason. |
| 1.1 | 19 Sep 2026 | Charts, Demo, and portable CI removed from Network scope. Option A default. |
