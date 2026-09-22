# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.9  
**Status:** Closed — phases A–H shipped  
**Date:** 11 September 2026  
**Scope:** Processes + Kql libraries only.

| Phase | Library | Goal | Status |
|---|---|---|---|
| **A Catalog** | Kql | Process-row fields + watch-only + bind errors | **Done** |
| **B Bind** | Processes | Full `ProcessKqlRow`; Kql Search level from AST | **Done** |
| **C Tempo** | Processes | Previous-sample map on query watcher + campaign | **Done** |
| **D Campaign** | Processes | Optional Match; richer JSONL | **Done** |
| **E Safety** | Processes | KillTree/KillSearch + search order | **Done** |
| **F Rows** | Processes | Thread + system `IKqlRow` | **Done** |
| **G Completeness** | both | IN/BETWEEN, type-mismatch, WOW64, Start-As | **Done** |
| **H Harden** | both | Guides and tests | **Done** |

Phase H: DevelopersGuides list `Search(query)`, `Watch(query)`, optional campaign `Query`, `SearchThreads`, `MatchSystem`, `IN` / `BETWEEN`. Comment persist keys are SHA-256 of the normalized path. Autostart documented as best-effort HKCU/HKLM Run + Startup folder. No Demo diff.
