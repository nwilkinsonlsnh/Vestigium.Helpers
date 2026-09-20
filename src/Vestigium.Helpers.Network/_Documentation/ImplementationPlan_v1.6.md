# Vestigium.Helpers.Network — implementation status

**Document ID:** VEST-HLP-NETWORK-PLAN-STATUS  
**Version:** 1.6  
**Date:** 19 September 2026

Phases 0–11 from [`ARCHIVE/PR01/ImplementationPlan_v1.0.md`](ARCHIVE/PR01/ImplementationPlan_v1.0.md) are **in the tree**. That file is history.

Active work is the PR series in this folder:

| PR | Status |
|---|---|
| PR01.001–010 | Code on `main` |
| PR01.007 | Live docs v1.6 |
| PR01.011 | Fixture roster on `main`. Run the commands below on Windows-latest. |
| PR02–PR04 | Planned. Do not mix into PR01 commits. |

```text
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~PR01_
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~Network
```
