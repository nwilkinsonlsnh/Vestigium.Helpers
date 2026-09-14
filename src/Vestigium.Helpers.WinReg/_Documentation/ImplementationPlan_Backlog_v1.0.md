# Vestigium.Helpers.WinReg — Backlog Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-BL-000  
**Version:** 1.2  
**Status:** B0 locked. B1–B2 **Done**. B3–B7 planned.  
**Date:** 13 September 2026  
**SRS:** [`Requirements_Backlog_v1.0.md`](Requirements_Backlog_v1.0.md)

| Phase | Covers | Status |
|---|---|---|
| **B0 Paper** | Backlog SRS + design. | **Locked** |
| **B1 Snapshots** | LastWriteTime + default value | **Done** |
| **B2 Client index + progress** | `RegistryClient.WriteIndex`; progress/cancel on WriteIndex, Export, Import | **Done** |
| **B3 Compare host API** | `RegistryCompareSummary` + honor `includePayload` | Planned |
| **B4 IndexFromReg** | `.reg` → `vest-regidx/1` | Planned |
| **B5 Privilege + connect** | Revert backup/restore; CanConnect | Planned |
| **B6 Write extras** | Import allow-list; Copy/Rename; Search cancel | Planned |
| **B7 Optional** | Compare ignore prefixes | Planned |
