# Vestigium.Helpers.Kql — Phase Implementation Plan

**Document ID:** VEST-HLP-KQL-PLAN-000  
**Version:** 1.0-draft  
**Status:** Draft with the SRS. No code until SRS Accepted.  
**Date:** 10 September 2026

## Scoreboard

| Phase | Goal | Status |
|---|---|---|
| **0 Paper** | SRS + this plan + Guide. Lock packs/groups/aliases. | **In progress (draft)** |
| **1 Session + catalog** | `KqlHelper.Create`, packs, group enablement, field lookup | Not started |
| **2 Parser** | Filter grammar, errors with line/col | Not started |
| **3 Binder + 3VL** | Bind names to enabled catalog; true/false/unknown | Not started |
| **4 LIKE / wildcards** | `*` `%` `?`, exact `==` | Not started |
| **5 Host adapter** | Processes `Search(query)` / Watch / Campaign consume Kql | Not started |
| **6 Demo** | Catalog explorer + query box | Not started |
| **7 Harden** | Guide matches engine, taxonomy, tests | Not started |

Do not start Phase 1 until the Processes owner accepts §3 aliases (`CPU.PrivateBytes`, `MEM` vs `RAM`, Service pack).
