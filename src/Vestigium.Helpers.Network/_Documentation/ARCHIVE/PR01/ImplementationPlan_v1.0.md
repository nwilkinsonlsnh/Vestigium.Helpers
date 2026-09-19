# Vestigium.Helpers.Network — Phase Implementation Plan

**Document ID:** VEST-HLP-NETWORK-PLAN-000  
**Version:** 1.2  
**Status:** Historical phases 0–11 shipped. Active work is PR01–PR04.  
**Date:** 19 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Index:** [`README.md`](README.md)

If this file and the SRS disagree, the SRS wins. Subnet math: the subnet addendum wins.

Working tree: `Vestigium.Helpers`. Library root: `src/Vestigium.Helpers.Network/`.

---

## 0. How build mode uses this file

Phase history only. Do not open a “Phase 12” commit. New work follows:

| Plan | Goal |
|---|---|
| [`PR01_ImplementationPlan.md`](PR01_ImplementationPlan.md) | Security harden + docs |
| [`PR02_ImplementationPlan.md`](PR02_ImplementationPlan.md) | Contract lock |
| [`PR03_ImplementationPlan.md`](PR03_ImplementationPlan.md) | Share campaigns + Demo tabs |
| [`PR04_ImplementationPlan.md`](PR04_ImplementationPlan.md) | Linux test gate; optional route write |

Commit form for new work: `Network PR0N: <short goal>`.

---

## 1. Phases (shipped)

| Phase | Goal | Status |
|---|---|---|
| **0–8** | Inventory through harden + demo skeleton | Shipped |
| **9 Subnet** | IPv4+IPv6 describe/plan/VLSM/classify | Shipped (`SubnetEngine`) |
| **10 MAC/EUI** | Parse, modified EUI-64, opt-in OUI | Shipped (`MacEngine`) |
| **11 Bandwidth** | Units, bases, website estimate | Shipped (`BandwidthEngine`) |
| **12 P95 bill** | Samples → percentile → volume | Partial (`BillP95`). Empty-sample rule is PR02. |
| **13 Share** | File-share transfer campaigns | Locked v1.5. Implements in **PR03**. |

---

## 2. Commands

```
dotnet build src/Vestigium.Helpers.Network/Vestigium.Helpers.Network.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Network
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~PR01_
dotnet run --project src/Vestigium.Helpers.Network.Demo/Vestigium.Helpers.Network.Demo.csproj
```
