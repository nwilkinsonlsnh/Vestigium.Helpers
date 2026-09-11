# PR02-Rev1 — Processes & Kql Library Backlog

**Document ID:** VEST-HLP-PRC-PR02-REV1  
**Version:** 1.2  
**Status:** Active. Build mode follows this file.  
**Date:** 11 September 2026  
**Scope:** `Vestigium.Helpers.Processes` and `Vestigium.Helpers.Kql` **libraries only**

See v1.1 body for phases B–H. Scoreboard:

| Phase | Library | Goal | Status |
|---|---|---|---|
| **A Catalog** | Kql | Process-row fields + watch-only hint + tighter bind errors | **Done** |
| **B Bind** | Processes | Full `ProcessKqlRow`; Kql `Search` uses the fields the AST names | Not started |
| **C Tempo** | Processes | Query watcher + campaign ticks keep last sample | Not started |
| **D Campaign** | Processes | `Match` optional when `Query` set; richer JSONL | Not started |
| **E Safety** | Processes | KillTree/KillSearch + search order | Not started |
| **F Rows** | Processes | Thread row + system row as `IKqlRow` | Not started |
| **G Completeness** | both | `IN`/`BETWEEN`, type-mismatch copy, WOW64 path, Start-As audit | Not started |
| **H Harden** | both | Tests, guides, comment-key hash | Not started |

Phase A shipped: `PROC.Description` / Version / Signer / SignerTrust / Package / Autostart / Comment / WindowStatus / Dep / Aslr / Cfg / StackProtection / Protection / Dpi / UiAccess / Virtualized / EnterpriseContext; `KqlField.WatchOnly`; bind error `unknown field '{name}' on pack=… enabled (…):` first 12 +rest; `KqlValue.From("")` is Unknown.
