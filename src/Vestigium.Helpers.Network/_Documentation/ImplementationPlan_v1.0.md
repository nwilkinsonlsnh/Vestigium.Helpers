# Vestigium.Helpers.Network — Phase Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-000  
**Version:** 1.1  
**Status:** Active. Phases 0–8 shipped. Phase 9 is the subnet calculator.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md) (SRS **v1.3**)  
**Subnet lock:** [`SubnetCalculator_v1.3.md`](SubnetCalculator_v1.3.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins. Subnet math: the subnet addendum wins.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Network/`.

---

## 0. How build mode uses this file

1. Read SRS §2 / §2.1 and the subnet addendum before touching prefix code.
2. Implement **one phase**. Close gate green before the next.
3. Commit form: `Network phase N: <short goal>`.
4. Do not invent APIs that are not in the SRS public surface.
5. Do not spawn cousin CLIs, including `ipcalc` / `ipv6calc`.
6. After each phase run the commands in §14.

---

## 1. Phases

| Phase | Goal | Close gate |
|---|---|---|
| **0–8** | Shipped. Inventory through harden + demo. | See v1.0 plan history |
| **9 Subnet** | IPv4+IPv6 describe/plan/VLSM/classify A–E | Addendum §6 tests green; Demo Subnet tab |

---

## 2. Phase 9 — Prefix calculator

**Goal.** Operators pass an address plus “at least N hosts” or “at least K networks” (or a CIDR) and bind network, broadcast, mask, CIDR, usable range, class label, and child lists. IPv6 is required in this slice.

**Build**

- Types: `AddressClass`, `AddressKind`, `TraditionalClass`, `PrefixBlock`, `PrefixPlan`, `SubnetQuery`.
- Engine: `SubnetEngine` (BigInteger). Reuse `Ipv4Prefix` for mask ↔ prefix.
- `NetworkHelper` methods listed in the addendum §3.
- Register HelperLog subcategory `Subnet`.
- Demo tab: address / hosts / networks / split / VLSM + result grid.

**Do not**

- Infer a classful mask when prefix and mask are both missing.
- Emit 2^20 child objects.
- Treat IPv6 as “later”.
- Use class to pick a mask.

**Close gate**

- Tests in addendum §6.
- `dotnet test --filter FullyQualifiedName~Subnet`
- Commit: `Network phase 9: subnet calculator IPv4 IPv6`

---

## 3. Commands

```
dotnet build src/Vestigium.Helpers.Network/Vestigium.Helpers.Network.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Network
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Subnet
dotnet run --project src/Vestigium.Helpers.Network.Demo/Vestigium.Helpers.Network.Demo.csproj
```
