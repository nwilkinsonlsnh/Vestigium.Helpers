# Vestigium.Helpers.WinReg — Developers Guide

**Document ID:** VEST-HLP-WINREG-DEV-000  
**Version:** 1.1  
**Status:** Matches engine after Phase 7  
**Date:** 12 September 2026  
**TFM:** `net10.0-windows`

Open `Vestigium.Helpers.slnx`. Implementation lives in `src/Vestigium.Helpers.WinReg/`.

No `reg.exe`. No `regedit`. Demo gallery is out of this guide.

## Façade

```text
RegistryHelper.Identity
RegistryHelper.Probe()
RegistryHelper.Local
RegistryHelper.For(machine)
RegistryHelper.CanConnect(machine)
RegistryHelper.ConnectTimeout

RegistryHelper.Export(path, hive, key, format, view, confirm)
RegistryHelper.Import(path, view, confirm)
RegistryHelper.Search(hive, key, term, mode, fields, maxDepth, maxResults, view)
RegistryHelper.MountHive(hiveFile, destination, subKey, confirm, out result)
RegistryHelper.DismountHive(destination, subKey, confirm)
```

`RegistryClient` owns the same read / write / search / export / import methods bound to one machine.

## Read

`GetKey` / `TryGetKey` / `GetValue` / `TryGetValue` / `ListSubKeys`

Missing → null. `/` in a path throws. Views: Default / Registry64 / Registry32.

## Write

`CreateKey` / `SetValue` / `DeleteKey` / `DeleteValue`

`confirm: false` → Denied. HKLM SYSTEM / SAM / SECURITY / SOFTWARE\Microsoft → Denied. Logs path + value **name** only.

## Search

StartsWith / EndsWith / Contains. Fields: KeyName, ValueName, ValueData (strings only). `maxResults` cap 256. `maxDepth` cap 32.

## Mount

HKLM or HKU only. Local only. Dispose unloads.
