# Vestigium.Helpers.WinReg — Backlog Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-BL-000  
**Version:** 1.1  
**Status:** B0 locked. B1 **Done**. B2–B7 planned.  
**Date:** 13 September 2026  
**SRS:** [`Requirements_Backlog_v1.0.md`](Requirements_Backlog_v1.0.md)  
**Design:** [`Design_Backlog_v1.0.md`](Design_Backlog_v1.0.md)

CRUD 0–7 and Comparer C0–C4 stay **Done** on [`ImplementationPlan_v1.0.md`](ImplementationPlan_v1.0.md).

---

## Scoreboard

| Phase | Covers | Status |
|---|---|---|
| **B0 Paper** | Backlog SRS + design. Mount signature amended. | **Locked** |
| **B1 Snapshots** | LastWriteTime + default value on Full GetKey | **Done** |
| **B2 Client index + progress** | `RegistryClient.WriteIndex`; progress/cancel on WriteIndex, Export, Import | Planned |
| **B3 Compare host API** | `RegistryCompareSummary` + honor `includePayload` | Planned |
| **B4 IndexFromReg** | `.reg` → `vest-regidx/1` | Planned |
| **B5 Privilege + connect** | Revert backup/restore; CanConnect does not leak a wait | Planned |
| **B6 Write extras** | Import `allowedHives`; CopyKey / RenameKey; Search cancel | Planned |
| **B7 Optional** | Compare ignore prefixes. ACL read stays deferred. | Planned |

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryBacklogB1
```
