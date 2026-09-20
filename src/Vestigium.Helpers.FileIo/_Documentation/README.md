# Vestigium.Helpers.FileIo — documentation

| Document | Role |
|---|---|
| [Requirements_v1.0.md](Requirements_v1.0.md) | Contract (SRS v1.1). Wins if code or a plan disagrees. |
| [DevelopersGuide_v1.0.md](DevelopersGuide_v1.0.md) | How a host runs a job. |
| [ImplementationPlan_v1.0.md](ImplementationPlan_v1.0.md) | Historical. Engine phases 0–5 (paper through harden). |
| [ImplementationPlan_v1.2.md](ImplementationPlan_v1.2.md) | **Active.** Packaging, Logging catalog, tests, index, gallery. |

## Active PRs (v1.2 wave)

| PR | Plan | Goal |
|---|---|---|
| PR01 | [PR01_ImplementationPlan.md](PR01_ImplementationPlan.md) | Analytics NuGet 1.0.1. Stop the project reference. |
| PR02 | [PR02_ImplementationPlan.md](PR02_ImplementationPlan.md) | Logging done properly. EVENTID block + JobId. Retire HelperCompat. |
| PR03 | [PR03_ImplementationPlan.md](PR03_ImplementationPlan.md) | Put the engine tests back in the suite. |
| PR04 | [PR04_ImplementationPlan.md](PR04_ImplementationPlan.md) | Dest index on disk. One wildcard rule. |
| PR05 | [PR05_ImplementationPlan.md](PR05_ImplementationPlan.md) | Demo gallery or stop claiming one. Docs match the slnx. |
| PR06 | [PR06_ImplementationPlan.md](PR06_ImplementationPlan.md) | Pack FileIo. Hashing reference only if that nupkg exists. |

Do not start PR0N+1 until PR0N's close gate is green.
