# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.3  
**Status:** Active.  
**Date:** 11 September 2026  
**Scope:** Processes + Kql libraries only.

| Phase | Library | Goal | Status |
|---|---|---|---|
| **A Catalog** | Kql | Process-row fields + watch-only + bind errors | **Done** |
| **B Bind** | Processes | Full `ProcessKqlRow`; Kql Search level from AST | **Done** |
| **C Tempo** | Processes | Previous-sample map on query watcher + campaign | Not started |
| **D Campaign** | Processes | Optional Match; richer JSONL | Not started |
| **E Safety** | Processes | KillTree/KillSearch + search order | Not started |
| **F Rows** | Processes | Thread + system `IKqlRow` | Not started |
| **G Completeness** | both | IN/BETWEEN, type-mismatch, WOW64, Start-As | Not started |
| **H Harden** | both | Guides and tests | Not started |

Phase B: `ProcessKqlRow` maps Description/Version/Signer/Package/Autostart/Comment/WindowStatus/mitigations. Blank WindowTitle is Unknown. `Search(query)` upgrades Slim→Full when the AST names a Full field (`CommandLine`, `Description`, …).

Locks, parked items, and phase narratives remain as v1.1/v1.2.
