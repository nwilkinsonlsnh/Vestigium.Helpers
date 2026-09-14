# Vestigium.Helpers.WinReg — Backlog Implementation Plan

**Document ID:** VEST-HLP-WINREG-PLAN-BL-000  
**Version:** 1.7  
**Status:** B0–B7 **Done**.  
**Date:** 14 September 2026

| Phase | Covers | Status |
|---|---|---|
| **B0 Paper** | Backlog SRS + design. | **Locked** |
| **B1 Snapshots** | LastWriteTime + default value | **Done** |
| **B2 Client index + progress** | Client WriteIndex; progress/cancel | **Done** |
| **B3 Compare host API** | `CompareDetailed` + `includePayload` | **Done** |
| **B4 IndexFromReg** | `.reg` → `vest-regidx/1` | **Done** |
| **B5 Privilege + connect** | Revert backup/restore; CanConnect timeout | **Done** |
| **B6 Write extras** | Import allow-list; Copy/Rename; Search cancel | **Done** |
| **B7 Optional** | Compare ignore prefixes | **Done** |

ACL read stays deferred. Alt creds, remote mount, Kql `REG.*`, watchers stay out.
