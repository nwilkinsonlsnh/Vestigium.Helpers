# Vestigium.Helpers.WinReg — Requirements

**Document ID:** VEST-HLP-WINREG-SRS-000  
**Version:** 1.4  
**Status:** Current. Incorporates the v1.4 addendum.  
**Date:** 21 September 2026  
**Package:** `Vestigium.Helpers.WinReg`  
**TFM:** `net10.0-windows`

Companion addendum: [`Requirements_v1.4_Addendum.md`](Requirements_v1.4_Addendum.md).

---

## 1. Purpose

Give a Windows host one façade (`RegistryHelper` / `RegistryClient`) for registry inventory, search, confirmed writes, export/import, hive mount, journal, ACL, and offline compare.

---

## 2. Locked decisions

| # | Decision |
|---|---|
| 1 | Windows only. No `reg.exe` / `regedit`. |
| 2 | Missing Get is null. `/` is illegal in a key path. |
| 3 | Writes need `confirm: true`. Result objects, not surprise exceptions, on deny. |
| 4 | Logs path + value name. Never value data. |
| 5 | Search cap 256 / depth 32. |
| 6 | Mount is local HKLM or HKU only, confirm required. |
| 7 | Journal `vest-regjnl/1`. Compact / Purge fail `InUse` if a journal is open. |
| 8 | DeleteKey snapshots `beforeTree` (cap 256) for rollback. |
| 9 | Compare is index vs index. No payloads in `WriteIndex`. Restore without payload is `Unsupported`. |
| 10 | Library never calls `VestigiumLogger.Initialize`. APPID `WinReg`. |

---

## 3. Goals

**G1.** Read keys and values with availability.  
**G2.** Confirmed writes with a journal.  
**G3.** Export / import .reg.  
**G4.** Offline compare of two boxes via index files.  
**G5.** ACL read; ACL write / take ownership with confirm.

---

## 4. Still out

Linux registries, spawning `regedit`, storing payloads in the compare index, remote WriteIndex until backlog B2.
