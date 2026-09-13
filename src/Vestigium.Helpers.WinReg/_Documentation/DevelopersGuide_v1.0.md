# Vestigium.Helpers.WinReg — Developers Guide

**Document ID:** VEST-HLP-WINREG-DEV-000  
**Version:** 1.3  
**Status:** Engine through Phase 7 + Comparer C1–C4.  
**Date:** 13 September 2026  
**TFM:** `net10.0-windows`

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.WinReg/`.

No `reg.exe`. No `regedit`. Demo gallery is out of this guide.

Comparer: [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md), [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md).

## Façade

```text
RegistryHelper.Identity / Probe()
RegistryHelper.Local / For(machine) / CanConnect / ConnectTimeout
RegistryHelper.Export / Import / Search
RegistryHelper.MountHive / DismountHive
RegistryHelper.WriteIndex(path, hive, key, view, confirm, progress, cancel)
RegistryHelper.Compare(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel)
```

## Read / write / search / mount

Missing Get → null. `/` throws. Views: Default / Registry64 / Registry32.  
Writes need `confirm: true`. Logs path + value **name** only.  
Search cap 256 / depth 32. Mount HKLM or HKU, local only.

## Comparer

Collect on each box, copy JSONL to the desk, compare there.

- `WriteIndex` → `vest-regidx/1` (header / key / value hash / footer). No payloads.
- `Compare` → `vest-regcmp/1` (header / verify / delta / footer).
- Verify is keys only. Unrelated stops unless `force`.
- Same values are footer counts unless `includeSame`.
- A compare file is not a valid index.
- Caps: 2,000,000 values / depth 64. Cancel → Denied.
