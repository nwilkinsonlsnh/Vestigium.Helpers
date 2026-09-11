# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.7  
**Status:** Active.  
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
| **G Completeness** | both | IN/BETWEEN, type-mismatch, WOW64, Start-As | Not started |
| **H Harden** | both | Guides and tests | Not started |

Phase F: `ThreadKqlRow` / `SystemKqlRow`. `ProcessHelper.SearchThreads(pid, query)` uses `KqlPack.Thread`. `ProcessHelper.MatchSystem(query)` uses `KqlPack.System`.
