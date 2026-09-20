# Vestigium.Helpers.Network — document set (Rev 1.6)

**Status:** Single source of truth as of 19 September 2026.  
**Package:** `Vestigium.Helpers.Network` (`net10.0`)

| Document | Role |
|---|---|
| [`Requirements_v1.6.md`](Requirements_v1.6.md) | Binding contract. What the library must do. |
| [`Design_v1.6.md`](Design_v1.6.md) | Locked decisions and shape. Why it looks like this. |
| [`DevelopersGuide_v1.6.md`](DevelopersGuide_v1.6.md) | How to call it. Doors, exceptions, tests. |

If these three disagree, **Requirements wins**. Design and the Guide must be updated in the same change.

Prefix math detail: archived [`ARCHIVE/PR01/SubnetCalculator_v1.3.md`](ARCHIVE/PR01/SubnetCalculator_v1.3.md) still wins over the SRS summary for calculator rules.  
MAC / bandwidth detail: [`ARCHIVE/PR01/MacAndBandwidth_v1.4.md`](ARCHIVE/PR01/MacAndBandwidth_v1.4.md).  
Share-transfer campaigns: [`ARCHIVE/PR01/ShareCampaign_v1.5.md`](ARCHIVE/PR01/ShareCampaign_v1.5.md) — locked text, implements in **PR03**.

## Active PR series

| Plan | Goal | Status |
|---|---|---|
| [`PR01_ImplementationPlan.md`](PR01_ImplementationPlan.md) | Security harden + docs match shipped code | **Code complete on main.** Confirm `PR01_` + `Network` on Windows. |
| [`PR02_ImplementationPlan.md`](PR02_ImplementationPlan.md) | Contract lock + remaining harden | Open. After PR01 Windows gate. |
| [`PR03_ImplementationPlan.md`](PR03_ImplementationPlan.md) | File-share transfer campaigns | Open. After PR02. |
| [`PR04_ImplementationPlan.md`](PR04_ImplementationPlan.md) | Linux test gate + optional IPv6 route write | Open. After PR03. HTTP stays out. |

Phase 0–11 history: [`ARCHIVE/PR01/ImplementationPlan_v1.0.md`](ARCHIVE/PR01/ImplementationPlan_v1.0.md). Those phases are in the tree. Active work is PR01–PR04.
