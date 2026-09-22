# Vestigium.Helpers.WinReg — Developers Guide

**Document ID:** VEST-HLP-WINREG-DEV-000  
**Version:** 1.4  
**Status:** Engine through Phase 7 + Comparer C4. Backlog B1–B7 not shipped.  
**Date:** 13 September 2026  
**TFM:** `net10.0-windows`

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.WinReg/`.

No `reg.exe`. No `regedit`. Demo gallery is out of this guide.

- Comparer: [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md), [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md)
- Backlog: [`Requirements_Backlog_v1.0.md`](Requirements_Backlog_v1.0.md), [`Design_Backlog_v1.0.md`](Design_Backlog_v1.0.md), [`ImplementationPlan_Backlog_v1.0.md`](ImplementationPlan_Backlog_v1.0.md)

## Façade (shipped)

```text
RegistryHelper.Identity / Probe()
RegistryHelper.Local / For(machine) / CanConnect / ConnectTimeout
RegistryHelper.Export / Import / Search
RegistryHelper.MountHive(..., confirm, out result) / DismountHive
RegistryHelper.WriteIndex(...)   // Local only until B2
RegistryHelper.Compare(...)      // file vs file; includePayload ignored until B3
```

## Read / write / search / mount

Missing Get → null. `/` throws. Views: Default / Registry64 / Registry32.  
Writes need `confirm: true`. Logs path + value **name** only.  
Search cap 256 / depth 32. Mount HKLM or HKU, local only.

`LastWriteTime` and default-value rows on Full GetKey land in B1.

## Comparer (shipped C4)

Collect on each box, copy JSONL to the desk, compare there.

- `WriteIndex` → `vest-regidx/1`. No payloads.
- `Compare` → `vest-regcmp/1`. Unrelated stops unless `force`.
- Same values are footer counts unless `includeSame`.
- Caps: 2,000,000 values / depth 64. Cancel → Denied.
