# Kql — Requirements addendum 1.1

**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Status:** Accepted — shipped. Tests passing 15 September 2026.

| Topic | Rule |
|---|---|
| `==` / `!=` | Wildcards `% * ?` are literals. Warning in Vestigium.Logging **and** `KqlCompileResult.Diagnostics`. |
| LIKE | Wildcards apply. |
| Short names | Session lookup: canonical suffix + declared aliases (`PID`, `WindowTitle`, `CommandLine`, …). |
| Unknown field | Error lists aliases and canonical names for the enabled pack. |
| Status | `VestigiumStatus.Warning` is required in Logging. |
