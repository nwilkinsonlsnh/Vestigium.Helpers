# Vestigium.Helpers.WinReg — Phase Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-000  
**Version:** 1.3  
**Status:** CRUD phases 0–7 **Done**. Comparer C0 paper locked. C1–C4 not started.  
**Date:** 13 September 2026  
**SRS:** [`Requirements_v1.0.md`](Requirements_v1.0.md) · [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md)  
**Design:** [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md)

---

## 1. Scoreboard (CRUD)

| Phase | Goal | Status |
|---|---|---|
| **0–7** | Read, CRUD, remote, export, import, mount, search | **Done** |

## 2. Scoreboard (Comparer)

| Phase | Goal | Status |
|---|---|---|
| **C0 Paper** | Comparer SRS + design + these rows. Defaults locked. | **Locked** |
| **C1 WriteIndex** | Live local key → `vest-regidx/1` + progress | Planned |
| **C2 Verify** | Two indexes → header + verify + footer. Unrelated stops. | Planned |
| **C3 Merge** | Value hashes → delta lines + footer counts. Same omitted unless asked. | Planned |
| **C4 Harden** | Caps, cancel, no payloads in logs, fixtures, guide matches | Planned |

Deferred after C4: `.reg` → index converter, hive source, `For(machine)` live index, ignore-list, Kql.

```
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~RegistryCompare
```
