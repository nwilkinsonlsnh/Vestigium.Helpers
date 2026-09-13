# Vestigium.Helpers.WinReg — Developers Guide

**Document ID:** VEST-HLP-WINREG-DEV-000  
**Version:** 1.2  
**Status:** Engine through Phase 7. Comparer is specified, not shipped.  
**Date:** 13 September 2026  
**TFM:** `net10.0-windows`

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.WinReg/`.

No `reg.exe`. No `regedit`. Demo gallery is out of this guide.

Comparer paper: [`Requirements_Comparer_v1.0.md`](Requirements_Comparer_v1.0.md), [`Design_Comparer_v1.0.md`](Design_Comparer_v1.0.md).

## Façade (shipped)

```text
RegistryHelper.Identity / Probe()
RegistryHelper.Local / For(machine) / CanConnect / ConnectTimeout
RegistryHelper.Export / Import / Search
RegistryHelper.MountHive / DismountHive
```

`RegistryClient` owns read / write / search / export / import bound to one machine.

## Read / write / search / mount

Missing Get → null. `/` throws. Views: Default / Registry64 / Registry32.  
Writes need `confirm: true`. HKLM SYSTEM / SAM / SECURITY / SOFTWARE\Microsoft → Denied.  
Logs path + value **name** only.

Search: StartsWith / EndsWith / Contains. Cap 256 / depth 32.  
Mount: HKLM or HKU, local only. Dispose unloads.

## Comparer (specified, not shipped)

```text
WriteIndex(path, hive, key, confirm, progress, cancel)     → vest-regidx/1
Compare(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel)
                                                            → vest-regcmp/1
```

Collect on each box, copy JSONL to the desk, compare there.  
Verify is keys only. Unrelated stops unless `force`. Same values are footer counts unless `includeSame`.
