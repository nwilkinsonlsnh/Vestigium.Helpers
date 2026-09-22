# Vestigium.Helpers.WinReg — SRS addendum 1.2

**Date:** 13 September 2026  
**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

### Mount signature (amends §12)

Shipped engine wins:

```text
IRegistryMount? MountHive(hiveFile, destination, subKey, confirm, out RegistryWriteResult result)
```

Null mount + Denied/NotFound/InUse/Unsupported on `result` when the load does not happen.

### Backlog

All P0–P3 library gaps: [`Requirements_Backlog_v1.0.md`](Requirements_Backlog_v1.0.md).
